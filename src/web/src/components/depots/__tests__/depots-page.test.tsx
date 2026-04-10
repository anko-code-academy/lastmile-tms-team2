import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import DepotsPage from "@/components/depots/depots-page";

const {
  createDepotMutateAsync,
  deleteDepotMutateAsync,
  mockSearchBoxProps,
  updateDepotMutateAsync,
} = vi.hoisted(() => ({
  createDepotMutateAsync: vi.fn(),
  deleteDepotMutateAsync: vi.fn(),
  mockSearchBoxProps: vi.fn(),
  updateDepotMutateAsync: vi.fn(),
}));

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
  }),
}));

vi.mock("@/lib/mapbox/config", () => ({
  getMapboxAccessToken: () => "pk.test-token",
  getMapboxConfigurationError: () => null,
}));

vi.mock("@/queries/depots", () => ({
  useDepots: () => ({
    data: [],
    isLoading: false,
    error: null,
  }),
  useCreateDepot: () => ({
    mutateAsync: createDepotMutateAsync,
    isPending: false,
  }),
  useUpdateDepot: () => ({
    mutateAsync: updateDepotMutateAsync,
    isPending: false,
  }),
  useDeleteDepot: () => ({
    mutateAsync: deleteDepotMutateAsync,
    isPending: false,
  }),
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
          aria-label="Find address"
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
                    coordinates: [144.9631, -37.8136],
                  },
                  properties: {
                    feature_type: "address",
                    address: "500 Collins Street",
                    context: {
                      country: {
                        id: "country.1",
                        name: "Australia",
                        country_code: "AU",
                        country_code_alpha_3: "AUS",
                      },
                      place: {
                        id: "place.1",
                        name: "Melbourne",
                      },
                      postcode: {
                        id: "postcode.1",
                        name: "3000",
                      },
                      region: {
                        id: "region.1",
                        name: "Victoria",
                        region_code: "AU-VIC",
                        region_code_full: "AU-VIC",
                      },
                    },
                    coordinates: {
                      accuracy: "rooftop",
                      latitude: -37.8136,
                      longitude: 144.9631,
                      routable_points: [
                        {
                          name: "default",
                          latitude: -37.8136,
                          longitude: 144.9631,
                        },
                      ],
                    },
                    full_address:
                      "500 Collins Street, Melbourne, Victoria 3000, Australia",
                    name: "500 Collins Street",
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

describe("DepotsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    createDepotMutateAsync.mockResolvedValue({});
    updateDepotMutateAsync.mockResolvedValue({});
    deleteDepotMutateAsync.mockResolvedValue({});
  });

  it("autofills depot address fields from the Mapbox search selection", async () => {
    render(<DepotsPage />);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /add depot/i }));

    await waitFor(() => {
      expect(mockSearchBoxProps).toHaveBeenLastCalledWith(
        expect.objectContaining({
          options: expect.objectContaining({
            country: "AU",
            types: "address,street,block",
          }),
        }),
      );
    });

    await user.click(screen.getByRole("button", { name: /use mapbox suggestion/i }));

    expect(screen.getByLabelText(/street address/i)).toHaveValue(
      "500 Collins Street",
    );
    expect(screen.getByLabelText(/^city/i)).toHaveValue("Melbourne");
    expect(screen.getByLabelText(/^state/i)).toHaveValue("VIC");
    expect(screen.getByLabelText(/postal code/i)).toHaveValue("3000");
    expect(screen.getByLabelText(/country code/i)).toHaveValue("AU");
    expect(
      screen.getByText(/address matched from search/i),
    ).toBeInTheDocument();
  });

  it("submits the autofilled depot address values when creating a depot", async () => {
    render(<DepotsPage />);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /add depot/i }));
    await user.type(screen.getByLabelText(/depot name/i), "Melbourne Central");
    await user.click(screen.getByRole("button", { name: /use mapbox suggestion/i }));
    await user.click(screen.getByRole("button", { name: /create depot/i }));

    await waitFor(() => {
      expect(createDepotMutateAsync).toHaveBeenCalledWith({
        name: "Melbourne Central",
        address: {
          street1: "500 Collins Street",
          city: "Melbourne",
          state: "VIC",
          postalCode: "3000",
          countryCode: "AU",
          isResidential: false,
        },
        operatingHours: [
          {
            dayOfWeek: 1,
            openTime: "08:00",
            closedTime: "17:00",
            isClosed: false,
          },
          {
            dayOfWeek: 2,
            openTime: "08:00",
            closedTime: "17:00",
            isClosed: false,
          },
          {
            dayOfWeek: 3,
            openTime: "08:00",
            closedTime: "17:00",
            isClosed: false,
          },
          {
            dayOfWeek: 4,
            openTime: "08:00",
            closedTime: "17:00",
            isClosed: false,
          },
          {
            dayOfWeek: 5,
            openTime: "08:00",
            closedTime: "17:00",
            isClosed: false,
          },
        ],
        isActive: true,
      });
    });
  });
});
