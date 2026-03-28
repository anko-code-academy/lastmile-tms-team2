import {
  CREATE_ZONE,
  DELETE_ZONE,
  UPDATE_ZONE,
  ZONE_BY_ID,
  ZONES_LIST,
} from "@/graphql/zones";
import type {
  GetZonesQuery,
  GetZoneQuery,
  CreateZoneMutation,
  UpdateZoneMutation,
} from "@/graphql/zones";
import { graphqlRequest } from "@/lib/network/graphql-client";
import type { CreateZoneRequest, UpdateZoneRequest, Zone } from "@/types/zones";

function toZone(z: { id: string; name: string; boundary?: string | null; isActive: boolean; depotId: string; depotName?: string | null; createdAt: string; updatedAt?: string | null }): Zone {
  return {
    id: z.id,
    name: z.name,
    boundary: z.boundary ?? "",
    isActive: z.isActive,
    depotId: z.depotId,
    depotName: z.depotName ?? null,
    createdAt: z.createdAt,
    updatedAt: z.updatedAt ?? null,
  };
}

export const zonesService = {
  list: async (): Promise<Zone[]> => {
    const data = await graphqlRequest<GetZonesQuery>(ZONES_LIST);
    return data.zones.map(toZone);
  },

  getById: async (id: string): Promise<Zone> => {
    const data = await graphqlRequest<GetZoneQuery>(ZONE_BY_ID, { id });
    if (!data.zone) {
      throw new Error("Zone not found");
    }
    return toZone(data.zone);
  },

  create: async (req: CreateZoneRequest): Promise<Zone> => {
    const input: Record<string, unknown> = {
      name: req.name,
      depotId: req.depotId,
      isActive: req.isActive,
    };

    if (req.boundaryInput.geoJson) {
      input.geoJson = req.boundaryInput.geoJson;
    } else if (req.boundaryInput.coordinates) {
      input.coordinates = req.boundaryInput.coordinates;
    } else if (req.boundaryInput.boundaryWkt) {
      input.boundaryWkt = req.boundaryInput.boundaryWkt;
    }

    const data = await graphqlRequest<CreateZoneMutation>(CREATE_ZONE, {
      input,
    });
    return toZone(data.createZone);
  },

  update: async (id: string, req: UpdateZoneRequest): Promise<Zone> => {
    const input: Record<string, unknown> = {
      name: req.name,
      depotId: req.depotId,
      isActive: req.isActive,
    };

    if (req.boundaryInput.geoJson) {
      input.geoJson = req.boundaryInput.geoJson;
    } else if (req.boundaryInput.coordinates) {
      input.coordinates = req.boundaryInput.coordinates;
    } else if (req.boundaryInput.boundaryWkt) {
      input.boundaryWkt = req.boundaryInput.boundaryWkt;
    }

    const data = await graphqlRequest<UpdateZoneMutation>(UPDATE_ZONE, {
      id,
      input,
    });
    if (!data.updateZone) {
      throw new Error("Zone not found");
    }
    return toZone(data.updateZone);
  },

  delete: async (id: string): Promise<void> => {
    await graphqlRequest<{ deleteZone: boolean }>(DELETE_ZONE, { id });
  },
};
