import {
  CREATE_DEPOT,
  DELETE_DEPOT,
  DEPOT_BY_ID,
  DEPOTS_LIST,
  UPDATE_DEPOT,
} from "@/graphql/depots";
import { normalizeDepot, serializeDepotOperatingHours } from "@/lib/depots/operating-hours";
import { graphqlRequest } from "@/lib/network/graphql-client";
import { mockDepots } from "@/mocks/depots.mock";
import type { CreateDepotRequest, Depot, UpdateDepotRequest } from "@/types/depots";

const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK_DATA === "true";
type GraphQLDepot = Parameters<typeof normalizeDepot>[0];

export const depotsService = {
  list: async (): Promise<Depot[]> => {
    if (USE_MOCK) {
      return mockDepots.map((depot) => ({
        id: depot.id,
        name: depot.name,
        addressId: depot.id === "00000000-0000-0000-0000-000000000001"
          ? "00000000-0000-0000-0000-000000000002"
          : depot.id,
        address: null,
        operatingHours: null,
        isActive: true,
        createdAt: new Date(0).toISOString(),
        updatedAt: null,
      }));
    }

    const data = await graphqlRequest<{ depots: GraphQLDepot[] }>(DEPOTS_LIST);
    return data.depots.map(normalizeDepot);
  },

  getById: async (id: string): Promise<Depot> => {
    const data = await graphqlRequest<{ depot: GraphQLDepot | null }>(DEPOT_BY_ID, {
      id,
    });
    if (!data.depot) {
      throw new Error("Depot not found");
    }
    return normalizeDepot(data.depot);
  },

  create: async (req: CreateDepotRequest): Promise<Depot> => {
    const data = await graphqlRequest<{ createDepot: GraphQLDepot }>(CREATE_DEPOT, {
      input: {
        name: req.name,
        address: {
          street1: req.address.street1,
          street2: req.address.street2 ?? null,
          city: req.address.city,
          state: req.address.state,
          postalCode: req.address.postalCode,
          countryCode: req.address.countryCode,
          isResidential: req.address.isResidential,
          contactName: req.address.contactName ?? null,
          companyName: req.address.companyName ?? null,
          phone: req.address.phone ?? null,
          email: req.address.email ?? null,
        },
        operatingHours: serializeDepotOperatingHours(req.operatingHours) ?? [],
        isActive: req.isActive,
      },
    });
    return normalizeDepot(data.createDepot);
  },

  update: async (id: string, req: UpdateDepotRequest): Promise<Depot> => {
    const input: Record<string, unknown> = {
      name: req.name,
      isActive: req.isActive,
    };

    if (req.address) {
      input.address = {
        street1: req.address.street1,
        street2: req.address.street2 ?? null,
        city: req.address.city,
        state: req.address.state,
        postalCode: req.address.postalCode,
        countryCode: req.address.countryCode,
        isResidential: req.address.isResidential,
        contactName: req.address.contactName ?? null,
        companyName: req.address.companyName ?? null,
        phone: req.address.phone ?? null,
        email: req.address.email ?? null,
      };
    }

    if (req.operatingHours) {
      input.operatingHours = serializeDepotOperatingHours(req.operatingHours);
    }

    const data = await graphqlRequest<{ updateDepot: GraphQLDepot | null }>(
      UPDATE_DEPOT,
      { id, input }
    );
    if (!data.updateDepot) {
      throw new Error("Depot not found");
    }
    return normalizeDepot(data.updateDepot);
  },

  delete: async (id: string): Promise<void> => {
    await graphqlRequest<{ deleteDepot: boolean }>(DELETE_DEPOT, { id });
  },
};
