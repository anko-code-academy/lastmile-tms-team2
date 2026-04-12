import type {
  GetRouteAssignmentCandidatesQuery,
  GetRoutePlanPreviewQuery,
  GetRouteQuery,
} from "@/graphql/routes";
import type {
  CancelRouteInput,
  CompleteRouteInput,
  CreateRouteInput,
  RoutePlanPreviewInput,
  RouteStopDraftInput,
  UpdateRouteAssignmentInput,
} from "@/graphql/generated";

export type {
  DriverStatus,
  ParcelStatus,
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

type RawRoute = NonNullable<GetRouteQuery["route"]>;

export type Route = Omit<
  RawRoute,
  | "__typename"
  | "zoneName"
  | "depotId"
  | "depotName"
  | "depotAddressLine"
  | "depotLongitude"
  | "depotLatitude"
  | "vehiclePlate"
  | "driverName"
  | "endDate"
  | "dispatchedAt"
  | "updatedAt"
  | "cancellationReason"
  | "path"
  | "stops"
  | "assignmentAuditTrail"
> & {
  zoneName: string;
  depotId: string | null;
  depotName: string | null;
  depotAddressLine: string | null;
  depotLongitude: number | null;
  depotLatitude: number | null;
  vehiclePlate: string;
  driverName: string;
  dispatchedAt: string | null;
  endDate: string | null;
  updatedAt: string | null;
  cancellationReason: string | null;
  path: RoutePathPoint[];
  stops: RouteStop[];
  assignmentAuditTrail: RouteAssignmentAuditEntry[];
};

export type DispatchMapStopStatus = "WAITING" | "DELIVERED" | "FAILED";

export type DispatchMapStop = RouteStop & {
  uiStatus: DispatchMapStopStatus;
};

export type DispatchMapRoute = Omit<Route, "stops"> & {
  stops: DispatchMapStop[];
  hasGeometry: boolean;
  hasPathGeometry: boolean;
  hasStopGeometry: boolean;
  hasDepotGeometry: boolean;
};

export type RouteStopDraft = RouteStopDraftInput;

export type CreateRouteRequest = CreateRouteInput;

export type RoutePlanPreviewRequest = RoutePlanPreviewInput;

export type UpdateRouteAssignmentRequest = UpdateRouteAssignmentInput;

export type CancelRouteRequest = CancelRouteInput;

export type CompleteRouteRequest = CompleteRouteInput;
