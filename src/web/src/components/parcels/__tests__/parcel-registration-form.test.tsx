import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ParcelRegistrationForm } from "@/components/parcels/parcel-registration-form";

const { mockPush, mockMutateAsync, mockDownloadLabel, mockOnViewQueue } =
  vi.hoisted(() => ({
    mockPush: vi.fn(),
    mockMutateAsync: vi.fn(),
    mockDownloadLabel: vi.fn(),
    mockOnViewQueue: vi.fn(),
  }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
    back: vi.fn(),
  }),
}));

vi.mock("@/queries/depots", () => ({
  useDepots: () => ({
    data: [
      {
        id: "depot-1",
        addressId: "address-1",
        name: "North Depot",
        isActive: true,
        address: {
          street1: "10 Depot Way",
          street2: null,
          city: "Chicago",
          state: "IL",
          postalCode: "60601",
          countryCode: "US",
        },
      },
    ],
    isLoading: false,
  }),
}));

vi.mock("@/queries/parcels", () => ({
  useRegisterParcel: () => ({
    mutateAsync: mockMutateAsync,
    isPending: false,
    isError: false,
    error: null,
  }),
}));

vi.mock("@/services/parcels.service", () => ({
  parcelsService: {
    downloadLabel: mockDownloadLabel,
  },
}));

vi.mock("@/components/form/select-dropdown", () => ({
  SelectDropdown: ({
    options,
    value,
    onChange,
    id,
    className,
    disabled,
  }: {
    options: Array<{ value: string | number; label: string }>;
    value: string | number;
    onChange: (value: string | number) => void;
    id?: string;
    className?: string;
    disabled?: boolean;
  }) => (
    <select
      data-testid={id ?? "select-dropdown"}
      className={className}
      disabled={disabled}
      value={String(value)}
      onChange={(event) => {
        const option = options.find(
          (candidate) => String(candidate.value) === event.target.value,
        );
        onChange(option?.value ?? event.target.value);
      }}
    >
      {options.map((option) => (
        <option key={String(option.value)} value={String(option.value)}>
          {option.label}
        </option>
      ))}
    </select>
  ),
}));

describe("ParcelRegistrationForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockMutateAsync.mockResolvedValue({
      id: "parcel-1",
      trackingNumber: "LM202604010001",
      barcode: "LM202604010001",
      status: "REGISTERED",
      serviceType: "STANDARD",
      weight: 2.5,
      weightUnit: "KG",
      length: 30,
      width: 20,
      height: 15,
      dimensionUnit: "CM",
      declaredValue: 120,
      currency: "USD",
      description: null,
      parcelType: "Box",
      estimatedDeliveryDate: "2026-04-08T00:00:00Z",
      createdAt: "2026-04-01T09:15:00Z",
      zoneId: "zone-1",
      zoneName: "North Zone",
      depotId: "depot-1",
      depotName: "North Depot",
    });
  });

  async function fillRequiredFields() {
    const user = userEvent.setup();

    await user.selectOptions(screen.getByTestId("shipperAddressId"), "address-1");
    fireEvent.change(screen.getByPlaceholderText("123 Main St"), {
      target: { value: "123 Main St" },
    });
    fireEvent.change(screen.getByLabelText(/^city/i), {
      target: { value: "Springfield" },
    });
    fireEvent.change(screen.getByLabelText(/state \/ province/i), {
      target: { value: "IL" },
    });
    fireEvent.change(screen.getByLabelText(/postal code/i), {
      target: { value: "62701" },
    });
    fireEvent.change(screen.getByLabelText(/est\. delivery date/i), {
      target: { value: "2026-04-08" },
    });

    return user;
  }

  it("submits the selected depot addressId as shipperAddressId", async () => {
    render(<ParcelRegistrationForm />);

    const user = await fillRequiredFields();
    await user.click(screen.getByRole("button", { name: /register parcel/i }));

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          shipperAddressId: "address-1",
        }),
      );
    });
  });

  it("shows success actions and calls the download/detail flows", async () => {
    render(<ParcelRegistrationForm onViewQueue={mockOnViewQueue} />);

    const user = await fillRequiredFields();
    await user.click(screen.getByRole("button", { name: /register parcel/i }));

    expect(await screen.findByText(/parcel registered/i)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /download 4x6 zpl/i }));
    await user.click(screen.getByRole("button", { name: /download a4 pdf/i }));
    await user.click(screen.getByRole("button", { name: /open parcel detail/i }));
    await user.click(screen.getByRole("button", { name: /view intake queue/i }));

    await waitFor(() => {
      expect(mockDownloadLabel).toHaveBeenNthCalledWith(1, "parcel-1", "zpl");
      expect(mockDownloadLabel).toHaveBeenNthCalledWith(2, "parcel-1", "pdf");
    });

    expect(mockPush).toHaveBeenCalledWith("/parcels/parcel-1");
    expect(mockOnViewQueue).toHaveBeenCalledTimes(1);
    expect(mockPush).toHaveBeenCalledWith("/parcels");
  }, 15000);
});
