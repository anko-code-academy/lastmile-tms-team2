export type { VehicleType, VehicleStatus } from "@/graphql/generated";

export type Vehicle = {
  id: string;
  registrationPlate: string;
  type: import("@/graphql/generated").VehicleType;
  parcelCapacity: number;
  weightCapacity: number;
  status: import("@/graphql/generated").VehicleStatus;
  depotId: string;
  depotName: string | null;
  totalRoutes: number;
  routesCompleted: number;
  totalMileage: number;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateVehicleRequest = {
  registrationPlate: string;
  type: import("@/graphql/generated").VehicleType;
  parcelCapacity: number;
  weightCapacity: number;
  status: import("@/graphql/generated").VehicleStatus;
  depotId: string;
};

export type UpdateVehicleRequest = {
  registrationPlate: string;
  type: import("@/graphql/generated").VehicleType;
  parcelCapacity: number;
  weightCapacity: number;
  status: import("@/graphql/generated").VehicleStatus;
  depotId: string;
};
