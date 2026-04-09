export type {
  DriverStatus,
  RouteAssignmentAuditAction,
  RouteStatus,
  StagingArea,
  VehicleStatus,
} from "@/graphql/generated";

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

export type RouteAssignmentAuditEntry = {
  id: string;
  action: import("@/graphql/generated").RouteAssignmentAuditAction;
  previousDriverId: string | null;
  previousDriverName: string | null;
  newDriverId: string;
  newDriverName: string;
  previousVehicleId: string | null;
  previousVehiclePlate: string | null;
  newVehicleId: string;
  newVehiclePlate: string;
  changedAt: string;
  changedBy: string | null;
};

export type DriverWorkloadRoute = {
  routeId: string;
  vehicleId: string;
  vehiclePlate: string;
  startDate: string;
  status: import("@/graphql/generated").RouteStatus;
};

export type AssignableDriver = {
  id: string;
  displayName: string;
  depotId: string;
  zoneId: string;
  status: import("@/graphql/generated").DriverStatus;
  isCurrentAssignment: boolean;
  workloadRoutes: DriverWorkloadRoute[];
};

export type AssignableVehicle = {
  id: string;
  registrationPlate: string;
  depotId: string;
  depotName: string | null;
  parcelCapacity: number;
  weightCapacity: number;
  status: import("@/graphql/generated").VehicleStatus;
  isCurrentAssignment: boolean;
};

export type RouteAssignmentCandidates = {
  vehicles: AssignableVehicle[];
  drivers: AssignableDriver[];
};
