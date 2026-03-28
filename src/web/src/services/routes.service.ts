import {
  CREATE_ROUTE,
  PAGINATED_ROUTES,
} from "@/graphql/routes";
import type {
  GetRoutesQuery,
  CreateRouteMutation,
} from "@/graphql/routes";
import type { RouteFilterInput } from "@/graphql/generated";
import { graphqlRequest } from "@/lib/network/graphql-client";
import type {
  Route,
  CreateRouteRequest,
} from "@/types/routes";
import {
  getMockRouteById,
  mockRoutes,
} from "@/mocks/routes.mock";

const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK_DATA === "true";

function mapRoute(raw: NonNullable<GetRoutesQuery["routes"]>[number]): Route {
  return {
    id: raw.id,
    vehicleId: raw.vehicleId,
    vehiclePlate: raw.vehiclePlate ?? "Unknown vehicle",
    driverId: raw.driverId,
    driverName: raw.driverName ?? "Unknown driver",
    startDate: raw.startDate,
    endDate: raw.endDate ?? null,
    startMileage: raw.startMileage,
    endMileage: raw.endMileage,
    totalMileage: raw.totalMileage,
    status: raw.status,
    parcelCount: raw.parcelCount,
    parcelsDelivered: raw.parcelsDelivered,
    createdAt: raw.createdAt,
  };
}

export const routesService = {
  getAll: async (
    where?: RouteFilterInput
  ): Promise<Route[]> => {
    if (USE_MOCK) {
      let items = [...mockRoutes];
      if (where?.status?.eq !== undefined) {
        items = items.filter((r) => r.status === where.status!.eq);
      }
      if (where?.vehicleId?.eq !== undefined) {
        items = items.filter((r) => r.vehicleId === where.vehicleId!.eq);
      }
      return Promise.resolve(items);
    }

    const variables: Record<string, unknown> = {};
    if (where !== undefined) {
      variables.where = where;
    }

    const data = await graphqlRequest<GetRoutesQuery>(
      PAGINATED_ROUTES,
      variables
    );
    return data.routes.map(mapRoute);
  },

  getById: async (id: string): Promise<Route> => {
    if (USE_MOCK) {
      const route = getMockRouteById(id);
      if (!route) throw new Error("Route not found");
      return Promise.resolve(route);
    }

    const routes = await routesService.getAll();
    const route = routes.find((r) => r.id === id);
    if (!route) throw new Error("Route not found");
    return route;
  },

  create: async (data: CreateRouteRequest): Promise<Route> => {
    if (USE_MOCK) {
      const newRoute: Route = {
        id: `mock-${Date.now()}`,
        vehicleId: data.vehicleId,
        vehiclePlate: "Mock Vehicle",
        driverId: data.driverId,
        driverName: "Mock Driver",
        startDate: data.startDate,
        endDate: null,
        startMileage: data.startMileage,
        endMileage: 0,
        totalMileage: 0,
        status: "PLANNED",
        parcelCount: data.parcelIds.length,
        parcelsDelivered: 0,
        createdAt: new Date().toISOString(),
      };
      return Promise.resolve(newRoute);
    }

    const res = await graphqlRequest<CreateRouteMutation>(
      CREATE_ROUTE,
      {
        input: {
          vehicleId: data.vehicleId,
          driverId: data.driverId,
          startDate: data.startDate,
          startMileage: data.startMileage,
          parcelIds: data.parcelIds,
        },
      }
    );
    return mapRoute(res.createRoute);
  },
};
