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
import {
  getMockVehiclesPaginated,
  getMockVehicleById,
} from "@/mocks/vehicles.mock";

const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK_DATA === "true";

export const vehiclesService = {
  getAll: async (
    where?: VehicleFilterInput
  ): Promise<Vehicle[]> => {
    if (USE_MOCK) {
      return Promise.resolve(
        getMockVehiclesPaginated(1, 1000).items,
      );
    }

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
    if (USE_MOCK) {
      const vehicle = getMockVehicleById(id);
      if (!vehicle) throw new Error("Vehicle not found");
      return Promise.resolve(vehicle);
    }

    const vehicles = await vehiclesService.getAll();
    const vehicle = vehicles.find((v) => v.id === id);
    if (!vehicle) throw new Error("Vehicle not found");
    return vehicle;
  },

  create: async (data: CreateVehicleRequest): Promise<Vehicle> => {
    if (USE_MOCK) {
      const newVehicle: Vehicle = {
        ...data,
        id: `mock-${Date.now()}`,
        depotName: "Test Depot",
        totalRoutes: 0,
        routesCompleted: 0,
        totalMileage: 0,
        createdAt: new Date().toISOString(),
        updatedAt: null,
      };
      return Promise.resolve(newVehicle);
    }

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
    if (USE_MOCK) {
      const existing = getMockVehicleById(id);
      if (!existing) throw new Error("Vehicle not found");
      const updated: Vehicle = {
        ...existing,
        ...data,
        depotName: existing.depotName,
        updatedAt: new Date().toISOString(),
      };
      return Promise.resolve(updated);
    }

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
    if (USE_MOCK) {
      if (!getMockVehicleById(id)) throw new Error("Vehicle not found");
      return Promise.resolve(true);
    }

    const res = await graphqlRequest<{ deleteVehicle: boolean }>(
      DELETE_VEHICLE,
      { id }
    );
    return res.deleteVehicle;
  },
};
