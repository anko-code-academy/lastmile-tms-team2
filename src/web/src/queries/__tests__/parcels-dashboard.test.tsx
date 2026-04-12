import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

import {
  depotParcelInventoryKeys,
  useDepotParcelInventory,
  useDepotParcelInventoryParcels,
} from "../parcels";
import * as parcelsServiceModule from "../../services/parcels.service";

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Ops" } },
  }),
}));

vi.mock("../../services/parcels.service", () => ({
  parcelsService: {
    getDepotParcelInventory: vi.fn(),
    getDepotParcelInventoryParcels: vi.fn(),
  },
}));

const mockParcelsService = vi.mocked(parcelsServiceModule.parcelsService);

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

  function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  }

  Wrapper.displayName = "QueryClientWrapper";
  return Wrapper;
}

describe("depot inventory parcel queries", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("fetches the depot inventory summary and has a stable key", async () => {
    mockParcelsService.getDepotParcelInventory.mockResolvedValueOnce({
      depotName: "Test Depot",
      generatedAt: "2026-04-12T10:00:00Z",
      statusCounts: [],
      zoneCounts: [],
      agingAlert: {
        thresholdMinutes: 240,
        count: 0,
      },
    } as never);

    const { result } = renderHook(() => useDepotParcelInventory(240), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockParcelsService.getDepotParcelInventory).toHaveBeenCalledWith(240);
    expect(depotParcelInventoryKeys.summary(240)).toEqual([
      "parcels",
      "depotInventory",
      "summary",
      240,
    ]);
  });

  it("fetches drill-down parcels with the selected filters", async () => {
    mockParcelsService.getDepotParcelInventoryParcels.mockResolvedValueOnce({
      totalCount: 1,
      pageInfo: {
        hasNextPage: false,
        hasPreviousPage: false,
        startCursor: "0",
        endCursor: "1",
      },
      nodes: [
        {
          id: "parcel-1",
          trackingNumber: "LM-DASH-0001",
          status: "RECEIVED_AT_DEPOT",
          zoneId: "zone-1",
          zoneName: "North Zone",
          ageMinutes: 480,
          lastUpdatedAt: "2026-04-12T02:00:00Z",
        },
      ],
    } as never);

    const { result } = renderHook(
      () =>
        useDepotParcelInventoryParcels({
          agingThresholdMinutes: 240,
          status: "RECEIVED_AT_DEPOT",
          zoneId: "zone-1",
          agingOnly: false,
          first: 20,
          after: null,
          enabled: true,
        }),
      {
        wrapper: createWrapper(),
      },
    );

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockParcelsService.getDepotParcelInventoryParcels).toHaveBeenCalledWith({
      agingThresholdMinutes: 240,
      status: "RECEIVED_AT_DEPOT",
      zoneId: "zone-1",
      agingOnly: false,
      first: 20,
      after: null,
    });
  });
});
