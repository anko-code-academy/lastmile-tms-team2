import type { RouteFilterInput } from "@/graphql/generated";
import {
  CANCEL_ROUTE,
  COMPLETE_ROUTE,
  CREATE_ROUTE,
  DISPATCH_ROUTE,
  GET_MY_ROUTE,
  GET_MY_ROUTES,
  GET_ROUTE,
  GET_ROUTE_ASSIGNMENT_CANDIDATES,
  GET_ROUTE_PLAN_PREVIEW,
  PAGINATED_ROUTES,
  START_ROUTE,
  UPDATE_ROUTE_ASSIGNMENT,
} from "@/graphql/routes";
import type {
  CancelRouteMutation,
  CompleteRouteMutation,
  CreateRouteMutation,
  DispatchRouteMutation,
  GetMyRouteQuery,
  GetMyRoutesQuery,
  GetRouteAssignmentCandidatesQuery,
  GetRoutePlanPreviewQuery,
  GetRouteQuery,
  GetRoutesQuery,
  StartRouteMutation,
  UpdateRouteAssignmentMutation,
} from "@/graphql/routes";
import { graphqlRequest } from "@/lib/network/graphql-client";
import type {
  CancelRouteRequest,
  CompleteRouteRequest,
  CreateRouteRequest,
  Route,
  RouteAssignmentCandidates,
  RoutePlanPreview,
  RoutePlanPreviewRequest,
  UpdateRouteAssignmentRequest,
} from "@/types/routes";

function mapRouteSummary(
  raw:
    | NonNullable<GetRoutesQuery["routes"]>[number]
    | NonNullable<CreateRouteMutation["createRoute"]>
    | NonNullable<UpdateRouteAssignmentMutation["updateRouteAssignment"]>
    | NonNullable<CancelRouteMutation["cancelRoute"]>
    | NonNullable<DispatchRouteMutation["dispatchRoute"]>
    | NonNullable<GetMyRoutesQuery["myRoutes"]>[number]
    | NonNullable<StartRouteMutation["startRoute"]>
    | NonNullable<CompleteRouteMutation["completeRoute"]>,
): Route {
  return {
    id: raw.id,
    zoneId: raw.zoneId,
    zoneName: raw.zoneName?.trim() || "Unknown zone",
    depotId: null,
    depotName: raw.depotName?.trim() || null,
    depotAddressLine: raw.depotAddressLine?.trim() || null,
    depotLongitude: null,
    depotLatitude: null,
    vehicleId: raw.vehicleId,
    vehiclePlate: raw.vehiclePlate?.trim() || "Unknown vehicle",
    driverId: raw.driverId,
    driverName: raw.driverName?.trim() || "Unknown driver",
    stagingArea: raw.stagingArea,
    startDate: raw.startDate,
    dispatchedAt: raw.dispatchedAt ?? null,
    endDate: raw.endDate ?? null,
    startMileage: raw.startMileage,
    endMileage: raw.endMileage,
    totalMileage: raw.totalMileage,
    status: raw.status,
    parcelCount: raw.parcelCount,
    parcelsDelivered: raw.parcelsDelivered,
    estimatedStopCount: raw.estimatedStopCount,
    plannedDistanceMeters: raw.plannedDistanceMeters,
    plannedDurationSeconds: raw.plannedDurationSeconds,
    createdAt: raw.createdAt,
    updatedAt: raw.updatedAt ?? null,
    cancellationReason:
      "cancellationReason" in raw ? raw.cancellationReason ?? null : null,
    path: [],
    stops: [],
    assignmentAuditTrail: [],
  };
}

function mapRouteDetail(
  raw: NonNullable<GetRouteQuery["route"]> | NonNullable<GetMyRouteQuery["myRoute"]>,
): Route {
  return {
    ...mapRouteSummary(raw),
    depotId: raw.depotId ?? null,
    depotName: raw.depotName?.trim() || null,
    depotAddressLine: raw.depotAddressLine?.trim() || null,
    depotLongitude: raw.depotLongitude ?? null,
    depotLatitude: raw.depotLatitude ?? null,
    path: raw.path ?? [],
    stops: raw.stops ?? [],
    assignmentAuditTrail: raw.assignmentAuditTrail ?? [],
  };
}

function mapRoutePlanPreview(raw: GetRoutePlanPreviewQuery["routePlanPreview"]): RoutePlanPreview {
  return {
    ...raw,
    candidateParcels: raw.candidateParcels ?? [],
    stops: raw.stops ?? [],
    path: raw.path ?? [],
    warnings: raw.warnings ?? [],
  };
}

export const routesService = {
  getAll: async (where?: RouteFilterInput): Promise<Route[]> => {
    const variables: Record<string, unknown> = {};
    if (where !== undefined) {
      variables.where = where;
    }

    const data = await graphqlRequest<GetRoutesQuery>(PAGINATED_ROUTES, variables);
    return data.routes.map(mapRouteSummary);
  },

  getById: async (id: string): Promise<Route> => {
    const data = await graphqlRequest<GetRouteQuery>(GET_ROUTE, { id });
    if (!data.route) {
      throw new Error("Route not found");
    }
    return mapRouteDetail(data.route);
  },

  getMine: async (): Promise<Route[]> => {
    const data = await graphqlRequest<GetMyRoutesQuery>(GET_MY_ROUTES);
    return data.myRoutes.map(mapRouteSummary);
  },

  getMyById: async (id: string): Promise<Route> => {
    const data = await graphqlRequest<GetMyRouteQuery>(GET_MY_ROUTE, { id });
    if (!data.myRoute) {
      throw new Error("Route not found");
    }
    return mapRouteDetail(data.myRoute);
  },

  create: async (data: CreateRouteRequest): Promise<Route> => {
    const result = await graphqlRequest<CreateRouteMutation>(CREATE_ROUTE, {
      input: {
        zoneId: data.zoneId,
        vehicleId: data.vehicleId,
        driverId: data.driverId,
        stagingArea: data.stagingArea,
        startDate: data.startDate,
        startMileage: data.startMileage,
        assignmentMode: data.assignmentMode,
        stopMode: data.stopMode,
        parcelIds: data.parcelIds,
        stops: data.stops,
      },
    });

    return mapRouteSummary(result.createRoute);
  },

  getAssignmentCandidates: async (
    serviceDate: string,
    zoneId: string,
    routeId?: string,
  ): Promise<RouteAssignmentCandidates> => {
    const data = await graphqlRequest<GetRouteAssignmentCandidatesQuery>(
      GET_ROUTE_ASSIGNMENT_CANDIDATES,
      {
        serviceDate,
        zoneId,
        routeId,
      },
    );

    return {
      vehicles: data.routeAssignmentCandidates?.vehicles ?? [],
      drivers: data.routeAssignmentCandidates?.drivers ?? [],
    };
  },

  getPlanPreview: async (request: RoutePlanPreviewRequest): Promise<RoutePlanPreview> => {
    const data = await graphqlRequest<GetRoutePlanPreviewQuery>(GET_ROUTE_PLAN_PREVIEW, {
      input: {
        zoneId: request.zoneId,
        vehicleId: request.vehicleId ?? undefined,
        driverId: request.driverId ?? undefined,
        startDate: request.startDate,
        assignmentMode: request.assignmentMode,
        stopMode: request.stopMode,
        parcelIds: request.parcelIds,
        stops: request.stops,
      },
    });

    return mapRoutePlanPreview(data.routePlanPreview);
  },

  updateAssignment: async (
    id: string,
    data: UpdateRouteAssignmentRequest,
  ): Promise<Route> => {
    const result = await graphqlRequest<UpdateRouteAssignmentMutation>(
      UPDATE_ROUTE_ASSIGNMENT,
      {
        id,
        input: {
          vehicleId: data.vehicleId,
          driverId: data.driverId,
        },
      },
    );

    if (!result.updateRouteAssignment) {
      throw new Error("Route not found");
    }

    return mapRouteSummary(result.updateRouteAssignment);
  },

  cancel: async (id: string, data: CancelRouteRequest): Promise<Route> => {
    const result = await graphqlRequest<CancelRouteMutation>(CANCEL_ROUTE, {
      id,
      input: {
        reason: data.reason,
      },
    });

    if (!result.cancelRoute) {
      throw new Error("Route not found");
    }

    return mapRouteSummary(result.cancelRoute);
  },

  dispatch: async (id: string): Promise<Route> => {
    const result = await graphqlRequest<DispatchRouteMutation>(DISPATCH_ROUTE, { id });
    if (!result.dispatchRoute) {
      throw new Error("Route not found");
    }

    return mapRouteSummary(result.dispatchRoute);
  },

  start: async (id: string): Promise<Route> => {
    const result = await graphqlRequest<StartRouteMutation>(START_ROUTE, { id });
    if (!result.startRoute) {
      throw new Error("Route not found");
    }

    return mapRouteSummary(result.startRoute);
  },

  complete: async (id: string, data: CompleteRouteRequest): Promise<Route> => {
    const result = await graphqlRequest<CompleteRouteMutation>(COMPLETE_ROUTE, {
      id,
      input: {
        endMileage: data.endMileage,
      },
    });

    if (!result.completeRoute) {
      throw new Error("Route not found");
    }

    return mapRouteSummary(result.completeRoute);
  },
};
