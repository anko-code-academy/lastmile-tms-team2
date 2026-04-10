import type { ReactNode } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { RouteLoadOutPage } from "@/components/parcels/route-loadout-page";
import type { LoadOutRoute } from "@/graphql/generated";

const { mockLoadParcelForRoute, mockCompleteLoadOut } = vi.hoisted(() => ({
  mockLoadParcelForRoute: vi.fn(),
  mockCompleteLoadOut: vi.fn(),
}));

let mockLoadOutRoutes: LoadOutRoute[] = [];
let mockRouteLoadOutBoard: unknown = null;

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Warehouse Operator" } },
  }),
}));

vi.mock("@/components/ui/label", () => ({
  Label: ({
    children,
    htmlFor,
  }: {
    children: ReactNode;
    htmlFor?: string;
  }) => <label htmlFor={htmlFor}>{children}</label>,
}));

vi.mock("@/queries/parcels", () => ({
  useLoadOutRoutes: () => ({
    data: mockLoadOutRoutes,
    isLoading: false,
    error: null,
  }),
  useRouteLoadOutBoard: () => ({
    data: mockRouteLoadOutBoard,
    isLoading: false,
    error: null,
  }),
  useLoadParcelForRoute: () => ({
    mutateAsync: mockLoadParcelForRoute,
    isPending: false,
  }),
  useCompleteLoadOut: () => ({
    mutateAsync: mockCompleteLoadOut,
    isPending: false,
  }),
}));

function makeBoard(overrides?: Partial<{ loadedParcelCount: number; remainingParcelCount: number }>) {
  return {
    id: "route-1",
    vehicleId: "vehicle-1",
    vehiclePlate: "TRUCK-101",
    driverId: "driver-1",
    driverName: "Jamie Parker",
    status: "PLANNED",
    stagingArea: "A",
    startDate: "2026-04-10T08:30:00Z",
    expectedParcelCount: 2,
    loadedParcelCount: overrides?.loadedParcelCount ?? 1,
    remainingParcelCount: overrides?.remainingParcelCount ?? 1,
    expectedParcels: [
      {
        parcelId: "parcel-1",
        trackingNumber: "LMLOADWEB1001",
        barcode: "LMLOADWEB1001",
        status: "Loaded",
        isLoaded: true,
      },
      {
        parcelId: "parcel-2",
        trackingNumber: "LMLOADWEB1002",
        barcode: "LMLOADWEB1002",
        status: "Staged",
        isLoaded: false,
      },
    ],
  };
}

describe("RouteLoadOutPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockLoadOutRoutes = [
      {
        id: "route-1",
        vehicleId: "vehicle-1",
        vehiclePlate: "TRUCK-101",
        driverId: "driver-1",
        driverName: "Jamie Parker",
        status: "PLANNED",
        stagingArea: "A",
        startDate: "2026-04-10T08:30:00Z",
        expectedParcelCount: 2,
        loadedParcelCount: 1,
        remainingParcelCount: 1,
      },
    ];
    mockRouteLoadOutBoard = makeBoard();
  });

  it("renders the load-out board with expected/loaded/remaining counts", () => {
    render(<RouteLoadOutPage />);

    expect(screen.getByText(/route load-out/i)).toBeInTheDocument();
    expect(screen.getByText(/expected parcels/i)).toBeInTheDocument();
    expect(screen.getByText(/loaded parcels/i)).toBeInTheDocument();
    expect(screen.getByText(/remaining parcels/i)).toBeInTheDocument();
  });

  it("submits typed scans and renders the loaded result message", async () => {
    mockLoadParcelForRoute.mockResolvedValue({
      outcome: "LOADED",
      message: "Parcel loaded successfully.",
      trackingNumber: "LMLOADWEB1002",
      parcelId: "parcel-2",
      conflictingRouteId: null,
      conflictingStagingArea: null,
      board: makeBoard({ loadedParcelCount: 2, remainingParcelCount: 0 }),
    });

    render(<RouteLoadOutPage />);

    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/scan barcode/i), "LMLOADWEB1002{enter}");

    await waitFor(() => {
      expect(mockLoadParcelForRoute).toHaveBeenCalledWith({
        routeId: "route-1",
        barcode: "LMLOADWEB1002",
      });
    });

    expect(screen.getByText(/parcel loaded successfully/i)).toBeInTheDocument();
  });

  it("renders a wrong-route warning with the conflicting staging area", async () => {
    mockLoadParcelForRoute.mockResolvedValue({
      outcome: "WRONG_ROUTE",
      message: "Parcel is assigned to a different active route.",
      trackingNumber: "LMLOADWEB9999",
      parcelId: "parcel-9",
      conflictingRouteId: "route-9",
      conflictingStagingArea: "B",
      board: makeBoard(),
    });

    render(<RouteLoadOutPage />);

    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/scan barcode/i), "LMLOADWEB9999{enter}");

    await waitFor(() => {
      expect(mockLoadParcelForRoute).toHaveBeenCalledWith({
        routeId: "route-1",
        barcode: "LMLOADWEB9999",
      });
    });

    expect(
      screen.getByText(/parcel is assigned to a different active route/i),
    ).toBeInTheDocument();
    expect(screen.getByText(/area b/i)).toBeInTheDocument();
  });

  it("shows short-load warning when completing with unloaded parcels", async () => {
    render(<RouteLoadOutPage />);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /complete load-out/i }));

    expect(screen.getByText(/short load warning/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /force complete/i })).toBeInTheDocument();
  });

  it("completes load-out and renders completion summary", async () => {
    mockLoadOutRoutes = [
      {
        ...mockLoadOutRoutes[0],
        loadedParcelCount: 2,
        remainingParcelCount: 0,
      },
    ];
    mockRouteLoadOutBoard = makeBoard({ loadedParcelCount: 2, remainingParcelCount: 0 });

    mockCompleteLoadOut.mockResolvedValue({
      success: true,
      message: "Load-out completed. Route is now in progress.",
      loadedCount: 2,
      skippedCount: 0,
      totalCount: 2,
      board: makeBoard({ loadedParcelCount: 2, remainingParcelCount: 0 }),
    });

    render(<RouteLoadOutPage />);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /complete load-out/i }));

    await waitFor(() => {
      expect(mockCompleteLoadOut).toHaveBeenCalledWith({
        routeId: "route-1",
        force: false,
      });
    });

    expect(screen.getByRole("heading", { name: /load-out completed/i })).toBeInTheDocument();
    expect(screen.getByText(/route is now in progress/i)).toBeInTheDocument();
  });
});
