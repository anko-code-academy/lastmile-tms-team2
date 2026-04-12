import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import RouteAdjustPage from "@/components/routes/route-adjust-page";

const {
  mockAddParcel,
  mockRemoveParcel,
  mockUseRoute,
  mockUseCandidates,
} = vi.hoisted(() => ({
  mockAddParcel: vi.fn(),
  mockRemoveParcel: vi.fn(),
  mockUseRoute: vi.fn(),
  mockUseCandidates: vi.fn(),
}));

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Dispatch User" } },
  }),
}));

vi.mock("next/navigation", () => ({
  useParams: () => ({ id: "route-1" }),
}));

vi.mock("@/queries/routes", () => ({
  useRoute: (...args: unknown[]) => mockUseRoute(...args),
  useDispatchedRouteParcelCandidates: (...args: unknown[]) => mockUseCandidates(...args),
  useAddParcelToDispatchedRoute: () => ({
    mutateAsync: mockAddParcel,
    isPending: false,
  }),
  useRemoveParcelFromDispatchedRoute: () => ({
    mutateAsync: mockRemoveParcel,
    isPending: false,
  }),
}));

describe("route-adjust-page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockAddParcel.mockResolvedValue({ id: "route-1" });
    mockRemoveParcel.mockResolvedValue({ id: "route-1" });
    mockUseRoute.mockReturnValue({
      data: {
        id: "route-1",
        zoneId: "zone-1",
        zoneName: "Zone A",
        depotId: "depot-1",
        depotName: "North Depot",
        depotAddressLine: "1 Depot Street",
        depotLongitude: 151.2093,
        depotLatitude: -33.8688,
        vehicleId: "vehicle-1",
        vehiclePlate: "TRUCK-101",
        driverId: "driver-1",
        driverName: "Jamie Parker",
        stagingArea: "A",
        startDate: "2026-04-09T08:00:00Z",
        dispatchedAt: "2026-04-09T07:45:00Z",
        endDate: null,
        startMileage: 120,
        endMileage: 0,
        totalMileage: 0,
        status: "DISPATCHED",
        parcelCount: 2,
        parcelsDelivered: 0,
        estimatedStopCount: 2,
        plannedDistanceMeters: 14000,
        plannedDurationSeconds: 2400,
        createdAt: "2026-04-08T08:00:00Z",
        updatedAt: "2026-04-09T07:55:00Z",
        cancellationReason: null,
        latestParcelAdjustment: null,
        path: [],
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
                trackingNumber: "LMROUTE0001",
                recipientLabel: "Recipient One",
                addressLine: "20 Recipient Road",
                status: "OUT_FOR_DELIVERY",
              },
            ],
          },
          {
            id: "stop-2",
            sequence: 2,
            recipientLabel: "Recipient Two",
            addressLine: "21 Recipient Road",
            longitude: 151.225,
            latitude: -33.874,
            parcels: [
              {
                parcelId: "parcel-2",
                trackingNumber: "LMROUTE0002",
                recipientLabel: "Recipient Two",
                addressLine: "21 Recipient Road",
                status: "OUT_FOR_DELIVERY",
              },
            ],
          },
        ],
        assignmentAuditTrail: [],
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
    mockUseCandidates.mockReturnValue({
      data: [
        {
          id: "candidate-1",
          trackingNumber: "LMSTAGED0003",
          recipientLabel: "Recipient Three",
          addressLine: "22 Recipient Road",
          longitude: 151.23,
          latitude: -33.876,
          status: "STAGED",
        },
      ],
      isLoading: false,
      error: null,
    });
  });

  it("requires a reason before adding a staged parcel", async () => {
    render(<RouteAdjustPage />);

    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /add parcel/i }));

    expect(
      screen.getByText(/a reason is required before adding a parcel/i),
    ).toBeInTheDocument();
    expect(mockAddParcel).not.toHaveBeenCalled();

    await user.type(
      screen.getByLabelText(/reason for adding/i),
      "Late staged handoff",
    );
    await user.click(screen.getByRole("button", { name: /add parcel/i }));

    await waitFor(() => {
      expect(mockAddParcel).toHaveBeenCalledWith({
        id: "route-1",
        data: {
          parcelId: "candidate-1",
          reason: "Late staged handoff",
        },
      });
    });
  });

  it("submits route parcel removals with a required reason and renders the audit log", async () => {
    render(<RouteAdjustPage />);

    const user = userEvent.setup();

    expect(screen.getByText(/adjustment log/i)).toBeInTheDocument();
    expect(screen.getByText(/lmadjust0001/i)).toBeInTheDocument();
    expect(screen.getByText(/late staged handoff/i)).toBeInTheDocument();

    await user.type(
      screen.getByLabelText(/reason for removal/i),
      "Customer cancelled at depot",
    );
    await user.click(screen.getAllByRole("button", { name: /remove parcel/i })[0]);

    await waitFor(() => {
      expect(mockRemoveParcel).toHaveBeenCalledWith({
        id: "route-1",
        data: {
          parcelId: "parcel-1",
          reason: "Customer cancelled at depot",
        },
      });
    });
  });
});
