import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import DriverSchedulePage from "@/components/routes/driver-schedule-page";

const { mockUseMyRoutes } = vi.hoisted(() => ({
  mockUseMyRoutes: vi.fn(),
}));

vi.mock("@/queries/routes", () => ({
  useMyRoutes: () => mockUseMyRoutes(),
  useDriverRouteRealtimeUpdates: vi.fn(),
}));

describe("driver-schedule-page", () => {
  it("shows only active driver assignments with the correct driver messages", () => {
    mockUseMyRoutes.mockReturnValue({
      data: [
        {
          id: "route-draft",
          zoneId: "zone-1",
          zoneName: "Zone A",
          depotId: null,
          depotName: "North Depot",
          depotAddressLine: null,
          depotLongitude: null,
          depotLatitude: null,
          vehicleId: "vehicle-1",
          vehiclePlate: "TRUCK-101",
          driverId: "driver-1",
          driverName: "Jamie Parker",
          stagingArea: "A",
          startDate: "2026-04-09T09:30:00Z",
          dispatchedAt: null,
          endDate: null,
          startMileage: 10,
          endMileage: 0,
          totalMileage: 0,
          status: "DRAFT",
          parcelCount: 3,
          parcelsDelivered: 0,
          estimatedStopCount: 2,
          plannedDistanceMeters: 12000,
          plannedDurationSeconds: 1800,
          createdAt: "2026-04-08T08:00:00Z",
          updatedAt: null,
          cancellationReason: null,
          latestParcelAdjustment: null,
          path: [],
          stops: [],
          assignmentAuditTrail: [],
          parcelAdjustmentAuditTrail: [],
        },
        {
          id: "route-dispatched",
          zoneId: "zone-2",
          zoneName: "Zone B",
          depotId: null,
          depotName: "South Depot",
          depotAddressLine: null,
          depotLongitude: null,
          depotLatitude: null,
          vehicleId: "vehicle-2",
          vehiclePlate: "TRUCK-202",
          driverId: "driver-1",
          driverName: "Jamie Parker",
          stagingArea: "B",
          startDate: "2026-04-09T08:00:00Z",
          dispatchedAt: "2026-04-09T07:45:00Z",
          endDate: null,
          startMileage: 20,
          endMileage: 0,
          totalMileage: 0,
          status: "DISPATCHED",
          parcelCount: 4,
          parcelsDelivered: 0,
          estimatedStopCount: 3,
          plannedDistanceMeters: 15000,
          plannedDurationSeconds: 2200,
          createdAt: "2026-04-08T08:00:00Z",
          updatedAt: null,
          cancellationReason: null,
          latestParcelAdjustment: {
            id: "adjustment-1",
            action: "ADDED",
            parcelId: "parcel-added",
            trackingNumber: "LMADJUST0001",
            reason: "Late staged handoff",
            affectedStopSequence: 3,
            changedAt: "2026-04-09T07:50:00Z",
            changedBy: "Dispatch User",
          },
          path: [],
          stops: [],
          assignmentAuditTrail: [],
          parcelAdjustmentAuditTrail: [],
        },
        {
          id: "route-progress",
          zoneId: "zone-3",
          zoneName: "Zone C",
          depotId: null,
          depotName: "East Depot",
          depotAddressLine: null,
          depotLongitude: null,
          depotLatitude: null,
          vehicleId: "vehicle-3",
          vehiclePlate: "TRUCK-303",
          driverId: "driver-1",
          driverName: "Jamie Parker",
          stagingArea: "A",
          startDate: "2026-04-09T10:30:00Z",
          dispatchedAt: "2026-04-09T10:00:00Z",
          endDate: null,
          startMileage: 30,
          endMileage: 0,
          totalMileage: 0,
          status: "IN_PROGRESS",
          parcelCount: 5,
          parcelsDelivered: 2,
          estimatedStopCount: 4,
          plannedDistanceMeters: 21000,
          plannedDurationSeconds: 3200,
          createdAt: "2026-04-08T08:00:00Z",
          updatedAt: null,
          cancellationReason: null,
          latestParcelAdjustment: null,
          path: [],
          stops: [],
          assignmentAuditTrail: [],
          parcelAdjustmentAuditTrail: [],
        },
        {
          id: "route-cancelled",
          zoneId: "zone-4",
          zoneName: "Zone D",
          depotId: null,
          depotName: "West Depot",
          depotAddressLine: null,
          depotLongitude: null,
          depotLatitude: null,
          vehicleId: "vehicle-4",
          vehiclePlate: "TRUCK-404",
          driverId: "driver-1",
          driverName: "Jamie Parker",
          stagingArea: "B",
          startDate: "2026-04-09T11:30:00Z",
          dispatchedAt: null,
          endDate: null,
          startMileage: 40,
          endMileage: 0,
          totalMileage: 0,
          status: "CANCELLED",
          parcelCount: 1,
          parcelsDelivered: 0,
          estimatedStopCount: 1,
          plannedDistanceMeters: 5000,
          plannedDurationSeconds: 900,
          createdAt: "2026-04-08T08:00:00Z",
          updatedAt: null,
          cancellationReason: "Cancelled",
          latestParcelAdjustment: null,
          path: [],
          stops: [],
          assignmentAuditTrail: [],
          parcelAdjustmentAuditTrail: [],
        },
      ],
      isLoading: false,
      error: null,
    });

    render(<DriverSchedulePage />);

    expect(screen.getByText(/TRUCK-101/i)).toBeInTheDocument();
    expect(screen.getByText(/TRUCK-202/i)).toBeInTheDocument();
    expect(screen.getByText(/TRUCK-303/i)).toBeInTheDocument();
    expect(screen.queryByText(/TRUCK-404/i)).not.toBeInTheDocument();
    expect(screen.getAllByText(/scheduled/i)[0]).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: /^ready to leave$/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: /^route in progress$/i }),
    ).toBeInTheDocument();
    expect(
      screen.getAllByRole("link", { name: /open route/i })[0],
    ).toHaveAttribute("href", "/routes/route-dispatched");
    expect(screen.getByText(/parcel added to route/i)).toBeInTheDocument();
    expect(screen.getByText(/late staged handoff/i)).toBeInTheDocument();
  });
});
