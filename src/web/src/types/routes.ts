import type {
  GetRouteAssignmentCandidatesQuery,
  GetRouteQuery,
} from "@/graphql/routes";

export type {
  DriverStatus,
  RouteAssignmentAuditAction,
  RouteStatus,
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

export type Route = {
  id: string;
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
  createdAt: string;
  updatedAt: string | null;
  cancellationReason: string | null;
  assignmentAuditTrail: RouteAssignmentAuditEntry[];
};

export type CreateRouteRequest = {
  vehicleId: string;
  driverId: string;
  stagingArea: import("@/graphql/generated").StagingArea;
  startDate: string;
  startMileage: number;
  parcelIds: string[];
};

export type UpdateRouteAssignmentRequest = {
  vehicleId: string;
  driverId: string;
};

export type CancelRouteRequest = {
  reason: string;
};
