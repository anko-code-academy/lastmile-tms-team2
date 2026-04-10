import { getSession } from "next-auth/react";

import {
  CANCEL_PARCEL,
  CONFIRM_INBOUND_RECEIVING_SESSION,
  GET_INBOUND_RECEIVING_SESSION,
  GET_OPEN_INBOUND_MANIFESTS,
  GET_PARCEL_IMPORT,
  GET_PARCEL_IMPORTS,
  GET_PARCEL_TRACKING_EVENTS,
  PARCEL,
  PARCEL_BY_TRACKING_NUMBER,
  GET_ROUTE_STAGING_BOARD,
  PRELOAD_PARCELS,
  PRELOAD_PARCELS_CONNECTION,
  PARCELS_FOR_ROUTE,
  REGISTER_PARCEL,
  REGISTERED_PARCELS,
  SCAN_INBOUND_PARCEL,
  GET_STAGING_ROUTES,
  START_INBOUND_RECEIVING_SESSION,
  STAGE_PARCEL_FOR_ROUTE,
  TRANSITION_PARCEL_STATUS,
  UPDATE_PARCEL,
} from "@/graphql/parcels";
import type { ParcelFilterInput, ParcelSortInput } from "@/graphql/generated";
import type {
  CancelParcelMutation,
  GetRouteStagingBoardQuery,
  GetParcelByTrackingNumberQuery,
  GetParcelImportQuery,
  GetParcelImportsQuery,
  GetInboundReceivingSessionQuery,
  GetOpenInboundManifestsQuery,
  GetParcelQuery,
  GetParcelsForRouteCreationQuery,
  GetParcelTrackingEventsQuery,
  GetPreLoadParcelsConnectionQuery,
  GetPreLoadParcelsQuery,
  GetRegisteredParcelsQuery,
  GetStagingRoutesQuery,
  ConfirmInboundReceivingSessionMutation,
  ScanInboundParcelMutation,
  StartInboundReceivingSessionMutation,
  StageParcelForRouteMutation,
  TransitionParcelStatusMutation,
  UpdateParcelMutation,
} from "@/graphql/parcels";
import { apiBaseUrl, parseApiErrorMessage } from "@/lib/network/api";
import { graphqlRequest } from "@/lib/network/graphql-client";
import { downloadAuthenticatedFile, saveBlobAsFile } from "@/lib/network/download";
import { isGuidString } from "@/lib/validation/guid-string";
import { ParcelWeightUnit } from "@/types/parcels";
import type {
  CancelParcelRequest,
  LabelDownloadFormat,
  ParcelConnectionPage,
  ParcelDetail,
  ParcelFormData,
  ParcelImportDetail,
  ParcelImportHistoryEntry,
  ParcelImportTemplateFormat,
  ParcelOption,
  RegisteredParcelResult,
  TrackingEvent,
  TransitionParcelStatusRequest,
  UpdateParcelRequest,
  UploadParcelImportRequest,
  UploadParcelImportResult,
  ConfirmInboundReceivingSessionRequest,
  InboundManifest,
  InboundParcelScanResult,
  InboundReceivingSession,
  RouteStagingBoard,
  ScanInboundParcelRequest,
  StageParcelForRouteRequest,
  StageParcelForRouteResult,
  StagingRouteSummary,
  StartInboundReceivingSessionRequest,
} from "@/types/parcels";

const USE_MOCK = false;
const mockParcels: Array<RegisteredParcelResult & { detail: ParcelDetail }> = [];

function buildApiUrl(path: string): string {
  return `${apiBaseUrl().replace(/\/$/, "")}${path}`;
}

function extractFileName(
  contentDisposition: string | null,
  fallbackFileName: string,
): string {
  if (!contentDisposition) {
    return fallbackFileName;
  }

  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const quotedMatch = contentDisposition.match(/filename="([^"]+)"/i);
  if (quotedMatch?.[1]) {
    return quotedMatch[1];
  }

  const plainMatch = contentDisposition.match(/filename=([^;]+)/i);
  if (plainMatch?.[1]) {
    return plainMatch[1].trim();
  }

  return fallbackFileName;
}

function toLocalParcelWeightUnit(weightUnit: string | null | undefined): number {
  return weightUnit?.toUpperCase() === "LB"
    ? ParcelWeightUnit.Lb
    : ParcelWeightUnit.Kg;
}

function findMockParcel(parcelKey: string) {
  return mockParcels.find(
    (candidate) =>
      candidate.id === parcelKey || candidate.trackingNumber === parcelKey,
  );
}

function mapStagingRoute(
  raw: NonNullable<GetStagingRoutesQuery["stagingRoutes"]>[number],
): StagingRouteSummary {
  return {
    id: raw.id,
    vehicleId: raw.vehicleId,
    vehiclePlate: raw.vehiclePlate,
    driverId: raw.driverId,
    driverName: raw.driverName,
    status: raw.status,
    stagingArea: raw.stagingArea,
    startDate: raw.startDate,
    expectedParcelCount: raw.expectedParcelCount,
    stagedParcelCount: raw.stagedParcelCount,
    remainingParcelCount: raw.remainingParcelCount,
  };
}

function mapRouteStagingBoard(
  raw: NonNullable<GetRouteStagingBoardQuery["routeStagingBoard"]>,
): RouteStagingBoard {
  return {
    id: raw.id,
    vehicleId: raw.vehicleId,
    vehiclePlate: raw.vehiclePlate,
    driverId: raw.driverId,
    driverName: raw.driverName,
    status: raw.status,
    stagingArea: raw.stagingArea,
    startDate: raw.startDate,
    expectedParcelCount: raw.expectedParcelCount,
    stagedParcelCount: raw.stagedParcelCount,
    remainingParcelCount: raw.remainingParcelCount,
    expectedParcels: raw.expectedParcels.map((parcel) => ({
      parcelId: parcel.parcelId,
      trackingNumber: parcel.trackingNumber,
      barcode: parcel.barcode,
      status: parcel.status,
      isStaged: parcel.isStaged,
    })),
  };
}

function mapStageParcelForRouteResult(
  raw: StageParcelForRouteMutation["stageParcelForRoute"],
): StageParcelForRouteResult {
  return {
    outcome: raw.outcome,
    message: raw.message,
    trackingNumber: raw.trackingNumber ?? null,
    parcelId: raw.parcelId ?? null,
    conflictingRouteId: raw.conflictingRouteId ?? null,
    conflictingStagingArea: raw.conflictingStagingArea ?? null,
    board: mapRouteStagingBoard(raw.board),
  };
}

async function authenticatedRequest(
  path: string,
  init: RequestInit = {},
): Promise<Response> {
  const session = await getSession();
  const headers = new Headers(init.headers);

  if (session?.accessToken) {
    headers.set("Authorization", `Bearer ${session.accessToken}`);
  }

  const response = await fetch(buildApiUrl(path), {
    ...init,
    headers,
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(await parseApiErrorMessage(response));
  }

  return response;
}

async function triggerDownload(
  response: Response,
  fallbackFileName: string,
): Promise<void> {
  if (typeof window === "undefined") {
    return;
  }

  const blob = await response.blob();
  const downloadUrl = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = downloadUrl;
  link.download = extractFileName(
    response.headers.get("Content-Disposition"),
    fallbackFileName,
  );
  document.body.append(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(downloadUrl);
}

export const parcelsService = {
  getPreLoadParcels: async (
    search?: string,
    where?: ParcelFilterInput,
    order?: ParcelSortInput[],
  ): Promise<GetPreLoadParcelsQuery["preLoadParcels"]> => {
    if (USE_MOCK) {
      return mockParcels;
    }

    const data = await graphqlRequest<GetPreLoadParcelsQuery>(PRELOAD_PARCELS, {
      search: search || undefined,
      where: where || undefined,
      order: order || undefined,
    });
    return data.preLoadParcels;
  },

  getPreLoadParcelsPage: async (
    search: string | undefined,
    where: ParcelFilterInput | undefined,
    order: ParcelSortInput[] | undefined,
    first: number,
    after?: string | null,
  ): Promise<
    ParcelConnectionPage<
      NonNullable<
        NonNullable<
          NonNullable<GetPreLoadParcelsConnectionQuery["preLoadParcelsConnection"]>["nodes"]
        >[number]
      >
    >
  > => {
    if (USE_MOCK) {
      const startIndex = after ? Number.parseInt(after, 10) || 0 : 0;
      const nodes = mockParcels.slice(startIndex, startIndex + first);
      const nextIndex = startIndex + nodes.length;
      return {
        totalCount: mockParcels.length,
        pageInfo: {
          hasNextPage: nextIndex < mockParcels.length,
          hasPreviousPage: startIndex > 0,
          startCursor: nodes.length > 0 ? String(startIndex) : null,
          endCursor: nodes.length > 0 ? String(nextIndex) : null,
        },
        nodes,
      };
    }

    const data = await graphqlRequest<GetPreLoadParcelsConnectionQuery>(
      PRELOAD_PARCELS_CONNECTION,
      {
        search: search || undefined,
        where: where || undefined,
        order: order || undefined,
        first,
        after: after || undefined,
      },
    );

    const connection = data.preLoadParcelsConnection;
    if (!connection) {
      return {
        totalCount: 0,
        pageInfo: {
          hasNextPage: false,
          hasPreviousPage: false,
          startCursor: null,
          endCursor: null,
        },
        nodes: [],
      };
    }

    return {
      totalCount: connection.totalCount ?? 0,
      pageInfo: {
        hasNextPage: connection.pageInfo.hasNextPage,
        hasPreviousPage: connection.pageInfo.hasPreviousPage,
        startCursor: connection.pageInfo.startCursor ?? null,
        endCursor: connection.pageInfo.endCursor ?? null,
      },
      nodes: (connection.nodes ?? []).filter(Boolean) as NonNullable<
        NonNullable<
          NonNullable<GetPreLoadParcelsConnectionQuery["preLoadParcelsConnection"]>["nodes"]
        >[number]
      >[],
    };
  },

  getForRouteCreation: async (
    vehicleId: string,
    driverId: string,
  ): Promise<ParcelOption[]> => {
    if (USE_MOCK) {
      return mockParcels.map((parcel) => ({
        id: parcel.id,
        trackingNumber: parcel.trackingNumber,
        weight: parcel.weight,
        weightUnit: toLocalParcelWeightUnit(parcel.weightUnit),
        zoneId: parcel.zoneId,
        zoneName: parcel.zoneName,
      }));
    }

    const data = await graphqlRequest<GetParcelsForRouteCreationQuery>(
      PARCELS_FOR_ROUTE,
      {
        vehicleId,
        driverId,
      },
    );

    return data.parcelsForRouteCreation.map((parcel) => ({
      id: parcel.id,
      trackingNumber: parcel.trackingNumber,
      weight: parcel.weight,
      weightUnit: toLocalParcelWeightUnit(parcel.weightUnit),
      zoneId: parcel.zoneId,
      zoneName: parcel.zoneName ?? null,
    }));
  },

  getStagingRoutes: async (): Promise<StagingRouteSummary[]> => {
    if (USE_MOCK) {
      return [];
    }

    const data = await graphqlRequest<GetStagingRoutesQuery>(GET_STAGING_ROUTES);
    return data.stagingRoutes.map(mapStagingRoute);
  },

  getRouteStagingBoard: async (
    routeId: string,
  ): Promise<RouteStagingBoard | null> => {
    if (USE_MOCK) {
      return null;
    }

    const data = await graphqlRequest<GetRouteStagingBoardQuery>(
      GET_ROUTE_STAGING_BOARD,
      { routeId },
    );

    return data.routeStagingBoard
      ? mapRouteStagingBoard(data.routeStagingBoard)
      : null;
  },

  stageParcelForRoute: async (
    request: StageParcelForRouteRequest,
  ): Promise<StageParcelForRouteResult> => {
    if (USE_MOCK) {
      throw new Error("Route staging is not available in mock mode.");
    }

    const data = await graphqlRequest<{
      stageParcelForRoute: StageParcelForRouteMutation["stageParcelForRoute"];
    }>(STAGE_PARCEL_FOR_ROUTE, {
      input: {
        routeId: request.routeId,
        barcode: request.barcode,
      },
    });

    return mapStageParcelForRouteResult(data.stageParcelForRoute);
  },

  getRegisteredParcels: async (
    _statusFilter?: string | null,
  ): Promise<GetRegisteredParcelsQuery["registeredParcels"]> => {
    void _statusFilter;

    if (USE_MOCK) {
      return mockParcels;
    }

    const data = await graphqlRequest<GetRegisteredParcelsQuery>(REGISTERED_PARCELS);
    return data.registeredParcels;
  },

  getById: async (id: string): Promise<ParcelDetail> => {
    if (USE_MOCK) {
      const parcel = findMockParcel(id)?.detail;
      if (!parcel) {
        throw new Error("Parcel not found.");
      }

      return parcel;
    }

    const data = await graphqlRequest<GetParcelQuery>(PARCEL, { id });

    if (!data.parcel) {
      throw new Error("Parcel not found.");
    }

    return data.parcel as ParcelDetail;
  },

  getByTrackingNumber: async (trackingNumber: string): Promise<ParcelDetail> => {
    if (USE_MOCK) {
      const parcel = findMockParcel(trackingNumber)?.detail;
      if (!parcel) {
        throw new Error("Parcel not found.");
      }

      return parcel;
    }

    const data = await graphqlRequest<GetParcelByTrackingNumberQuery>(
      PARCEL_BY_TRACKING_NUMBER,
      { trackingNumber },
    );

    if (!data.parcelByTrackingNumber) {
      throw new Error("Parcel not found.");
    }

    return data.parcelByTrackingNumber as ParcelDetail;
  },

  getByKey: async (parcelKey: string): Promise<ParcelDetail> => {
    return isGuidString(parcelKey)
      ? parcelsService.getById(parcelKey)
      : parcelsService.getByTrackingNumber(parcelKey);
  },

  register: async (form: ParcelFormData): Promise<RegisteredParcelResult> => {
    if (USE_MOCK) {
      return {
        id: "40000000-0000-0000-0000-000000000099",
        trackingNumber: "LM20260329MOCK001",
        barcode: "LM20260329MOCK001",
        status: "Registered",
        serviceType: form.serviceType,
        weight: form.weight,
        weightUnit: form.weightUnit === 1 ? "KG" : "LB",
        length: form.length,
        width: form.width,
        height: form.height,
        dimensionUnit: form.dimensionUnit,
        declaredValue: form.declaredValue,
        currency: form.currency,
        description: form.description || null,
        parcelType: form.parcelType || null,
        estimatedDeliveryDate: form.estimatedDeliveryDate,
        createdAt: new Date().toISOString(),
        zoneId: "00000000-0000-0000-0000-000000000099",
        zoneName: "Mock Zone",
        depotId: "00000000-0000-0000-0000-000000000099",
        depotName: "Mock Depot",
      };
    }

    const data = await graphqlRequest<{ registerParcel: RegisteredParcelResult }>(
      REGISTER_PARCEL,
      {
      input: {
        shipperAddressId: form.shipperAddressId,
        recipientAddress: {
          street1: form.recipientStreet1,
          street2: form.recipientStreet2 || null,
          city: form.recipientCity,
          state: form.recipientState,
          postalCode: form.recipientPostalCode,
          countryCode: form.recipientCountryCode,
          isResidential: form.recipientIsResidential,
          contactName: form.recipientContactName || null,
          companyName: form.recipientCompanyName || null,
          phone: form.recipientPhone || null,
          email: form.recipientEmail || null,
        },
        description: form.description || null,
        parcelType: form.parcelType || null,
        serviceType: form.serviceType,
        weight: form.weight,
        weightUnit: form.weightUnit === 1 ? "KG" : "LB",
        length: form.length,
        width: form.width,
        height: form.height,
        dimensionUnit: form.dimensionUnit === "CM" ? "CM" : "IN",
        declaredValue: form.declaredValue,
        currency: form.currency,
        estimatedDeliveryDate: `${form.estimatedDeliveryDate}T00:00:00+00:00`,
      },
      },
    );
    return data.registerParcel;
  },

  update: async ({ id, data }: UpdateParcelRequest): Promise<ParcelDetail> => {
    if (USE_MOCK) {
      const parcel = mockParcels.find((candidate) => candidate.id === id)?.detail;
      if (!parcel) {
        throw new Error("Parcel not found.");
      }

      return {
        ...parcel,
        shipperAddressId: data.shipperAddressId,
        description: data.description || null,
        parcelType: data.parcelType || null,
        serviceType: data.serviceType,
        weight: data.weight,
        weightUnit: data.weightUnit === 1 ? "KG" : "LB",
        length: data.length,
        width: data.width,
        height: data.height,
        dimensionUnit: data.dimensionUnit,
        declaredValue: data.declaredValue,
        currency: data.currency,
        estimatedDeliveryDate: `${data.estimatedDeliveryDate}T00:00:00+00:00`,
        lastModifiedAt: new Date().toISOString(),
        recipientAddress: {
          street1: data.recipientStreet1,
          street2: data.recipientStreet2 || null,
          city: data.recipientCity,
          state: data.recipientState,
          postalCode: data.recipientPostalCode,
          countryCode: data.recipientCountryCode,
          isResidential: data.recipientIsResidential,
          contactName: data.recipientContactName || null,
          companyName: data.recipientCompanyName || null,
          phone: data.recipientPhone || null,
          email: data.recipientEmail || null,
        },
        changeHistory: [
          {
            action: "Updated",
            fieldName: "Description",
            beforeValue: parcel.description,
            afterValue: data.description || null,
            changedAt: new Date().toISOString(),
            changedBy: "Mock User",
          },
        ],
      };
    }

    const result = await graphqlRequest<UpdateParcelMutation>(UPDATE_PARCEL, {
        input: {
          id,
          shipperAddressId: data.shipperAddressId,
          recipientAddress: {
            street1: data.recipientStreet1,
            street2: data.recipientStreet2 || null,
            city: data.recipientCity,
            state: data.recipientState,
            postalCode: data.recipientPostalCode,
            countryCode: data.recipientCountryCode,
            isResidential: data.recipientIsResidential,
            contactName: data.recipientContactName || null,
            companyName: data.recipientCompanyName || null,
            phone: data.recipientPhone || null,
            email: data.recipientEmail || null,
          },
          description: data.description || null,
          parcelType: data.parcelType || null,
          serviceType: data.serviceType,
          weight: data.weight,
          weightUnit: data.weightUnit === 1 ? "KG" : "LB",
          length: data.length,
          width: data.width,
          height: data.height,
          dimensionUnit: data.dimensionUnit === "CM" ? "CM" : "IN",
          declaredValue: data.declaredValue,
          currency: data.currency,
          estimatedDeliveryDate: `${data.estimatedDeliveryDate}T00:00:00+00:00`,
        },
      });

    if (!result.updateParcel) {
      throw new Error("Parcel not found.");
    }

    return result.updateParcel as ParcelDetail;
  },

  cancel: async ({ id, reason }: CancelParcelRequest): Promise<ParcelDetail> => {
    if (USE_MOCK) {
      const parcel = mockParcels.find((candidate) => candidate.id === id)?.detail;
      if (!parcel) {
        throw new Error("Parcel not found.");
      }

      return {
        ...parcel,
        status: "Cancelled",
        cancellationReason: reason,
        canEdit: false,
        canCancel: false,
        allowedNextStatuses: [],
        lastModifiedAt: new Date().toISOString(),
        changeHistory: [
          {
            action: "Cancelled",
            fieldName: "Cancellation reason",
            beforeValue: null,
            afterValue: reason,
            changedAt: new Date().toISOString(),
            changedBy: "Mock User",
          },
          ...parcel.changeHistory,
        ],
      };
    }

    const result = await graphqlRequest<CancelParcelMutation>(CANCEL_PARCEL, {
      input: {
        id,
        reason,
      },
    });

    if (!result.cancelParcel) {
      throw new Error("Parcel not found.");
    }

    return result.cancelParcel as ParcelDetail;
  },

  downloadLabel: async (id: string, format: LabelDownloadFormat): Promise<string> => {
    if (USE_MOCK) {
      const parcel = mockParcels.find((candidate) => candidate.id === id);
      if (!parcel) {
        throw new Error("Parcel not found.");
      }

      const mockBody =
        format === "zpl"
          ? `^XA^FO40,40^A0N,40,40^FD${parcel.trackingNumber}^FS^XZ`
          : `Mock A4 label for ${parcel.trackingNumber}`;

      const blob = new Blob([mockBody], {
        type: format === "zpl" ? "text/plain;charset=utf-8" : "application/pdf",
      });

      const fileName =
        format === "zpl"
          ? `parcel-${parcel.trackingNumber}.zpl`
          : `parcel-${parcel.trackingNumber}-a4.pdf`;

      saveBlobAsFile(blob, fileName);
      return fileName;
    }

    return downloadAuthenticatedFile(
      format === "zpl"
        ? `/api/parcels/${id}/labels/4x6.zpl`
        : `/api/parcels/${id}/labels/a4.pdf`,
    );
  },

  downloadBulkLabels: async (
    parcelIds: string[],
    format: LabelDownloadFormat,
  ): Promise<string> => {
    if (parcelIds.length === 0) {
      throw new Error("Select at least one parcel.");
    }

    if (USE_MOCK) {
      const fileName =
        format === "zpl" ? "parcel-labels-4x6.zpl" : "parcel-labels-a4.pdf";
      const blob = new Blob(
        [
          format === "zpl"
            ? parcelIds
                .map((parcelId) => {
                  const parcel = mockParcels.find((candidate) => candidate.id === parcelId);
                  return `^XA^FO40,40^A0N,40,40^FD${parcel?.trackingNumber ?? parcelId}^FS^XZ`;
                })
                .join("\n")
            : `Mock A4 labels for ${parcelIds.length} parcels`,
        ],
        {
          type: format === "zpl" ? "text/plain;charset=utf-8" : "application/pdf",
        },
      );

      saveBlobAsFile(blob, fileName);
      return fileName;
    }

    return downloadAuthenticatedFile(
      format === "zpl"
        ? "/api/parcels/labels/4x6.zpl"
        : "/api/parcels/labels/a4.pdf",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ parcelIds }),
      },
    );
  },

  getParcelImports: async (): Promise<ParcelImportHistoryEntry[]> => {
    if (USE_MOCK) {
      return [];
    }

    const data = await graphqlRequest<{
      parcelImports: GetParcelImportsQuery["parcelImports"];
    }>(GET_PARCEL_IMPORTS);

    return data.parcelImports as ParcelImportHistoryEntry[];
  },

  getParcelImport: async (id: string): Promise<ParcelImportDetail | null> => {
    if (USE_MOCK) {
      return null;
    }

    const data = await graphqlRequest<{
      parcelImport: GetParcelImportQuery["parcelImport"];
    }>(GET_PARCEL_IMPORT, { id });

    return (data.parcelImport as ParcelImportDetail | null) ?? null;
  },

  uploadParcelImport: async (
    request: UploadParcelImportRequest,
  ): Promise<UploadParcelImportResult> => {
    if (USE_MOCK) {
      return { importId: "mock-import-1" };
    }

    const formData = new FormData();
    formData.append("shipperAddressId", request.shipperAddressId);
    formData.append("file", request.file);

    const response = await authenticatedRequest("/api/parcel-imports", {
      method: "POST",
      body: formData,
    });

    return (await response.json()) as UploadParcelImportResult;
  },

  downloadParcelImportTemplate: async (
    format: ParcelImportTemplateFormat,
  ): Promise<void> => {
    if (USE_MOCK) {
      return;
    }

    const response = await authenticatedRequest(
      `/api/parcel-imports/template.${format}`,
      { method: "GET" },
    );

    await triggerDownload(response, `parcel-import-template.${format}`);
  },

  downloadParcelImportErrors: async (id: string): Promise<void> => {
    if (USE_MOCK) {
      return;
    }

    const response = await authenticatedRequest(
      `/api/parcel-imports/${id}/errors.csv`,
      { method: "GET" },
    );

    await triggerDownload(response, "parcel-import-errors.csv");
  },

  getTrackingEvents: async (parcelId: string): Promise<TrackingEvent[]> => {
    if (USE_MOCK) {
      return findMockParcel(parcelId)?.detail.statusTimeline ?? [];
    }

    const data = await graphqlRequest<{
      parcelTrackingEvents: GetParcelTrackingEventsQuery["parcelTrackingEvents"];
    }>(GET_PARCEL_TRACKING_EVENTS, { parcelId });

    return data.parcelTrackingEvents as TrackingEvent[];
  },

  transitionStatus: async (
    request: TransitionParcelStatusRequest,
  ): Promise<RegisteredParcelResult> => {
    if (USE_MOCK) {
      return {
        id: request.parcelId,
        trackingNumber: "MOCK001",
        barcode: "MOCK001",
        status: request.newStatus,
        serviceType: "STANDARD",
        weight: 0,
        weightUnit: "KG",
        length: 0,
        width: 0,
        height: 0,
        dimensionUnit: "CM",
        declaredValue: 0,
        currency: "USD",
        description: null,
        parcelType: null,
        estimatedDeliveryDate: new Date().toISOString(),
        createdAt: new Date().toISOString(),
        zoneId: "00000000-0000-0000-0000-000000000000",
        zoneName: null,
        depotId: "00000000-0000-0000-0000-000000000000",
        depotName: null,
      };
    }

    const data = await graphqlRequest<{
      transitionParcelStatus: TransitionParcelStatusMutation["transitionParcelStatus"];
    }>(TRANSITION_PARCEL_STATUS, {
      input: {
        parcelId: request.parcelId,
        newStatus: request.newStatus,
        location: request.location ?? null,
        description: request.description ?? null,
      },
    });

    return data.transitionParcelStatus as RegisteredParcelResult;
  },

  getOpenInboundManifests: async (): Promise<InboundManifest[]> => {
    if (USE_MOCK) {
      return [];
    }

    const data = await graphqlRequest<GetOpenInboundManifestsQuery>(
      GET_OPEN_INBOUND_MANIFESTS,
    );

    return (data.openInboundManifests ?? []) as InboundManifest[];
  },

  getInboundReceivingSession: async (
    sessionId: string,
  ): Promise<InboundReceivingSession | null> => {
    if (USE_MOCK) {
      return null;
    }

    const data = await graphqlRequest<GetInboundReceivingSessionQuery>(
      GET_INBOUND_RECEIVING_SESSION,
      { sessionId },
    );

    return (data.inboundReceivingSession as InboundReceivingSession | null) ?? null;
  },

  startInboundReceivingSession: async (
    request: StartInboundReceivingSessionRequest,
  ): Promise<InboundReceivingSession> => {
    if (USE_MOCK) {
      throw new Error("Inbound receiving is not available in mock mode.");
    }

    const data = await graphqlRequest<{
      startInboundReceivingSession: StartInboundReceivingSessionMutation["startInboundReceivingSession"];
    }>(START_INBOUND_RECEIVING_SESSION, {
      input: {
        manifestId: request.manifestId,
      },
    });

    return data.startInboundReceivingSession as InboundReceivingSession;
  },

  scanInboundParcel: async (
    request: ScanInboundParcelRequest,
  ): Promise<InboundParcelScanResult> => {
    if (USE_MOCK) {
      throw new Error("Inbound receiving is not available in mock mode.");
    }

    const data = await graphqlRequest<{
      scanInboundParcel: ScanInboundParcelMutation["scanInboundParcel"];
    }>(SCAN_INBOUND_PARCEL, {
      input: {
        sessionId: request.sessionId,
        barcode: request.barcode,
      },
    });

    return data.scanInboundParcel as InboundParcelScanResult;
  },

  confirmInboundReceivingSession: async (
    request: ConfirmInboundReceivingSessionRequest,
  ): Promise<InboundReceivingSession> => {
    if (USE_MOCK) {
      throw new Error("Inbound receiving is not available in mock mode.");
    }

    const data = await graphqlRequest<{
      confirmInboundReceivingSession: ConfirmInboundReceivingSessionMutation["confirmInboundReceivingSession"];
    }>(CONFIRM_INBOUND_RECEIVING_SESSION, {
      input: {
        sessionId: request.sessionId,
      },
    });

    return data.confirmInboundReceivingSession as InboundReceivingSession;
  },
};
