import type { ReactNode } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { DashboardOverviewClient } from "@/components/dashboard/dashboard-overview";

const mockUseDepotParcelInventory = vi.fn();
const mockUseDepotParcelInventoryParcels = vi.fn();

vi.mock("@/queries/vehicles", () => ({
  useVehicles: () => ({
    data: [{ id: "vehicle-1" }],
    isLoading: false,
    error: null,
  }),
}));

vi.mock("@/queries/routes", () => ({
  useRoutes: () => ({
    data: [{ id: "route-1" }],
    isLoading: false,
    error: null,
  }),
}));

vi.mock("@/queries/depots", () => ({
  useDepots: () => ({
    data: [{ id: "depot-1" }],
    isLoading: false,
    error: null,
  }),
}));

vi.mock("@/queries/zones", () => ({
  useZones: () => ({
    data: [{ id: "zone-1" }],
    isLoading: false,
    error: null,
  }),
}));

vi.mock("@/queries/users", () => ({
  useUsers: () => ({
    data: [{ id: "user-1" }],
    isLoading: false,
    error: null,
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
  useDepotParcelInventory: (...args: unknown[]) => mockUseDepotParcelInventory(...args),
  useDepotParcelInventoryParcels: (...args: unknown[]) => mockUseDepotParcelInventoryParcels(...args),
}));

describe("DashboardOverviewClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockUseDepotParcelInventory.mockReturnValue({
      data: {
        depotName: "Test Depot",
        generatedAt: "2026-04-12T10:00:00Z",
        statusCounts: [
          { status: "RECEIVED_AT_DEPOT", count: 4 },
          { status: "SORTED", count: 3 },
          { status: "STAGED", count: 0 },
          { status: "LOADED", count: 2 },
          { status: "EXCEPTION", count: 1 },
        ],
        zoneCounts: [
          { zoneId: "zone-1", zoneName: "North Zone", count: 6 },
          { zoneId: "zone-2", zoneName: "South Zone", count: 4 },
        ],
        agingAlert: {
          thresholdMinutes: 240,
          count: 2,
        },
      },
      isLoading: false,
      error: null,
    });

    mockUseDepotParcelInventoryParcels.mockReturnValue({
      data: {
        totalCount: 2,
        pageInfo: {
          hasNextPage: false,
          hasPreviousPage: false,
          startCursor: "0",
          endCursor: "2",
        },
        nodes: [
          {
            id: "parcel-1",
            trackingNumber: "LM-DASH-0001",
            status: "RECEIVED_AT_DEPOT",
            zoneName: "North Zone",
            ageMinutes: 480,
            lastUpdatedAt: "2026-04-12T02:00:00Z",
          },
          {
            id: "parcel-2",
            trackingNumber: "LM-DASH-0002",
            status: "RECEIVED_AT_DEPOT",
            zoneName: "North Zone",
            ageMinutes: 360,
            lastUpdatedAt: "2026-04-12T04:00:00Z",
          },
        ],
      },
      isLoading: false,
      error: null,
    });
  });

  it("renders depot inventory cards and opens a drill-down dialog from a status count", async () => {
    render(
      <DashboardOverviewClient
        accessToken="token"
        displayName="Ops Manager"
        isAdmin={true}
      />,
    );

    expect(screen.getByText(/depot inventory/i)).toBeInTheDocument();
    expect(screen.getByText("Test Depot")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /received at depot/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /north zone/i })).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /received at depot/i }));

    expect(
      screen.getByRole("dialog", { name: /received at depot parcels/i }),
    ).toBeInTheDocument();
    expect(screen.getByText("LM-DASH-0001")).toBeInTheDocument();
    expect(screen.getByText("480 min")).toBeInTheDocument();
  });

  it("passes the selected aging threshold into the inventory hook", async () => {
    render(
      <DashboardOverviewClient
        accessToken="token"
        displayName="Ops Manager"
        isAdmin={false}
      />,
    );

    expect(mockUseDepotParcelInventory).toHaveBeenCalledWith(240);

    const user = userEvent.setup();
    await user.clear(screen.getByLabelText(/aging threshold hours/i));
    await user.type(screen.getByLabelText(/aging threshold hours/i), "6");

    await waitFor(() => {
      expect(mockUseDepotParcelInventory).toHaveBeenLastCalledWith(360);
    });
  });

  it("shows an empty state when the user has no assigned depot", () => {
    mockUseDepotParcelInventory.mockReturnValueOnce({
      data: null,
      isLoading: false,
      error: null,
    });

    render(
      <DashboardOverviewClient
        accessToken="token"
        displayName="Ops Manager"
        isAdmin={false}
      />,
    );

    expect(screen.getByText(/no depot inventory available/i)).toBeInTheDocument();
    expect(screen.getByText(/assigned depot/i)).toBeInTheDocument();
  });
});
