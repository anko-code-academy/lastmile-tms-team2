import {
  CREATE_VEHICLE,
  DELETE_VEHICLE,
  PAGINATED_VEHICLES,
  UPDATE_VEHICLE,
} from "@/graphql/vehicles";
import type {
  GetVehiclesQuery,
  CreateVehicleMutation,
  UpdateVehicleMutation,
} from "@/graphql/vehicles";
import type { VehicleFilterInput } from "@/graphql/generated";
import { graphqlRequest } from "@/lib/network/graphql-client";
import type {
  Vehicle,
  CreateVehicleRequest,
  UpdateVehicleRequest,
} from "@/types/vehicles";

export const vehiclesService = {
  getAll: async (
    where?: VehicleFilterInput
  ): Promise<Vehicle[]> => {
    const variables: Record<string, unknown> = {};
    if (where !== undefined) {
      variables.where = where;
    }

    const data = await graphqlRequest<GetVehiclesQuery>(
      PAGINATED_VEHICLES,
      variables
    );
    return data.vehicles.map((v) => ({
      id: v.id,
      registrationPlate: v.registrationPlate,
      type: v.type,
      parcelCapacity: v.parcelCapacity,
      weightCapacity: v.weightCapacity,
      status: v.status,
      depotId: v.depotId,
      depotName: v.depotName ?? null,
      totalRoutes: v.totalRoutes,
      routesCompleted: v.routesCompleted,
      totalMileage: v.totalMileage,
      createdAt: v.createdAt,
      updatedAt: v.updatedAt ?? null,
    }));
  },

  getById: async (id: string): Promise<Vehicle> => {
    const vehicles = await vehiclesService.getAll();
    const vehicle = vehicles.find((v) => v.id === id);
    if (!vehicle) throw new Error("Vehicle not found");
    return vehicle;
  },

  create: async (data: CreateVehicleRequest): Promise<Vehicle> => {
    const res = await graphqlRequest<CreateVehicleMutation>(
      CREATE_VEHICLE,
      {
        input: {
          registrationPlate: data.registrationPlate,
          type: data.type,
          parcelCapacity: data.parcelCapacity,
          weightCapacity: data.weightCapacity,
          status: data.status,
          depotId: data.depotId,
        },
      }
    );
    const v = res.createVehicle;
    return {
      id: v.id,
      registrationPlate: v.registrationPlate,
      type: v.type,
      parcelCapacity: v.parcelCapacity,
      weightCapacity: v.weightCapacity,
      status: v.status,
      depotId: v.depotId,
      depotName: v.depotName ?? null,
      totalRoutes: v.totalRoutes,
      routesCompleted: v.routesCompleted,
      totalMileage: v.totalMileage,
      createdAt: v.createdAt,
      updatedAt: v.updatedAt ?? null,
    };
  },

  update: async (id: string, data: UpdateVehicleRequest): Promise<Vehicle> => {
    const res = await graphqlRequest<UpdateVehicleMutation>(
      UPDATE_VEHICLE,
      {
        id,
        input: {
          registrationPlate: data.registrationPlate,
          type: data.type,
          parcelCapacity: data.parcelCapacity,
          weightCapacity: data.weightCapacity,
          status: data.status,
          depotId: data.depotId,
        },
      }
    );
    if (!res.updateVehicle) throw new Error("Vehicle not found");
    const v = res.updateVehicle;
    return {
      id: v.id,
      registrationPlate: v.registrationPlate,
      type: v.type,
      parcelCapacity: v.parcelCapacity,
      weightCapacity: v.weightCapacity,
      status: v.status,
      depotId: v.depotId,
      depotName: v.depotName ?? null,
      totalRoutes: v.totalRoutes,
      routesCompleted: v.routesCompleted,
      totalMileage: v.totalMileage,
      createdAt: v.createdAt,
      updatedAt: v.updatedAt ?? null,
    };
  },

  delete: async (id: string): Promise<boolean> => {
    const res = await graphqlRequest<{ deleteVehicle: boolean }>(
      DELETE_VEHICLE,
      { id }
    );
    return res.deleteVehicle;
  },
};
