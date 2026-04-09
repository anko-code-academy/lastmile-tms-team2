import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import RouteEditPage from "@/components/routes/route-edit-page";

const { mockPush, mockMutateAsync } = vi.hoisted(() => ({
  mockPush: vi.fn(),
  mockMutateAsync: vi.fn(),
}));

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Dispatch User" } },
  }),
}));

vi.mock("next/navigation", () => ({
  useParams: () => ({ id: "route-1" }),
  useRouter: () => ({
    push: mockPush,
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
      updatedAt: null,
      assignmentAuditTrail: [],
    },
    isLoading: false,
    error: null,
  }),
  useRouteAssignmentCandidates: () => ({
    data: {
      vehicles: [
        {
          id: "vehicle-1",
          registrationPlate: "TRUCK-101",
          parcelCapacity: 12,
          weightCapacity: 180,
          status: "IN_USE",
          depotId: "depot-1",
          depotName: "Test Depot",
          isCurrentAssignment: true,
        },
        {
          id: "vehicle-2",
          registrationPlate: "TRUCK-202",
          parcelCapacity: 14,
          weightCapacity: 220,
          status: "AVAILABLE",
          depotId: "depot-1",
          depotName: "Test Depot",
          isCurrentAssignment: false,
        },
      ],
      drivers: [
        {
          id: "driver-1",
          displayName: "Jamie Parker",
          depotId: "depot-1",
          zoneId: "zone-1",
          status: "ACTIVE",
          isCurrentAssignment: true,
          workloadRoutes: [],
        },
        {
          id: "driver-2",
          displayName: "Alex Nguyen",
          depotId: "depot-1",
          zoneId: "zone-1",
          status: "ACTIVE",
          isCurrentAssignment: false,
          workloadRoutes: [
            {
              routeId: "route-2-abcdef",
              vehicleId: "vehicle-3",
              vehiclePlate: "TRUCK-303",
              startDate: "2026-04-09T10:00:00Z",
              status: "COMPLETED",
            },
          ],
        },
      ],
    },
    isLoading: false,
    error: null,
  }),
  useUpdateRouteAssignment: () => ({
    mutateAsync: mockMutateAsync,
    isPending: false,
  }),
}));

describe("route-edit-page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockMutateAsync.mockResolvedValue({ id: "route-1" });
  });

  it("submits the updated assignment and redirects back to the route detail page", async () => {
    render(<RouteEditPage />);

    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /truck-101/i }));
    await user.click(screen.getByRole("option", { name: /truck-202/i }));

    await user.click(screen.getByRole("button", { name: /select driver/i }));
    await user.click(screen.getByRole("option", { name: /alex nguyen/i }));

    await user.click(screen.getByRole("button", { name: /save assignment/i }));

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith({
        id: "route-1",
        data: {
          vehicleId: "vehicle-2",
          driverId: "driver-2",
        },
      });
    });

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith("/routes/route-1");
    });
  });

  it("renders workload details for the selected replacement driver", async () => {
    render(<RouteEditPage />);

    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /truck-101/i }));
    await user.click(screen.getByRole("option", { name: /truck-202/i }));

    await user.click(screen.getByRole("button", { name: /select driver/i }));
    await user.click(screen.getByRole("option", { name: /alex nguyen/i }));

    expect(screen.getByText(/driver workload/i)).toBeInTheDocument();
    expect(screen.getByText(/truck-303/i)).toBeInTheDocument();
    expect(screen.getByText(/completed/i)).toBeInTheDocument();
  });
});
