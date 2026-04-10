import { afterEach, describe, expect, it, vi } from "vitest";
import { geocodeDepotAddress } from "@/lib/mapbox/geocoding";

describe("geocodeDepotAddress", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("requests a temporary Mapbox geocode without browser caching", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({
        features: [{ center: [144.9631, -37.8136] }],
      }),
    } as Response);

    const result = await geocodeDepotAddress(
      {
        street1: "1 Market Street",
        street2: null,
        city: "Melbourne",
        state: "VIC",
        postalCode: "3000",
        countryCode: "AU",
        isResidential: false,
        contactName: null,
        companyName: null,
        phone: null,
        email: null,
        geoLocation: null,
      },
      "pk.test-token",
    );

    expect(result).toEqual({
      longitude: 144.9631,
      latitude: -37.8136,
    });
    expect(fetchMock).toHaveBeenCalledTimes(1);

    const [requestUrl, requestInit] = fetchMock.mock.calls[0] ?? [];
    const url = requestUrl instanceof URL ? requestUrl : new URL(String(requestUrl));

    expect(url.searchParams.get("permanent")).toBe("false");
    expect(url.searchParams.get("access_token")).toBe("pk.test-token");
    expect(requestInit).toMatchObject({
      cache: "no-store",
    });
  });
});
