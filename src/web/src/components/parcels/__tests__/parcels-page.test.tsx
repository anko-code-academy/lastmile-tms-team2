import type { ReactNode } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import ParcelsPage from "@/components/parcels/parcels-page";

const { mockDownloadBulkLabels, mockCancelParcel } = vi.hoisted(() => ({
  mockDownloadBulkLabels: vi.fn(),
  mockCancelParcel: vi.fn(),
}));

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Warehouse User" } },
  }),
}));

vi.mock("@/components/ui/label", () => ({
  Label: ({ children }: { children: ReactNode }) => <>{children}</>,
}));

vi.mock("@/queries/parcels", () => ({
  usePreLoadParcelsPage: () => ({
    data: {
      totalCount: 3,
      pageInfo: {
        hasNextPage: false,
        hasPreviousPage: false,
        startCursor: "0",
        endCursor: "3",
      },
      nodes: [
        {
          id: "parcel-1",
          trackingNumber: "LM202604010001",
          status: "REGISTERED",
          serviceType: "STANDARD",
          weight: 2.5,
          weightUnit: "KG",
          parcelType: "Box",
          createdAt: "2026-04-01T09:15:00Z",
          zoneName: "North Zone",
        },
        {
          id: "parcel-2",
          trackingNumber: "LM202604010002",
          status: "STAGED",
          serviceType: "EXPRESS",
          weight: 1.1,
          weightUnit: "KG",
          parcelType: "Envelope",
          createdAt: "2026-04-01T09:30:00Z",
          zoneName: "Central Zone",
        },
        {
          id: "parcel-3",
          trackingNumber: "LM202604010003",
          status: "DELIVERED",
          serviceType: "STANDARD",
          weight: 3.4,
          weightUnit: "KG",
          parcelType: "Crate",
          createdAt: "2026-04-01T10:15:00Z",
          zoneName: "South Zone",
        },
      ],
    },
    isLoading: false,
    error: null,
  }),
  useAvailableParcelTypes: () => ({
    data: [
      { id: "parcel-1", parcelType: "Box" },
      { id: "parcel-2", parcelType: "Envelope" },
      { id: "parcel-3", parcelType: "Crate" },
    ],
    isLoading: false,
    error: null,
  }),
  useCancelParcel: () => ({
    mutateAsync: mockCancelParcel,
    isPending: false,
  }),
}));

vi.mock("@/services/parcels.service", () => ({
  parcelsService: {
    downloadBulkLabels: mockDownloadBulkLabels,
  },
}));

vi.mock("@/queries/zones", () => ({
  useZones: () => ({ data: [] }),
}));

vi.mock("@/components/parcels/parcel-import-panel", () => ({
  ParcelImportPanel: () => <div data-testid="parcel-import-panel" />,
}));

vi.mock("@/components/parcels/parcel-import-history-table", () => ({
  ParcelImportHistoryTable: () => <div data-testid="parcel-import-history-table" />,
}));

describe("ParcelsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("enables bulk actions when parcels are selected", async () => {
    render(<ParcelsPage />);

    const user = userEvent.setup();
    const zplButton = screen.getByRole("button", { name: /download 4x6 zpl/i });

    expect(zplButton).toBeDisabled();

    await user.click(screen.getByLabelText(/select parcel lm202604010001/i));

    expect(zplButton).toBeEnabled();

    await user.click(zplButton);

    await waitFor(() => {
      expect(mockDownloadBulkLabels).toHaveBeenCalledWith(["parcel-1"], "zpl");
    });
  });

  it("uses tracking-number URLs for parcel detail links", () => {
    render(<ParcelsPage />);

    expect(
      screen.getByRole("link", { name: "LM202604010001" }),
    ).toHaveAttribute("href", "/parcels/LM202604010001");
  });

  it("shows a route staging entry point from the pre-load queue", () => {
    render(<ParcelsPage />);

    expect(
      screen.getByRole("link", { name: /route staging/i }),
    ).toHaveAttribute("href", "/parcels/staging");
  });

  it("selects all visible parcels from the header control", async () => {
    render(<ParcelsPage />);

    const user = userEvent.setup();
    await user.click(screen.getByLabelText(/select all visible parcels/i));
    await user.click(screen.getByRole("button", { name: /download a4 pdf/i }));

    await waitFor(() => {
      expect(mockDownloadBulkLabels).toHaveBeenCalledWith(
        ["parcel-1", "parcel-2", "parcel-3"],
        "pdf",
      );
    });
  });

  it("disables edit and cancellation once a parcel is past load-out", () => {
    render(<ParcelsPage />);

    expect(
      screen.getByRole("link", { name: /edit lm202604010003/i }),
    ).toHaveAttribute("aria-disabled", "true");
    expect(
      screen.getByRole("button", { name: /cancel lm202604010003/i }),
    ).toBeDisabled();
  });

  it("requires a cancellation reason before cancelling a parcel", async () => {
    render(<ParcelsPage />);

    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: /cancel lm202604010001/i }));
    await user.click(screen.getByRole("button", { name: /^cancel parcel$/i }));

    expect(screen.getByText(/cancellation reason is required/i)).toBeInTheDocument();

    await user.type(
      screen.getByLabelText(/cancellation reason/i),
      "Duplicate order",
    );
    await user.click(screen.getByRole("button", { name: /^cancel parcel$/i }));

    await waitFor(() => {
      expect(mockCancelParcel).toHaveBeenCalledWith({
        id: "parcel-1",
        reason: "Duplicate order",
      });
    });
  });
});
