import { beforeEach, describe, expect, it, vi } from "vitest";

import { routesService } from "../routes.service";

vi.mock("@/lib/network/graphql-client", () => ({
  graphqlRequest: vi.fn(),
}));

import { graphqlRequest } from "@/lib/network/graphql-client";

const mockGraphql = graphqlRequest as ReturnType<typeof vi.fn>;

describe("routesService dispatch map", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("requests routes for the selected day range and maps stop statuses", async () => {
    mockGraphql.mockResolvedValueOnce({
      routes: [
        {
          id: "route-1",
          zoneId: "zone-1",
          zoneName: "Zone A",
          vehicleId: "vehicle-1",
          vehiclePlate: "TRUCK-101",
          driverId: "driver-1",
          driverName: "Jamie Parker",
          stagingArea: "A",
          startDate: "2026-04-12T08:00:00Z",
          endDate: null,
          startMileage: 100,
          endMileage: 0,
          totalMileage: 0,
          status: "IN_PROGRESS",
          parcelCount: 2,
          parcelsDelivered: 1,
          estimatedStopCount: 1,
          plannedDistanceMeters: 12000,
          plannedDurationSeconds: 1800,
          createdAt: "2026-04-12T06:00:00Z",
          updatedAt: "2026-04-12T07:00:00Z",
          cancellationReason: null,
          depotId: "depot-1",
          depotName: "Main Depot",
          depotAddressLine: "1 Depot Street",
          depotLongitude: 151.2093,
          depotLatitude: -33.8688,
          path: [
            { longitude: 151.2093, latitude: -33.8688 },
            { longitude: 151.2150, latitude: -33.8720 },
          ],
          stops: [
            {
              id: "stop-1",
              sequence: 1,
              recipientLabel: "Recipient One",
              addressLine: "20 Recipient Road",
              longitude: 151.2150,
              latitude: -33.8720,
              parcels: [
                {
                  parcelId: "parcel-1",
                  trackingNumber: "LMTEST0001",
                  recipientLabel: "Recipient One",
                  addressLine: "20 Recipient Road",
                  status: "DELIVERED",
                },
                {
                  parcelId: "parcel-2",
                  trackingNumber: "LMTEST0002",
                  recipientLabel: "Recipient One",
                  addressLine: "20 Recipient Road",
                  status: "FAILED_ATTEMPT",
                },
              ],
            },
          ],
          assignmentAuditTrail: [],
        },
      ],
    });

    const result = await routesService.getDispatchMapRoutes("2026-04-12");

    expect(mockGraphql).toHaveBeenCalledWith(
      expect.any(Object),
      expect.objectContaining({
        where: {
          startDate: {
            gte: new Date(2026, 3, 12).toISOString(),
            lt: new Date(2026, 3, 13).toISOString(),
          },
        },
      }),
    );

    expect(result).toHaveLength(1);
    expect(result[0].hasGeometry).toBe(true);
    expect(result[0].hasDepotGeometry).toBe(true);
    expect(result[0].vehiclePlate).toBe("TRUCK-101");
    expect(result[0].driverName).toBe("Jamie Parker");
    expect(result[0]).not.toHaveProperty("popupSummary");
    expect(result[0].stops[0].uiStatus).toBe("FAILED");
  });
});
