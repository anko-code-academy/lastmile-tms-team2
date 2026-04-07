export interface DepotStorageLayout {
  depotId: string;
  depotName: string;
  storageZones: StorageZone[];
}

export interface StorageZone {
  id: string;
  name: string;
  depotId: string;
  storageAisles: StorageAisle[];
}

export interface StorageAisle {
  id: string;
  name: string;
  storageZoneId: string;
  binLocations: BinLocation[];
}

export interface BinLocation {
  id: string;
  name: string;
  isActive: boolean;
  storageAisleId: string;
}

export interface CreateStorageZoneRequest {
  depotId: string;
  name: string;
}

export interface UpdateStorageZoneRequest {
  depotId: string;
  name: string;
}

export interface CreateStorageAisleRequest {
  storageZoneId: string;
  name: string;
}

export interface UpdateStorageAisleRequest {
  storageZoneId: string;
  name: string;
}

export interface CreateBinLocationRequest {
  storageAisleId: string;
  name: string;
  isActive: boolean;
}

export interface UpdateBinLocationRequest {
  storageAisleId: string;
  name: string;
  isActive: boolean;
}
