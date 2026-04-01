import { describe, expect, it } from "vitest";
import { normalizeDepot } from "@/lib/depots/operating-hours";

describe("normalizeDepot", () => {
  it("preserves depot geoLocation while normalizing operating hours", () => {
    const depot = normalizeDepot({
      id: "depot-1",
      name: "Main Depot",
      address: {
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
        geoLocation: {
          latitude: -37.8136,
          longitude: 144.9631,
        },
      },
      operatingHours: [
        {
          dayOfWeek: "MONDAY",
          openTime: "08:00:00",
          closedTime: "17:00:00",
          isClosed: false,
        },
      ],
      isActive: true,
      createdAt: "2026-03-01T00:00:00Z",
      updatedAt: null,
    });

    expect(depot.address?.geoLocation).toEqual({
      latitude: -37.8136,
      longitude: 144.9631,
    });
    expect(depot.operatingHours?.[0]?.dayOfWeek).toBe(1);
  });
});
