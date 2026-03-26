import { describe, expect, it } from "vitest";

import {
  dayOfWeekFromIndex,
  dayOfWeekToIndex,
  normalizeDepot,
  serializeDepotOperatingHours,
} from "../depot-operating-hours";

describe("depot operating hours helpers", () => {
  it("serializes numeric day indexes to GraphQL enum values", () => {
    expect(
      serializeDepotOperatingHours([
        {
          dayOfWeek: 1,
          openTime: "08:00",
          closedTime: "17:00",
          isClosed: false,
        },
      ])
    ).toEqual([
      {
        dayOfWeek: "MONDAY",
        openTime: "08:00",
        closedTime: "17:00",
        isClosed: false,
      },
    ]);
  });

  it("normalizes GraphQL enum values back to numeric day indexes", () => {
    expect(
      normalizeDepot({
        id: "depot-1",
        name: "Depot",
        address: null,
        operatingHours: [
          {
            dayOfWeek: "WEDNESDAY",
            openTime: "08:00:00",
            closedTime: "17:00:00",
            isClosed: false,
          },
        ],
        isActive: true,
        createdAt: "2026-03-25T00:00:00Z",
        updatedAt: null,
      }).operatingHours
    ).toEqual([
      {
        dayOfWeek: 3,
        openTime: "08:00:00",
        closedTime: "17:00:00",
        isClosed: false,
      },
    ]);
  });

  it("supports both enum strings and numeric string fallbacks", () => {
    expect(dayOfWeekToIndex("SATURDAY")).toBe(6);
    expect(dayOfWeekToIndex("2")).toBe(2);
    expect(dayOfWeekFromIndex(4)).toBe("THURSDAY");
  });
});
