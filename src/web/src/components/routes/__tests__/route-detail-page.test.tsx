import { Suspense } from "react";
import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import RouteDetailPage from "@/components/routes/route-detail-page";

const {
  mockCancelRoute,
  mockDispatchRoute,
  mockStartRoute,
  mockCompleteRoute,
  mockUseRoute,
  mockUseMyRoute,
} = vi.hoisted(() => ({
  mockCancelRoute: vi.fn(),
  mockDispatchRoute: vi.fn(),
  mockStartRoute: vi.fn(),
  mockCompleteRoute: vi.fn(),
  mockUseRoute: vi.fn(),
  mockUseMyRoute: vi.fn(),
}));

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Dispatch User" } },
  }),
}));

vi.mock("@/components/routes/route-map", () => ({
  RouteMap: () => <div data-testid="route-map" />,
}));

vi.mock("@/queries/routes", () => ({
  useRoute: (...args: unknown[]) => mockUseRoute(...args),
  useMyRoute: (...args: unknown[]) => mockUseMyRoute(...args),
  useDriverRouteRealtimeUpdates: vi.fn(),
  useCancelRoute: () => ({
    mutateAsync: mockCancelRoute,
    isPending: false,
  }),
  useDispatchRoute: () => ({
    mutate: mockDispatchRoute,
    isPending: false,
  }),
  useStartRoute: () => ({
    mutate: mockStartRoute,
    isPending: false,
  }),
  useCompleteRoute: () => ({
    mutateAsync: mockCompleteRoute,
    isPending: false,
  }),
}));

const baseRoute = {
  id: "route-1",
  zoneId: "zone-1",
  zoneName: "Zone A",
  vehicleId: "vehicle-1",
  vehiclePlate: "TRUCK-101",
  driverId: "driver-1",
  driverName: "Jamie Parker",
  stagingArea: "A",
  startDate: "2026-04-09T08:00:00Z",
  dispatchedAt: null,
  endDate: null,
  startMileage: 120,
  endMileage: 0,
  totalMileage: 0,
  status: "DRAFT",
  parcelCount: 3,
  parcelsDelivered: 0,
  estimatedStopCount: 2,
  plannedDistanceMeters: 14000,
  plannedDurationSeconds: 2400,
  depotName: "Test Depot",
  depotAddressLine: "1 Depot Street",
  depotLongitude: 151.2093,
  depotLatitude: -33.8688,
  path: [
    { longitude: 151.2093, latitude: -33.8688 },
    { longitude: 151.215, latitude: -33.872 },
  ],
  stops: [
    {
      id: "stop-1",
      sequence: 1,
      recipientLabel: "Recipient One",
      addressLine: "20 Recipient Road",
      longitude: 151.215,
      latitude: -33.872,
      parcels: [
        {
          parcelId: "parcel-1",
          trackingNumber: "LMSTAGEWEB0001",
          recipientLabel: "Recipient One",
          addressLine: "20 Recipient Road",
        },
      ],
    },
  ],
  createdAt: "2026-04-08T08:00:00Z",
  updatedAt: "2026-04-09T07:30:00Z",
  cancellationReason: null,
  latestParcelAdjustment: null,
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
  parcelAdjustmentAuditTrail: [],
};

describe("route-detail-page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseRoute.mockReturnValue({
      data: baseRoute,
      isLoading: false,
      error: null,
    });
    mockUseMyRoute.mockReturnValue({
      data: {
        ...baseRoute,
        status: "DISPATCHED",
        dispatchedAt: "2026-04-09T07:45:00Z",
        latestParcelAdjustment: {
          id: "adjustment-1",
          action: "ADDED",
          parcelId: "parcel-added",
          trackingNumber: "LMADJUST0001",
          reason: "Late staged handoff",
          affectedStopSequence: 2,
          changedAt: "2026-04-09T07:50:00Z",
          changedBy: "Dispatch User",
        },
        parcelAdjustmentAuditTrail: [
          {
            id: "adjustment-1",
            action: "ADDED",
            parcelId: "parcel-added",
            trackingNumber: "LMADJUST0001",
            reason: "Late staged handoff",
            affectedStopSequence: 2,
            changedAt: "2026-04-09T07:50:00Z",
            changedBy: "Dispatch User",
          },
        ],
      },
      isLoading: false,
      error: null,
    });
  });

  it("renders the assignment audit panel and draft edit action", async () => {
    mockCancelRoute.mockResolvedValue(undefined);

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
      screen.getByRole("button", { name: /cancel route/i }),
    ).toBeInTheDocument();
    expect(
      await screen.findByRole("heading", { name: /assignment audit/i }),
    ).toBeInTheDocument();
    expect(screen.getByText(/initial assignment/i)).toBeInTheDocument();
    expect(screen.getByText(/reassignment/i)).toBeInTheDocument();
    expect(screen.getAllByText(/unassigned/i)).toHaveLength(2);
    expect(screen.getByText(/alex nguyen/i)).toBeInTheDocument();
    expect(screen.getByText(/truck-202/i)).toBeInTheDocument();
  });

  it("opens the cancel dialog and submits route cancellation", async () => {
    mockCancelRoute.mockResolvedValue(undefined);
    const user = userEvent.setup();

    await act(async () => {
      render(
        <Suspense fallback={null}>
          <RouteDetailPage params={Promise.resolve({ id: "route-1" })} />
        </Suspense>,
      );
    });

    await user.click(screen.getByRole("button", { name: /cancel route/i }));

    expect(
      await screen.findByRole("heading", { name: /cancel this route/i }),
    ).toBeInTheDocument();

    await user.type(
      screen.getByLabelText(/cancellation reason/i),
      "Weather closure",
    );
    await user.click(screen.getByRole("button", { name: /confirm cancellation/i }));

    expect(mockCancelRoute).toHaveBeenCalledWith({
      id: "route-1",
      data: { reason: "Weather closure" },
    });
  });

  it("renders a read-only driver view with schedule messaging", async () => {
    await act(async () => {
      render(
        <Suspense fallback={null}>
          <RouteDetailPage
            params={Promise.resolve({ id: "route-1" })}
            mode="driver"
          />
        </Suspense>,
      );
    });

    expect(screen.getByText(/driver message/i)).toBeInTheDocument();
    expect(screen.getByText(/route update/i)).toBeInTheDocument();
    expect(screen.getByText(/late staged handoff/i)).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: /^ready to leave$/i }),
    ).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: /my schedule/i })[0]).toHaveAttribute(
      "href",
      "/routes/my",
    );
    expect(screen.queryByRole("link", { name: /edit assignment/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /dispatch/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /start route/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /cancel route/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /open vehicle/i })).not.toBeInTheDocument();
    expect(
      screen.queryByRole("heading", { name: /assignment audit/i }),
    ).not.toBeInTheDocument();
  });

  it("shows the adjust stops action for dispatched dispatch views", async () => {
    mockUseRoute.mockReturnValue({
      data: {
        ...baseRoute,
        status: "DISPATCHED",
        dispatchedAt: "2026-04-09T07:45:00Z",
      },
      isLoading: false,
      error: null,
    });

    await act(async () => {
      render(
        <Suspense fallback={null}>
          <RouteDetailPage params={Promise.resolve({ id: "route-1" })} />
        </Suspense>,
      );
    });

    expect(
      screen.getByRole("link", { name: /adjust stops/i }),
    ).toHaveAttribute("href", "/routes/route-1/adjust");
  });
});
