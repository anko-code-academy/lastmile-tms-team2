export interface Zone {
  id: string;
  name: string;
  /** WKT polygon boundary, e.g. "POLYGON ((lon lat, ...))" */
  boundary: string;
  boundaryGeoJson: string | null;
  isActive: boolean;
  depotId: string;
  depotName: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateZoneRequest {
  name: string;
  depotId: string;
  isActive: boolean;
  boundaryGeoJson: string;
}

export interface UpdateZoneRequest {
  name: string;
  depotId: string;
  isActive: boolean;
  boundaryGeoJson: string;
}
