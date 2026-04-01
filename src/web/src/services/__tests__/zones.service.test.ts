import { beforeEach, describe, expect, it, vi } from "vitest";
import { zonesService } from "../zones.service";

vi.mock("@/lib/network/graphql-client", () => ({
  graphqlRequest: vi.fn(),
}));

import { graphqlRequest } from "@/lib/network/graphql-client";

const mockGraphql = graphqlRequest as ReturnType<typeof vi.fn>;

describe("zonesService", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("normalizes boundaryGeoJson when listing zones", async () => {
    mockGraphql.mockResolvedValueOnce({
      zones: [
        {
          id: "zone-1",
          name: "North",
          boundary:
            "POLYGON ((145 -37.8, 145.1 -37.8, 145.1 -37.7, 145 -37.7, 145 -37.8))",
          boundaryGeoJson:
            '{"type":"Polygon","coordinates":[[[145,-37.8],[145.1,-37.8],[145.1,-37.7],[145,-37.7],[145,-37.8]]]}',
          isActive: true,
          depotId: "depot-1",
          depotName: "Main Depot",
          createdAt: "2026-03-01T00:00:00Z",
          updatedAt: null,
        },
      ],
    });

    const result = await zonesService.list();

    expect(result).toHaveLength(1);
    expect(result[0].boundaryGeoJson).toContain('"type":"Polygon"');
    expect(result[0].boundary).toContain("POLYGON");
  });

  it("submits GeoJSON-only payloads for create", async () => {
    mockGraphql.mockResolvedValueOnce({
      createZone: {
        id: "zone-2",
        name: "West",
        boundary:
          "POLYGON ((145 -37.8, 145.1 -37.8, 145.1 -37.7, 145 -37.7, 145 -37.8))",
        boundaryGeoJson:
          '{"type":"Polygon","coordinates":[[[145,-37.8],[145.1,-37.8],[145.1,-37.7],[145,-37.7],[145,-37.8]]]}',
        isActive: true,
        depotId: "depot-1",
        depotName: "Main Depot",
        createdAt: "2026-03-01T00:00:00Z",
        updatedAt: null,
      },
    });

    await zonesService.create({
      name: "West",
      depotId: "depot-1",
      isActive: true,
      boundaryGeoJson:
        '{"type":"Polygon","coordinates":[[[145,-37.8],[145.1,-37.8],[145.1,-37.7],[145,-37.7],[145,-37.8]]]}',
    });

    expect(mockGraphql).toHaveBeenCalledWith(expect.any(Object), {
      input: {
        name: "West",
        depotId: "depot-1",
        isActive: true,
        geoJson:
          '{"type":"Polygon","coordinates":[[[145,-37.8],[145.1,-37.8],[145.1,-37.7],[145,-37.7],[145,-37.8]]]}',
      },
    });
  });
});
