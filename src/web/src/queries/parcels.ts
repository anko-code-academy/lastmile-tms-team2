import { useEffect } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  HubConnectionBuilder,
  HubConnectionState,
} from "@microsoft/signalr";
import { useSession } from "next-auth/react";

import { parcelsService } from "@/services/parcels.service";
import type { ParcelFilterInput, ParcelSortInput } from "@/graphql/generated";
import { normalizeParcelStatusForFilter } from "@/lib/labels/parcels";
import { apiBaseUrl } from "@/lib/network/api";
import type { MutationToastMeta } from "@/lib/query/mutation-toast-meta";
import type {
  CancelParcelRequest,
  GraphQLParcelStatus,
  LabelDownloadFormat,
  ParcelDetail,
  ParcelFormData,
  ParcelImportDetail,
  ParcelImportTemplateFormat,
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
  LoadParcelForRouteRequest,
  CompleteLoadOutRequest,
} from "@/types/parcels";
import type {
  LoadOutRoute,
  RouteLoadOutBoard,
  LoadParcelForRouteResult,
  CompleteLoadOutResult,
} from "@/graphql/generated";

const parcelImportPollingStatuses = new Set(["Queued", "Processing"]);
const parcelStatusLiveUpdateStatus = "OUT_FOR_DELIVERY";

type ParcelUpdatedEvent = {
  trackingNumber?: string | null;
  status?: string | null;
  lastModifiedAt?: string | null;
};

export const parcelKeys = {
  all: ["parcels"] as const,
  preLoad: (
    search?: string,
    where?: ParcelFilterInput,
    order?: ParcelSortInput[],
  ) => [...parcelKeys.all, "preLoad", search ?? "", JSON.stringify(where ?? {}), JSON.stringify(order ?? [])] as const,
  preLoadPage: (
    search: string | undefined,
    where: ParcelFilterInput | undefined,
    order: ParcelSortInput[] | undefined,
    first: number,
    after?: string | null,
  ) =>
    [
      ...parcelKeys.all,
      "preLoadPage",
      search ?? "",
      JSON.stringify(where ?? {}),
      JSON.stringify(order ?? []),
      first,
      after ?? "",
    ] as const,
  preLoadAll: () => [...parcelKeys.all, "preLoadAll"] as const,
  forRoute: (vehicleId?: string, driverId?: string) =>
    [...parcelKeys.all, "forRoute", vehicleId ?? "", driverId ?? ""] as const,
  registered: () => [...parcelKeys.all, "registered"] as const,
  details: () => [...parcelKeys.all, "detail"] as const,
  detail: (id: string) => [...parcelKeys.details(), id] as const,
  imports: () => [...parcelKeys.all, "imports"] as const,
  importDetail: (id: string) => [...parcelKeys.imports(), "detail", id] as const,
  trackingEvents: (parcelId: string) => [...parcelKeys.detail(parcelId), "trackingEvents"] as const,
  inboundManifests: () => [...parcelKeys.all, "inboundManifests"] as const,
  inboundSession: (sessionId?: string | null) =>
    [...parcelKeys.all, "inboundSession", sessionId ?? ""] as const,
  stagingRoutes: () => [...parcelKeys.all, "stagingRoutes"] as const,
  routeStagingBoard: (routeId?: string | null) =>
    [...parcelKeys.all, "routeStagingBoard", routeId ?? ""] as const,
  loadOutRoutes: () => [...parcelKeys.all, "loadOutRoutes"] as const,
  routeLoadOutBoard: (routeId?: string | null) =>
    [...parcelKeys.all, "routeLoadOutBoard", routeId ?? ""] as const,
};

export function useParcelsForRouteCreation(
  vehicleId?: string,
  driverId?: string,
) {
  const { status } = useSession();
  return useQuery({
    queryKey: parcelKeys.forRoute(vehicleId, driverId),
    queryFn: () => parcelsService.getForRouteCreation(vehicleId!, driverId!),
    enabled: status === "authenticated" && !!vehicleId && !!driverId,
  });
}

export function useStagingRoutes() {
  const { status } = useSession();
  return useQuery<StagingRouteSummary[]>({
    queryKey: parcelKeys.stagingRoutes(),
    queryFn: () => parcelsService.getStagingRoutes(),
    enabled: status === "authenticated",
  });
}

export function useRouteStagingBoard(routeId?: string | null) {
  const { status } = useSession();
  return useQuery<RouteStagingBoard | null>({
    queryKey: parcelKeys.routeStagingBoard(routeId),
    queryFn: () => parcelsService.getRouteStagingBoard(routeId!),
    enabled: status === "authenticated" && !!routeId,
  });
}

export function useStageParcelForRoute() {
  const qc = useQueryClient();
  return useMutation<StageParcelForRouteResult, Error, StageParcelForRouteRequest>({
    mutationFn: (request) => parcelsService.stageParcelForRoute(request),
    onSuccess: async (result, variables) => {
      qc.setQueryData(parcelKeys.routeStagingBoard(variables.routeId), result.board);
      await qc.invalidateQueries({ queryKey: parcelKeys.stagingRoutes() });
      await qc.invalidateQueries({ queryKey: parcelKeys.all });
      await qc.invalidateQueries({ queryKey: parcelKeys.details() });
    },
  });
}

export function useLoadOutRoutes() {
  const { status } = useSession();
  return useQuery<LoadOutRoute[]>({
    queryKey: parcelKeys.loadOutRoutes(),
    queryFn: () => parcelsService.getLoadOutRoutes(),
    enabled: status === "authenticated",
  });
}

export function useRouteLoadOutBoard(routeId?: string | null) {
  const { status } = useSession();
  return useQuery<RouteLoadOutBoard | null>({
    queryKey: parcelKeys.routeLoadOutBoard(routeId),
    queryFn: () => parcelsService.getRouteLoadOutBoard(routeId!),
    enabled: status === "authenticated" && !!routeId,
  });
}

export function useLoadParcelForRoute() {
  const qc = useQueryClient();
  return useMutation<LoadParcelForRouteResult, Error, LoadParcelForRouteRequest>({
    mutationFn: (request) => parcelsService.loadParcelForRoute(request),
    onSuccess: async (result, variables) => {
      qc.setQueryData(parcelKeys.routeLoadOutBoard(variables.routeId), result.board);
      await qc.invalidateQueries({ queryKey: parcelKeys.loadOutRoutes() });
      await qc.invalidateQueries({ queryKey: parcelKeys.all });
      await qc.invalidateQueries({ queryKey: parcelKeys.details() });
    },
  });
}

export function useCompleteLoadOut() {
  const qc = useQueryClient();
  return useMutation<CompleteLoadOutResult, Error, CompleteLoadOutRequest>({
    mutationFn: (request) => parcelsService.completeLoadOut(request),
    meta: {
      successToast: {
        title: "Load-out completed",
        describe: () => "The route is now in progress.",
      },
    } satisfies MutationToastMeta,
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: parcelKeys.all });
      await qc.invalidateQueries({ queryKey: parcelKeys.loadOutRoutes() });
    },
  });
}

export function usePreLoadParcelsPage(
  search: string | undefined,
  where: ParcelFilterInput | undefined,
  order: ParcelSortInput[] | undefined,
  first: number,
  after?: string | null,
) {
  const { status: sessionStatus } = useSession();
  return useQuery({
    queryKey: parcelKeys.preLoadPage(search, where, order, first, after),
    queryFn: () =>
      parcelsService.getPreLoadParcelsPage(search, where, order, first, after),
    placeholderData: keepPreviousData,
    enabled: sessionStatus === "authenticated",
  });
}

export function usePreLoadParcels(
  search?: string,
  where?: ParcelFilterInput,
  order?: ParcelSortInput[],
) {
  const { status: sessionStatus } = useSession();
  return useQuery({
    queryKey: parcelKeys.preLoad(search, where, order),
    queryFn: () => parcelsService.getPreLoadParcels(search, where, order),
    placeholderData: keepPreviousData,
    enabled: sessionStatus === "authenticated",
  });
}

export function useAvailableParcelTypes() {
  const { status: sessionStatus } = useSession();
  return useQuery({
    queryKey: parcelKeys.preLoadAll(),
    queryFn: () => parcelsService.getPreLoadParcels(),
    staleTime: 5 * 60 * 1000,
    enabled: sessionStatus === "authenticated",
  });
}

export function useRegisteredParcels(statusFilter?: GraphQLParcelStatus | null) {
  const { status } = useSession();
  return useQuery({
    queryKey: [...parcelKeys.registered(), statusFilter ?? "all"] as const,
    queryFn: () => parcelsService.getRegisteredParcels(statusFilter),
    enabled: status === "authenticated",
  });
}

export function useRegisterParcel() {
  const qc = useQueryClient();
  return useMutation<
    RegisteredParcelResult,
    Error,
    ParcelFormData
  >({
    mutationFn: (form: ParcelFormData) => parcelsService.register(form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: parcelKeys.all });
    },
  });
}

export function useUpdateParcel() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (request: UpdateParcelRequest) => parcelsService.update(request),
    meta: {
      successToast: {
        title: "Parcel updated",
        describe: () => "Parcel changes were saved successfully.",
      },
    } satisfies MutationToastMeta,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: parcelKeys.all });
      qc.invalidateQueries({ queryKey: parcelKeys.details() });
    },
  });
}

export function useCancelParcel() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (request: CancelParcelRequest) => parcelsService.cancel(request),
    meta: {
      successToast: {
        title: "Parcel cancelled",
        describe: () => "The parcel was removed from the pre-load queue.",
      },
    } satisfies MutationToastMeta,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: parcelKeys.all });
      qc.invalidateQueries({ queryKey: parcelKeys.details() });
    },
  });
}

export function useParcel(parcelKey: string) {
  const { status } = useSession();

  return useQuery<ParcelDetail>({
    queryKey: parcelKeys.detail(parcelKey),
    queryFn: () => parcelsService.getByKey(parcelKey),
    enabled: status === "authenticated" && Boolean(parcelKey),
  });
}

export function useParcelImports() {
  const { status } = useSession();
  return useQuery({
    queryKey: parcelKeys.imports(),
    queryFn: () => parcelsService.getParcelImports(),
    enabled: status === "authenticated",
  });
}

export function useParcelImport(importId: string | null | undefined) {
  const { status } = useSession();

  return useQuery<ParcelImportDetail | null>({
    queryKey: parcelKeys.importDetail(importId ?? "latest"),
    queryFn: () => parcelsService.getParcelImport(importId!),
    enabled: status === "authenticated" && !!importId,
    refetchInterval: (query) => {
      const data = query.state.data;
      return data && parcelImportPollingStatuses.has(data.status) ? 1000 : false;
    },
  });
}

export function useUploadParcelImport() {
  const qc = useQueryClient();

  return useMutation<UploadParcelImportResult, Error, UploadParcelImportRequest>({
    mutationFn: (request) => parcelsService.uploadParcelImport(request),
    onSuccess: async (result) => {
      await qc.invalidateQueries({ queryKey: parcelKeys.imports() });
      await qc.invalidateQueries({
        queryKey: parcelKeys.importDetail(result.importId),
      });
    },
  });
}

export function useDownloadParcelImportTemplate() {
  return useMutation<void, Error, ParcelImportTemplateFormat>({
    mutationFn: (format) => parcelsService.downloadParcelImportTemplate(format),
  });
}

export function useDownloadParcelImportErrors() {
  return useMutation<void, Error, string>({
    mutationFn: (importId) => parcelsService.downloadParcelImportErrors(importId),
  });
}

export function useDownloadParcelLabel() {
  return useMutation<string, Error, { parcelId: string; format: LabelDownloadFormat }>({
    mutationFn: ({ parcelId, format }) =>
      parcelsService.downloadLabel(parcelId, format),
  });
}

export function useParcelTrackingEvents(parcelId: string) {
  const { status } = useSession();
  return useQuery<TrackingEvent[]>({
    queryKey: parcelKeys.trackingEvents(parcelId),
    queryFn: () => parcelsService.getTrackingEvents(parcelId),
    enabled: status === "authenticated" && Boolean(parcelId),
  });
}

export function useTransitionParcelStatus() {
  const qc = useQueryClient();
  return useMutation<RegisteredParcelResult, Error, TransitionParcelStatusRequest>({
    mutationFn: (request) => parcelsService.transitionStatus(request),
    meta: {
      successToast: {
        title: "Status updated",
        describe: () => "The parcel status was saved and the timeline was updated.",
      },
    } satisfies MutationToastMeta,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: parcelKeys.all });
      qc.invalidateQueries({ queryKey: parcelKeys.details() });
    },
  });
}

export function useOpenInboundManifests() {
  const { status } = useSession();
  return useQuery<InboundManifest[]>({
    queryKey: parcelKeys.inboundManifests(),
    queryFn: () => parcelsService.getOpenInboundManifests(),
    enabled: status === "authenticated",
  });
}

export function useInboundReceivingSession(sessionId: string | null | undefined) {
  const { status } = useSession();
  return useQuery<InboundReceivingSession | null>({
    queryKey: parcelKeys.inboundSession(sessionId),
    queryFn: () => parcelsService.getInboundReceivingSession(sessionId!),
    enabled: status === "authenticated" && !!sessionId,
  });
}

export function useStartInboundReceivingSession() {
  const qc = useQueryClient();
  return useMutation<InboundReceivingSession, Error, StartInboundReceivingSessionRequest>({
    mutationFn: (request) => parcelsService.startInboundReceivingSession(request),
    onSuccess: async (session) => {
      qc.setQueryData(parcelKeys.inboundSession(session.id), session);
      await qc.invalidateQueries({ queryKey: parcelKeys.inboundManifests() });
    },
  });
}

export function useScanInboundParcel() {
  const qc = useQueryClient();
  return useMutation<InboundParcelScanResult, Error, ScanInboundParcelRequest>({
    mutationFn: (request) => parcelsService.scanInboundParcel(request),
    onSuccess: async (result) => {
      qc.setQueryData(parcelKeys.inboundSession(result.session.id), result.session);
      await qc.invalidateQueries({ queryKey: parcelKeys.inboundManifests() });
      await qc.invalidateQueries({ queryKey: parcelKeys.all });
      await qc.invalidateQueries({ queryKey: parcelKeys.details() });
    },
  });
}

export function useConfirmInboundReceivingSession() {
  const qc = useQueryClient();
  return useMutation<InboundReceivingSession, Error, ConfirmInboundReceivingSessionRequest>({
    mutationFn: (request) => parcelsService.confirmInboundReceivingSession(request),
    onSuccess: async (session) => {
      qc.setQueryData(parcelKeys.inboundSession(session.id), session);
      await qc.invalidateQueries({ queryKey: parcelKeys.inboundManifests() });
      await qc.invalidateQueries({ queryKey: parcelKeys.all });
      await qc.invalidateQueries({ queryKey: parcelKeys.details() });
    },
  });
}

export function useParcelRealtimeUpdates(parcel: ParcelDetail | null | undefined) {
  const queryClient = useQueryClient();
  const { data: session, status } = useSession();
  const accessToken = session?.accessToken ?? "";
  const trackingNumber = parcel?.trackingNumber ?? "";
  const normalizedStatus = normalizeParcelStatusForFilter(parcel?.status ?? "");

  useEffect(() => {
    if (
      status !== "authenticated" ||
      normalizedStatus !== parcelStatusLiveUpdateStatus ||
      !trackingNumber
    ) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl().replace(/\/$/, "")}/hubs/parcels`, {
        // Rebuild the connection when NextAuth rotates the token so SignalR renegotiates with fresh auth.
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .build();

    const handleParcelUpdated = (event: ParcelUpdatedEvent) => {
      if (event.trackingNumber !== trackingNumber) {
        return;
      }

      void queryClient.invalidateQueries({ queryKey: parcelKeys.details() });
    };

    connection.on("ParcelUpdated", handleParcelUpdated);

    let isDisposed = false;

    void (async () => {
      try {
        await connection.start();

        if (isDisposed) {
          return;
        }

        await connection.invoke("SubscribeToParcel", trackingNumber);
      } catch (error) {
        console.warn("Failed to subscribe to parcel updates", error);
      }
    })();

    return () => {
      isDisposed = true;
      connection.off("ParcelUpdated", handleParcelUpdated);

      void (async () => {
        try {
          if (connection.state === HubConnectionState.Connected) {
            await connection.invoke("UnsubscribeFromParcel", trackingNumber);
          }
        } catch (error) {
          console.warn("Failed to unsubscribe from parcel updates", error);
        } finally {
          try {
            await connection.stop();
          } catch (error) {
            console.warn("Failed to stop parcel updates connection", error);
          }
        }
      })();
    };
  }, [accessToken, normalizedStatus, queryClient, status, trackingNumber]);
}
