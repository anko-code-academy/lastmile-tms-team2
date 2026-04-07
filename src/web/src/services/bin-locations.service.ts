import {
  CREATE_BIN_LOCATION,
  CREATE_STORAGE_AISLE,
  CREATE_STORAGE_ZONE,
  DELETE_BIN_LOCATION,
  DELETE_STORAGE_AISLE,
  DELETE_STORAGE_ZONE,
  GET_DEPOT_STORAGE_LAYOUT,
  UPDATE_BIN_LOCATION,
  UPDATE_STORAGE_AISLE,
  UPDATE_STORAGE_ZONE,
} from "@/graphql/bin-locations";
import type {
  CreateBinLocationMutation,
  CreateStorageAisleMutation,
  CreateStorageZoneMutation,
  GetDepotStorageLayoutQuery,
  UpdateBinLocationMutation,
  UpdateStorageAisleMutation,
  UpdateStorageZoneMutation,
} from "@/graphql/bin-locations";
import { graphqlRequest } from "@/lib/network/graphql-client";
import type {
  BinLocation,
  CreateBinLocationRequest,
  CreateStorageAisleRequest,
  CreateStorageZoneRequest,
  DepotStorageLayout,
  StorageAisle,
  StorageZone,
  UpdateBinLocationRequest,
  UpdateStorageAisleRequest,
  UpdateStorageZoneRequest,
} from "@/types/bin-locations";

function toBinLocation(bin: {
  id: string;
  name: string;
  isActive: boolean;
  storageAisleId: string;
}): BinLocation {
  return {
    id: bin.id,
    name: bin.name,
    isActive: bin.isActive,
    storageAisleId: bin.storageAisleId,
  };
}

function toStorageAisle(aisle: {
  id: string;
  name: string;
  storageZoneId: string;
  binLocations?: Array<{
    id: string;
    name: string;
    isActive: boolean;
    storageAisleId: string;
  }> | null;
}): StorageAisle {
  return {
    id: aisle.id,
    name: aisle.name,
    storageZoneId: aisle.storageZoneId,
    binLocations: aisle.binLocations?.map(toBinLocation) ?? [],
  };
}

function toStorageZone(zone: {
  id: string;
  name: string;
  depotId: string;
  storageAisles?: Array<{
    id: string;
    name: string;
    storageZoneId: string;
    binLocations?: Array<{
      id: string;
      name: string;
      isActive: boolean;
      storageAisleId: string;
    }> | null;
  }> | null;
}): StorageZone {
  return {
    id: zone.id,
    name: zone.name,
    depotId: zone.depotId,
    storageAisles: zone.storageAisles?.map(toStorageAisle) ?? [],
  };
}

function toDepotStorageLayout(layout: NonNullable<GetDepotStorageLayoutQuery["depotStorageLayout"]>): DepotStorageLayout {
  return {
    depotId: layout.depotId,
    depotName: layout.depotName,
    storageZones: layout.storageZones.map(toStorageZone),
  };
}

export const binLocationsService = {
  getDepotStorageLayout: async (depotId: string): Promise<DepotStorageLayout | null> => {
    const data = await graphqlRequest<GetDepotStorageLayoutQuery>(GET_DEPOT_STORAGE_LAYOUT, {
      depotId,
    });

    return data.depotStorageLayout
      ? toDepotStorageLayout(data.depotStorageLayout)
      : null;
  },

  createStorageZone: async (req: CreateStorageZoneRequest): Promise<StorageZone> => {
    const data = await graphqlRequest<CreateStorageZoneMutation>(CREATE_STORAGE_ZONE, {
      input: {
        depotId: req.depotId,
        name: req.name,
      },
    });

    return toStorageZone(data.createStorageZone);
  },

  updateStorageZone: async (id: string, req: UpdateStorageZoneRequest): Promise<StorageZone> => {
    const data = await graphqlRequest<UpdateStorageZoneMutation>(UPDATE_STORAGE_ZONE, {
      id,
      input: {
        depotId: req.depotId,
        name: req.name,
      },
    });

    if (!data.updateStorageZone) {
      throw new Error("Storage zone not found");
    }

    return toStorageZone(data.updateStorageZone);
  },

  deleteStorageZone: async (id: string): Promise<void> => {
    await graphqlRequest(DELETE_STORAGE_ZONE, { id });
  },

  createStorageAisle: async (req: CreateStorageAisleRequest): Promise<StorageAisle> => {
    const data = await graphqlRequest<CreateStorageAisleMutation>(CREATE_STORAGE_AISLE, {
      input: {
        storageZoneId: req.storageZoneId,
        name: req.name,
      },
    });

    return toStorageAisle(data.createStorageAisle);
  },

  updateStorageAisle: async (id: string, req: UpdateStorageAisleRequest): Promise<StorageAisle> => {
    const data = await graphqlRequest<UpdateStorageAisleMutation>(UPDATE_STORAGE_AISLE, {
      id,
      input: {
        storageZoneId: req.storageZoneId,
        name: req.name,
      },
    });

    if (!data.updateStorageAisle) {
      throw new Error("Storage aisle not found");
    }

    return toStorageAisle(data.updateStorageAisle);
  },

  deleteStorageAisle: async (id: string): Promise<void> => {
    await graphqlRequest(DELETE_STORAGE_AISLE, { id });
  },

  createBinLocation: async (req: CreateBinLocationRequest): Promise<BinLocation> => {
    const data = await graphqlRequest<CreateBinLocationMutation>(CREATE_BIN_LOCATION, {
      input: {
        storageAisleId: req.storageAisleId,
        name: req.name,
        isActive: req.isActive,
      },
    });

    return toBinLocation(data.createBinLocation);
  },

  updateBinLocation: async (id: string, req: UpdateBinLocationRequest): Promise<BinLocation> => {
    const data = await graphqlRequest<UpdateBinLocationMutation>(UPDATE_BIN_LOCATION, {
      id,
      input: {
        storageAisleId: req.storageAisleId,
        name: req.name,
        isActive: req.isActive,
      },
    });

    if (!data.updateBinLocation) {
      throw new Error("Bin location not found");
    }

    return toBinLocation(data.updateBinLocation);
  },

  deleteBinLocation: async (id: string): Promise<void> => {
    await graphqlRequest(DELETE_BIN_LOCATION, { id });
  },
};
