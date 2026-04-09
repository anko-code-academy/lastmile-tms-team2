import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import NewRoutePage from "@/components/routes/route-create-page";

const { mockPush, mockCreateRoute, mockCandidates } = vi.hoisted(() => ({
  mockPush: vi.fn(),
  mockCreateRoute: vi.fn(),
  mockCandidates: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

vi.mock("@/queries/routes", () => ({
  useCreateRoute: () => ({
    mutateAsync: mockCreateRoute,
    isPending: false,
  }),
  useRouteAssignmentCandidates: () => mockCandidates(),
}));

vi.mock("@/queries/parcels", () => ({
  useParcelsForRouteCreation: () => ({
    data: [
      {
        id: "parcel-1",
        trackingNumber: "LMSTAGEWEB0001",
        weight: 2.5,
        weightUnit: "KG",
        zoneId: "zone-1",
        zoneName: "Zone A",
      },
    ],
    isLoading: false,
    error: null,
  }),
}));

describe("route-create-page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockCreateRoute.mockResolvedValue({ id: "route-1" });
    mockCandidates.mockReturnValue({
      data: {
        vehicles: [
          {
            id: "vehicle-1",
            registrationPlate: "TRUCK-101",
            parcelCapacity: 12,
            weightCapacity: 180,
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
            isCurrentAssignment: false,
            workloadRoutes: [
              {
                routeId: "route-2-abcdef",
                vehicleId: "vehicle-2",
                vehiclePlate: "TRUCK-202",
                startDate: "2026-04-09T09:00:00Z",
                status: "COMPLETED",
              },
            ],
          },
        ],
      },
      isLoading: false,
      error: null,
    });
  });

  it("submits the selected staging area with the create route request", async () => {
    render(<NewRoutePage />);

    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /select vehicle/i }));
    await user.click(screen.getByRole("option", { name: /truck-101/i }));

    await user.click(screen.getByRole("button", { name: /select driver/i }));
    await user.click(screen.getByRole("option", { name: /jamie parker/i }));

    await user.click(screen.getByRole("button", { name: /select staging area/i }));
    await user.click(screen.getByRole("option", { name: /area b/i }));

    await user.click(screen.getByLabelText(/lmstageweb0001/i));
    await user.click(screen.getByRole("button", { name: /create route/i }));

    await waitFor(() => {
      expect(mockCreateRoute).toHaveBeenCalledWith(
        expect.objectContaining({
          vehicleId: "vehicle-1",
          driverId: "driver-1",
          stagingArea: "B",
          parcelIds: ["parcel-1"],
          startMileage: 0,
          startDate: expect.any(String),
        }),
      );
    });

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith("/routes");
    });
  });

  it("shows workload details for the selected driver", async () => {
    render(<NewRoutePage />);

    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /select vehicle/i }));
    await user.click(screen.getByRole("option", { name: /truck-101/i }));

    await user.click(screen.getByRole("button", { name: /select driver/i }));
    await user.click(screen.getByRole("option", { name: /jamie parker/i }));

    expect(screen.getByText(/driver workload/i)).toBeInTheDocument();
    expect(screen.getByText(/other routes already assigned/i)).toBeInTheDocument();
    expect(screen.getByText(/truck-202/i)).toBeInTheDocument();
    expect(screen.getByText(/completed/i)).toBeInTheDocument();
  });
});
