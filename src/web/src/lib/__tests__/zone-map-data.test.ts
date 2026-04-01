import { describe, expect, it } from "vitest";
import {
  buildDepotMarkerData,
  buildViewportBounds,
  buildZoneOverlayData,
  parseZonePolygon,
} from "@/lib/zones/map-data";
import type { Depot } from "@/types/depots";
import type { Zone } from "@/types/zones";

const boundaryGeoJson =
  '{"type":"Polygon","coordinates":[[[145,-37.8],[145.1,-37.8],[145.1,-37.7],[145,-37.7],[145,-37.8]]]}';

const zones: Zone[] = [
  {
    id: "zone-1",
    name: "North",
    boundary: "POLYGON ((145 -37.8, 145.1 -37.8, 145.1 -37.7, 145 -37.7, 145 -37.8))",
    boundaryGeoJson,
    isActive: true,
    depotId: "depot-1",
    depotName: "Main Depot",
    createdAt: "2026-03-01T00:00:00Z",
    updatedAt: null,
  },
  {
    id: "zone-2",
    name: "South",
    boundary: "POLYGON ((145.2 -37.9, 145.3 -37.9, 145.3 -37.8, 145.2 -37.8, 145.2 -37.9))",
    boundaryGeoJson:
      '{"type":"Polygon","coordinates":[[[145.2,-37.9],[145.3,-37.9],[145.3,-37.8],[145.2,-37.8],[145.2,-37.9]]]}',
    isActive: true,
    depotId: "depot-1",
    depotName: "Main Depot",
    createdAt: "2026-03-01T00:00:00Z",
    updatedAt: null,
  },
];

const depots: Depot[] = [
  {
    id: "depot-1",
    name: "Main Depot",
    address: {
      street1: "1 Market Street",
      street2: null,
      city: "Melbourne",
      state: "VIC",
      postalCode: "3000",
      countryCode: "AU",
      isResidential: false,
      contactName: null,
      companyName: null,
      phone: null,
      email: null,
      geoLocation: {
        latitude: -37.8136,
        longitude: 144.9631,
      },
    },
    operatingHours: null,
    isActive: true,
    createdAt: "2026-03-01T00:00:00Z",
    updatedAt: null,
  },
];

describe("zone map data", () => {
  it("parses backend GeoJSON polygons", () => {
    expect(parseZonePolygon(boundaryGeoJson)?.type).toBe("Polygon");
  });

  it("builds overlay labels and excludes the editable zone", () => {
    const overlay = buildZoneOverlayData(zones, {
      editableZoneId: "zone-2",
      selectedZoneId: "zone-1",
    });

    expect(overlay.polygons.features).toHaveLength(1);
    expect(overlay.labels[0]?.name).toBe("North");
    expect(overlay.bounds).not.toBeNull();
  });

  it("deduplicates depot markers and includes draft geometry in bounds", () => {
    const overlay = buildZoneOverlayData(zones);
    const depotMarkers = buildDepotMarkerData(depots, zones, "depot-1", null);
    const bounds = buildViewportBounds(
      overlay,
      depotMarkers,
      parseZonePolygon(boundaryGeoJson),
    );

    expect(depotMarkers).toHaveLength(1);
    expect(bounds).not.toBeNull();
    expect(bounds?.[0]).toBeLessThan(bounds?.[2] ?? 0);
  });
});
