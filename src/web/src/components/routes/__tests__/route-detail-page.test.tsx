import { Suspense } from "react";
import { act, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import RouteDetailPage from "@/components/routes/route-detail-page";

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Dispatch User" } },
  }),
}));

vi.mock("@/queries/routes", () => ({
  useRoute: () => ({
    data: {
      id: "route-1",
      vehicleId: "vehicle-1",
      vehiclePlate: "TRUCK-101",
      driverId: "driver-1",
      driverName: "Jamie Parker",
      stagingArea: "A",
      startDate: "2026-04-09T08:00:00Z",
      endDate: null,
      startMileage: 120,
      endMileage: 0,
      totalMileage: 0,
      status: "PLANNED",
      parcelCount: 3,
      parcelsDelivered: 0,
      createdAt: "2026-04-08T08:00:00Z",
      updatedAt: "2026-04-09T07:30:00Z",
      assignmentAuditTrail: [
        {
          id: "audit-1",
          action: "ASSIGNED",
          previousDriverId: null,
          previousDriverName: null,
          newDriverId: "driver-1",
          newDriverName: "Jamie Parker",
          previousVehicleId: null,
          previousVehiclePlate: null,
          newVehicleId: "vehicle-1",
          newVehiclePlate: "TRUCK-101",
          changedAt: "2026-04-08T08:00:00Z",
          changedBy: "Dispatch User",
        },
        {
          id: "audit-2",
          action: "REASSIGNED",
          previousDriverId: "driver-1",
          previousDriverName: "Jamie Parker",
          newDriverId: "driver-2",
          newDriverName: "Alex Nguyen",
          previousVehicleId: "vehicle-1",
          previousVehiclePlate: "TRUCK-101",
          newVehicleId: "vehicle-2",
          newVehiclePlate: "TRUCK-202",
          changedAt: "2026-04-09T07:30:00Z",
          changedBy: "Dispatch User",
        },
      ],
    },
    isLoading: false,
    error: null,
  }),
}));

describe("route-detail-page", () => {
  it("renders the assignment audit panel and planned edit action", async () => {
    await act(async () => {
      render(
        <Suspense fallback={null}>
          <RouteDetailPage params={Promise.resolve({ id: "route-1" })} />
        </Suspense>,
      );
    });

    expect(
      await screen.findByRole("link", { name: /edit assignment/i }),
    ).toHaveAttribute("href", "/routes/route-1/edit");
    expect(
      await screen.findByRole("heading", { name: /assignment audit/i }),
    ).toBeInTheDocument();
    expect(screen.getByText(/initial assignment/i)).toBeInTheDocument();
    expect(screen.getByText(/reassignment/i)).toBeInTheDocument();
    expect(screen.getAllByText(/unassigned/i)).toHaveLength(2);
    expect(screen.getByText(/alex nguyen/i)).toBeInTheDocument();
    expect(screen.getByText(/truck-202/i)).toBeInTheDocument();
  });
});
