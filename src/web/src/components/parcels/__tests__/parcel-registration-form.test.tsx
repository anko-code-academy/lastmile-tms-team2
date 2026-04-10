import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ParcelRegistrationForm } from "@/components/parcels/parcel-registration-form";

const {
  mockPush,
  mockMutateAsync,
  mockDownloadLabel,
  mockOnViewQueue,
  mockDepots,
  mockSearchBoxProps,
} =
  vi.hoisted(() => ({
    mockPush: vi.fn(),
    mockMutateAsync: vi.fn(),
    mockDownloadLabel: vi.fn(),
    mockOnViewQueue: vi.fn(),
    mockDepots: [
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
    mockSearchBoxProps: vi.fn(),
  }));

vi.mock("@/lib/mapbox/config", () => ({
  getMapboxAccessToken: () => "pk.test-token",
  getMapboxConfigurationError: () => null,
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
    back: vi.fn(),
  }),
}));

vi.mock("@/queries/depots", () => ({
  useDepots: () => ({
    data: mockDepots,
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

vi.mock("@mapbox/search-js-react", () => ({
  SearchBox: ({
    onChange,
    onClear,
    onRetrieve,
    options,
    placeholder,
    value,
  }: {
    onChange?: (value: string) => void;
    onClear?: () => void;
    onRetrieve?: (response: unknown) => void;
    options?: Record<string, unknown>;
    placeholder?: string;
    value?: string;
  }) => {
    mockSearchBoxProps({ options, placeholder, value });

    return (
      <div data-testid="mapbox-searchbox">
      <input
        aria-label="Find Address"
        placeholder={placeholder}
        value={value ?? ""}
        onChange={(event) => onChange?.(event.target.value)}
      />
      <button
        type="button"
        onClick={() =>
          onRetrieve?.({
            type: "FeatureCollection",
            features: [
              {
                type: "Feature",
                geometry: {
                  type: "Point",
                  coordinates: [-74.0113353, 40.7033938],
                },
                properties: {
                  feature_type: "address",
                  address: "54 Pearl Street",
                  context: {
                    country: {
                      id: "country.1",
                      name: "United States",
                      country_code: "US",
                      country_code_alpha_3: "USA",
                    },
                    place: {
                      id: "place.1",
                      name: "New York",
                    },
                    postcode: {
                      id: "postcode.1",
                      name: "10004",
                    },
                    region: {
                      id: "region.1",
                      name: "New York",
                      region_code: "US-NY",
                      region_code_full: "US-NY",
                    },
                  },
                  coordinates: {
                    accuracy: "rooftop",
                    latitude: 40.7033938,
                    longitude: -74.0113353,
                    routable_points: [
                      {
                        name: "default",
                        latitude: 40.7033938,
                        longitude: -74.0113353,
                      },
                    ],
                  },
                  full_address:
                    "54 Pearl Street, New York, New York 10004, United States",
                  name: "Fraunces Tavern",
                },
              },
            ],
          })
        }
      >
        Use Mapbox suggestion
      </button>
      <button type="button" onClick={() => onClear?.()}>
        Clear Mapbox suggestion
      </button>
      </div>
    );
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

function getFutureDate(daysFromNow = 2): string {
  const date = new Date();
  date.setDate(date.getDate() + daysFromNow);
  return date.toISOString().slice(0, 10);
}

describe("ParcelRegistrationForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockDepots.splice(0, mockDepots.length, {
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
    });

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
    const futureDate = getFutureDate();

    await user.selectOptions(screen.getByTestId("shipperAddressId"), "address-1");
    fireEvent.change(screen.getByPlaceholderText("123 Main St"), {
      target: { value: "123 Main St" },
    });
    fireEvent.change(screen.getByLabelText(/recipient name/i), {
      target: { value: "John Doe" },
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
      target: { value: futureDate },
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

  it("autofills recipient address fields from the Mapbox search selection", async () => {
    render(<ParcelRegistrationForm />);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /use mapbox suggestion/i }));

    expect(screen.getByPlaceholderText("123 Main St")).toHaveValue(
      "54 Pearl Street",
    );
    expect(screen.getByLabelText(/^city/i)).toHaveValue("New York");
    expect(screen.getByLabelText(/state \/ province/i)).toHaveValue("NY");
    expect(screen.getByLabelText(/postal code/i)).toHaveValue("10004");
    expect(screen.getByLabelText(/country code/i)).toHaveValue("US");
    expect(
      screen.getByText(/address matched from search/i),
    ).toBeInTheDocument();
  });

  it("submits autofilled address values from the Mapbox search flow", async () => {
    render(<ParcelRegistrationForm />);

    const user = userEvent.setup();
    const futureDate = getFutureDate();
    await user.selectOptions(screen.getByTestId("shipperAddressId"), "address-1");
    await user.click(screen.getByRole("button", { name: /use mapbox suggestion/i }));
    fireEvent.change(screen.getByLabelText(/recipient name/i), {
      target: { value: "John Doe" },
    });
    fireEvent.change(screen.getByLabelText(/est\. delivery date/i), {
      target: { value: futureDate },
    });

    await user.click(screen.getByRole("button", { name: /register parcel/i }));

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith(
        expect.objectContaining({
          recipientCity: "New York",
          recipientCountryCode: "US",
          recipientPostalCode: "10004",
          recipientState: "NY",
          recipientStreet1: "54 Pearl Street",
          shipperAddressId: "address-1",
        }),
      );
    });
  });

  it("configures Mapbox search to favor address results in the depot country", async () => {
    render(<ParcelRegistrationForm />);

    const user = userEvent.setup();
    await user.selectOptions(screen.getByTestId("shipperAddressId"), "address-1");

    await waitFor(() => {
      expect(mockSearchBoxProps).toHaveBeenLastCalledWith(
        expect.objectContaining({
          options: expect.objectContaining({
            country: "US",
            types: "address,street,block",
          }),
        }),
      );
    });

    await user.type(screen.getByLabelText("Find Address"), "54 ");

    await waitFor(() => {
      expect(mockSearchBoxProps).toHaveBeenLastCalledWith(
        expect.objectContaining({
          options: expect.objectContaining({
            country: "US",
            types: "address",
          }),
        }),
      );
    });
  });

  it("falls back to proximity-only search when the depot country code is invalid", async () => {
    mockDepots[0] = {
      ...mockDepots[0],
      address: {
        ...mockDepots[0].address,
        city: "New York",
        countryCode: "NY",
        state: "NY",
      },
    };

    render(<ParcelRegistrationForm />);

    const user = userEvent.setup();
    await user.selectOptions(screen.getByTestId("shipperAddressId"), "address-1");

    await waitFor(() => {
      expect(mockSearchBoxProps).toHaveBeenLastCalledWith(
        expect.objectContaining({
          options: expect.not.objectContaining({
            country: expect.anything(),
          }),
        }),
      );
    });

    expect(screen.queryByText(/country locked to/i)).not.toBeInTheDocument();
  });

  it("normalizes alpha-3 depot country codes before passing them to Mapbox", async () => {
    mockDepots[0] = {
      ...mockDepots[0],
      address: {
        ...mockDepots[0].address,
        city: "Sydney",
        countryCode: "AUS",
        postalCode: "2000",
        state: "NSW",
      },
      name: "Sydney Depot",
    };

    render(<ParcelRegistrationForm />);

    const user = userEvent.setup();
    await user.selectOptions(screen.getByTestId("shipperAddressId"), "address-1");

    await waitFor(() => {
      expect(mockSearchBoxProps).toHaveBeenLastCalledWith(
        expect.objectContaining({
          options: expect.objectContaining({
            country: "AU",
          }),
        }),
      );
    });

    expect(screen.getByText(/country locked to au/i)).toBeInTheDocument();
  });

  it("shows success actions and calls the download/detail flows", async () => {
    render(<ParcelRegistrationForm onViewQueue={mockOnViewQueue} />);

    const user = await fillRequiredFields();
    await user.click(screen.getByRole("button", { name: /register parcel/i }));

    expect(await screen.findByText(/parcel registered/i)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /download 4x6 zpl/i }));
    await user.click(screen.getByRole("button", { name: /download a4 pdf/i }));
    await user.click(screen.getByRole("button", { name: /open parcel detail/i }));
    await user.click(screen.getByRole("button", { name: /view pre-load queue/i }));

    await waitFor(() => {
      expect(mockDownloadLabel).toHaveBeenNthCalledWith(1, "parcel-1", "zpl");
      expect(mockDownloadLabel).toHaveBeenNthCalledWith(2, "parcel-1", "pdf");
    });

    expect(mockPush).toHaveBeenCalledWith("/parcels/LM202604010001");
    expect(mockOnViewQueue).toHaveBeenCalledTimes(1);
    expect(mockPush).toHaveBeenCalledWith("/parcels");
  }, 15000);
});
