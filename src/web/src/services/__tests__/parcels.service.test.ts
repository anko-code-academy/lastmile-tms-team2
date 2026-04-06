import { beforeEach, describe, expect, it, vi } from "vitest";

import { ParcelWeightUnit } from "@/types/parcels";
import { parcelsService } from "../parcels.service";

vi.mock("@/lib/network/graphql-client", () => ({
  graphqlRequest: vi.fn(),
}));

import { graphqlRequest } from "@/lib/network/graphql-client";

const mockGraphql = graphqlRequest as ReturnType<typeof vi.fn>;

describe("parcelsService", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getPreLoadParcelsPage", () => {
    it("requests cursor pagination variables and returns the connection payload", async () => {
      mockGraphql.mockResolvedValueOnce({
        preLoadParcelsConnection: {
          totalCount: 9,
          pageInfo: {
            hasNextPage: true,
            hasPreviousPage: false,
            startCursor: "cursor-1",
            endCursor: "cursor-2",
          },
          nodes: [
            {
              id: "parcel-1",
              trackingNumber: "LMTESTSEED0001",
              status: "Sorted",
              serviceType: "Standard",
              weight: 2.5,
              weightUnit: "KG",
              parcelType: "Box",
              createdAt: "2026-04-06T10:00:00Z",
              zoneName: "Test Zone",
              estimatedDeliveryDate: "2026-04-08T00:00:00Z",
              recipientContactName: "Sam Seed",
              recipientCompanyName: null,
              recipientStreet1: "99 Recipient Rd",
              recipientCity: "Sydney",
              recipientPostalCode: "2001",
            },
          ],
        },
      });

      const result = await parcelsService.getPreLoadParcelsPage(
        "LMTEST",
        { status: { in: ["SORTED"] } },
        [{ trackingNumber: "ASC" }],
        20,
        "cursor-1",
      );

      expect(mockGraphql).toHaveBeenCalledWith(
        expect.any(Object),
        expect.objectContaining({
          search: "LMTEST",
          where: { status: { in: ["SORTED"] } },
          order: [{ trackingNumber: "ASC" }],
          first: 20,
          after: "cursor-1",
        }),
      );
      expect(result.totalCount).toBe(9);
      expect(result.pageInfo.hasNextPage).toBe(true);
      expect(result.nodes).toHaveLength(1);
      expect(result.nodes[0].zoneName).toBe("Test Zone");
    });
  });

  describe("getForRouteCreation", () => {
    it("filters by selected vehicle and driver and normalizes weight units", async () => {
      mockGraphql.mockResolvedValueOnce({
        parcelsForRouteCreation: [
          {
            id: "parcel-1",
            trackingNumber: "LMTESTSEED0001",
            weight: 2.5,
            weightUnit: "LB",
            zoneId: "zone-1",
            zoneName: "Test Zone",
          },
        ],
      });

      const result = await parcelsService.getForRouteCreation(
        "vehicle-1",
        "driver-1",
      );

      expect(mockGraphql).toHaveBeenCalledWith(
        expect.any(Object),
        {
          vehicleId: "vehicle-1",
          driverId: "driver-1",
        },
      );
      expect(result).toEqual([
        expect.objectContaining({
          id: "parcel-1",
          trackingNumber: "LMTESTSEED0001",
          weightUnit: ParcelWeightUnit.Lb,
          zoneName: "Test Zone",
        }),
      ]);
    });
  });
});
