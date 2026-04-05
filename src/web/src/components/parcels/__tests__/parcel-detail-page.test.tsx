import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import ParcelDetailPage from "@/components/parcels/parcel-detail-page";

const { mockDownloadLabel, mockCancelParcel, mockTransitionParcelStatus } = vi.hoisted(
  () => ({
    mockDownloadLabel: vi.fn(),
    mockCancelParcel: vi.fn(),
    mockTransitionParcelStatus: vi.fn(),
  }),
);

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Warehouse User" } },
  }),
}));

vi.mock("next/navigation", () => ({
  useParams: () => ({ id: "parcel-1" }),
}));

vi.mock("@/queries/parcels", () => ({
  useParcel: () => ({
    data: {
      id: "parcel-1",
      trackingNumber: "LM202604010001",
      barcode: "LM202604010001",
      status: "REGISTERED",
      shipperAddressId: "address-1",
      serviceType: "STANDARD",
      weight: 2.5,
      weightUnit: "KG",
      length: 30,
      width: 20,
      height: 15,
      dimensionUnit: "CM",
      declaredValue: 120,
      currency: "USD",
      description: "Warehouse intake parcel",
      parcelType: "Box",
      cancellationReason: null,
      estimatedDeliveryDate: "2026-04-08T00:00:00Z",
      deliveryAttempts: 0,
      zoneId: "zone-1",
      zoneName: "North Zone",
      depotId: "depot-1",
      depotName: "North Depot",
      createdAt: "2026-04-01T09:15:00Z",
      lastModifiedAt: "2026-04-01T10:00:00Z",
      canEdit: true,
      canCancel: true,
      recipientAddress: {
        street1: "123 Main St",
        street2: null,
        city: "Springfield",
        state: "IL",
        postalCode: "62701",
        countryCode: "US",
        isResidential: true,
        contactName: "Jamie Carter",
        companyName: null,
        phone: "+1 555 0100",
        email: "jamie@example.com",
      },
      changeHistory: [
        {
          action: "Updated",
          fieldName: "Description",
          beforeValue: "Old note",
          afterValue: "Warehouse intake parcel",
          changedAt: "2026-04-01T10:00:00Z",
          changedBy: "Warehouse User",
        },
      ],
      allowedNextStatuses: ["RECEIVED_AT_DEPOT", "CANCELLED"],
    },
    isLoading: false,
    error: null,
  }),
  useCancelParcel: () => ({
    mutateAsync: mockCancelParcel,
    isPending: false,
  }),
  useTransitionParcelStatus: () => ({
    mutateAsync: mockTransitionParcelStatus,
    isPending: false,
  }),
  useParcelTrackingEvents: () => ({
    data: [],
    isLoading: false,
    error: null,
  }),
}));

vi.mock("@/services/parcels.service", () => ({
  parcelsService: {
    downloadLabel: mockDownloadLabel,
  },
}));

describe("ParcelDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders parcel details, history, and label actions", async () => {
    render(<ParcelDetailPage />);

    expect(
      screen.getByRole("heading", { name: "LM202604010001" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Jamie Carter")).toBeInTheDocument();
    expect(screen.getAllByText("North Zone").length).toBeGreaterThan(0);
    expect(screen.getByText("+1 555 0100")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /edit parcel/i })).toBeInTheDocument();
    expect(screen.getByText(/change history/i)).toBeInTheDocument();
    expect(screen.getByText("Description")).toBeInTheDocument();
    expect(screen.getByText("Old note")).toBeInTheDocument();
    expect(screen.getByText("Warehouse intake parcel")).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /download 4x6 zpl/i }));
    await user.click(screen.getByRole("button", { name: /download a4 pdf/i }));

    await waitFor(() => {
      expect(mockDownloadLabel).toHaveBeenNthCalledWith(1, "parcel-1", "zpl");
      expect(mockDownloadLabel).toHaveBeenNthCalledWith(2, "parcel-1", "pdf");
    });
  });

  it("requires a cancellation reason before cancelling from detail view", async () => {
    render(<ParcelDetailPage />);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /^cancel parcel$/i }));
    await user.click(screen.getAllByRole("button", { name: /^cancel parcel$/i })[1]);

    expect(screen.getByText(/cancellation reason is required/i)).toBeInTheDocument();

    await user.type(screen.getByLabelText(/cancellation reason/i), "Customer request");
    await user.click(screen.getAllByRole("button", { name: /^cancel parcel$/i })[1]);

    await waitFor(() => {
      expect(mockCancelParcel).toHaveBeenCalledWith({
        id: "parcel-1",
        reason: "Customer request",
      });
    });
  });
});
