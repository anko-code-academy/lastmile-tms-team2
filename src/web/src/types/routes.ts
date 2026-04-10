import type {
  GetRouteAssignmentCandidatesQuery,
  GetRoutePlanPreviewQuery,
  GetRouteQuery,
} from "@/graphql/routes";

export type {
  DriverStatus,
  RouteAssignmentAuditAction,
  RouteAssignmentMode,
  RouteStatus,
  RouteStopMode,
  StagingArea,
  VehicleStatus,
} from "@/graphql/generated";

export type RouteAssignmentAuditEntry =
  NonNullable<NonNullable<GetRouteQuery["route"]>["assignmentAuditTrail"]>[number];

export type DriverWorkloadRoute =
  GetRouteAssignmentCandidatesQuery["routeAssignmentCandidates"]["drivers"][number]["workloadRoutes"][number];

export type AssignableDriver =
  GetRouteAssignmentCandidatesQuery["routeAssignmentCandidates"]["drivers"][number];

export type AssignableVehicle =
  GetRouteAssignmentCandidatesQuery["routeAssignmentCandidates"]["vehicles"][number];

export type RouteAssignmentCandidates =
  GetRouteAssignmentCandidatesQuery["routeAssignmentCandidates"];

export type RouteStopParcel =
  NonNullable<NonNullable<GetRouteQuery["route"]>["stops"]>[number]["parcels"][number];

export type RouteStop =
  NonNullable<NonNullable<GetRouteQuery["route"]>["stops"]>[number];

export type RoutePathPoint =
  NonNullable<NonNullable<GetRouteQuery["route"]>["path"]>[number];

export type RoutePlanCandidateParcel =
  GetRoutePlanPreviewQuery["routePlanPreview"]["candidateParcels"][number];

export type RoutePlanPreview = GetRoutePlanPreviewQuery["routePlanPreview"];

export type RouteStopDraft = {
  sequence: number;
  parcelIds: string[];
};

export type Route = {
  id: string;
  zoneId: string;
  zoneName: string;
  depotId: string | null;
  depotName: string | null;
  depotAddressLine: string | null;
  depotLongitude: number | null;
  depotLatitude: number | null;
  vehicleId: string;
  vehiclePlate: string;
  driverId: string;
  driverName: string;
  stagingArea: import("@/graphql/generated").StagingArea;
  startDate: string;
  endDate: string | null;
  startMileage: number;
  endMileage: number;
  totalMileage: number;
  status: import("@/graphql/generated").RouteStatus;
  parcelCount: number;
  parcelsDelivered: number;
  estimatedStopCount: number;
  plannedDistanceMeters: number;
  plannedDurationSeconds: number;
  createdAt: string;
  updatedAt: string | null;
  cancellationReason: string | null;
  path: RoutePathPoint[];
  stops: RouteStop[];
  assignmentAuditTrail: RouteAssignmentAuditEntry[];
};

export type CreateRouteRequest = {
  zoneId: string;
  vehicleId: string;
  driverId: string;
  stagingArea: import("@/graphql/generated").StagingArea;
  startDate: string;
  startMileage: number;
  assignmentMode: import("@/graphql/generated").RouteAssignmentMode;
  stopMode: import("@/graphql/generated").RouteStopMode;
  parcelIds: string[];
  stops: RouteStopDraft[];
};

export type RoutePlanPreviewRequest = {
  zoneId: string;
  vehicleId?: string | null;
  driverId?: string | null;
  startDate: string;
  assignmentMode: import("@/graphql/generated").RouteAssignmentMode;
  stopMode: import("@/graphql/generated").RouteStopMode;
  parcelIds: string[];
  stops: RouteStopDraft[];
};

export type UpdateRouteAssignmentRequest = {
  vehicleId: string;
  driverId: string;
};

export type CancelRouteRequest = {
  reason: string;
};

export type CompleteRouteRequest = {
  endMileage: number;
};
