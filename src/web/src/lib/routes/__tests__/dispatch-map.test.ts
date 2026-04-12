import { describe, expect, it } from "vitest";

import {
  buildDispatchMapDayRange,
  computeDispatchMapStopStatus,
  routeGeometryHint,
  toDispatchMapRoute,
} from "@/lib/routes/dispatch-map";
import type { Route } from "@/types/routes";

const baseRoute: Route = {
  id: "route-1",
  zoneId: "zone-1",
  zoneName: "Zone A",
  depotId: "depot-1",
  depotName: "Main Depot",
  depotAddressLine: "1 Depot Street",
  depotLongitude: 151.2093,
  depotLatitude: -33.8688,
  vehicleId: "vehicle-1",
  vehiclePlate: "TRUCK-101",
  driverId: "driver-1",
  driverName: "Jamie Parker",
  stagingArea: "A",
  startDate: "2026-04-12T08:00:00Z",
  dispatchedAt: null,
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
  path: [],
  stops: [],
  assignmentAuditTrail: [],
};

describe("dispatch map helpers", () => {
  it("builds a half-open local day range from YYYY-MM-DD", () => {
    const range = buildDispatchMapDayRange("2026-04-12");

    expect(range).toEqual({
      gte: new Date(2026, 3, 12).toISOString(),
      lt: new Date(2026, 3, 13).toISOString(),
    });
  });

  it("marks a stop as delivered when every parcel is delivered", () => {
    const status = computeDispatchMapStopStatus([
      { status: "DELIVERED" },
      { status: "DELIVERED" },
    ]);

    expect(status).toBe("DELIVERED");
  });

  it("marks a stop as failed when any parcel is failed or returned", () => {
    expect(
      computeDispatchMapStopStatus([
        { status: "OUT_FOR_DELIVERY" },
        { status: "FAILED_ATTEMPT" },
      ]),
    ).toBe("FAILED");

    expect(
      computeDispatchMapStopStatus([
        { status: "DELIVERED" },
        { status: "RETURNED_TO_DEPOT" },
      ]),
    ).toBe("FAILED");
  });

  it("marks a stop as waiting for all other parcel combinations", () => {
    const status = computeDispatchMapStopStatus([
      { status: "SORTED" },
      { status: "OUT_FOR_DELIVERY" },
    ]);

    expect(status).toBe("WAITING");
  });

  it("treats depot coordinates as map geometry even when no path or stops exist", () => {
    const route = toDispatchMapRoute(baseRoute);

    expect(route.hasDepotGeometry).toBe(true);
    expect(route.hasGeometry).toBe(true);
    expect(routeGeometryHint(route)).toBe("Depot only");
  });
});
