import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import DispatchMapPage from "@/components/routes/dispatch-map-page";

const { mockUseDispatchMapRoutes } = vi.hoisted(() => ({
  mockUseDispatchMapRoutes: vi.fn(),
}));

vi.mock("@/queries/routes", () => ({
  useDispatchMapRoutes: (dateYmd: string) => mockUseDispatchMapRoutes(dateYmd),
}));

vi.mock("@/components/routes/dispatch-map-canvas", () => ({
  DispatchMapCanvas: ({
    routes,
    selectedRouteId,
    onSelectRoute,
  }: {
    routes: Array<{ id: string }>;
    selectedRouteId: string | null;
    onSelectRoute: (routeId: string) => void;
  }) => (
    <div>
      <div data-testid="dispatch-map-canvas">
        {routes.length}:{selectedRouteId ?? "none"}
      </div>
      <button type="button" onClick={() => onSelectRoute("route-2")}>
        Select second route
      </button>
    </div>
  ),
}));

describe("dispatch-map-page", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-04-12T10:00:00Z"));
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("defaults to today, renders route counts, and syncs selection from the map", async () => {
    mockUseDispatchMapRoutes.mockReturnValue({
      data: [
        {
          id: "route-1",
          vehiclePlate: "TRUCK-101",
          driverName: "Jamie Parker",
          zoneName: "Zone A",
          startDate: "2026-04-12T08:00:00Z",
          status: "IN_PROGRESS",
          parcelCount: 2,
          estimatedStopCount: 1,
          hasGeometry: true,
          stops: [],
        },
        {
          id: "route-2",
          vehiclePlate: "TRUCK-202",
          driverName: "Alex Nguyen",
          zoneName: "Zone B",
          startDate: "2026-04-12T09:30:00Z",
          status: "COMPLETED",
          parcelCount: 3,
          estimatedStopCount: 2,
          hasGeometry: false,
          stops: [],
        },
      ],
      isLoading: false,
      error: null,
    });

    render(<DispatchMapPage />);

    expect(mockUseDispatchMapRoutes).toHaveBeenCalledWith("2026-04-12");
    expect(screen.getByText(/2 routes scheduled/i)).toBeInTheDocument();
    expect(screen.getByText(/no geometry/i)).toBeInTheDocument();
    expect(screen.getByTestId("dispatch-map-canvas")).toHaveTextContent(
      "2:route-1",
    );

    fireEvent.click(screen.getByRole("button", { name: /select second route/i }));

    expect(
      screen.getByRole("button", { name: /truck-202/i }),
    ).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByTestId("dispatch-map-canvas")).toHaveTextContent(
      "2:route-2",
    );
  });

  it("renders an empty state when no routes exist for the selected day", () => {
    mockUseDispatchMapRoutes.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });

    render(<DispatchMapPage />);

    expect(
      screen.getByText(/no routes are scheduled for this date/i),
    ).toBeInTheDocument();
  });
});
