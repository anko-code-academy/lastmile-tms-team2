import { describe, expect, it } from "vitest";

import {
  canRouteParcelToExceptionFromSortStation,
  getSortTargetBinsCaption,
  PARCEL_SORT_BLOCK_CODES,
  sortStationExceptionDescription,
} from "../sort-station";

describe("canRouteParcelToExceptionFromSortStation", () => {
  it("allows PascalCase statuses from API", () => {
    expect(canRouteParcelToExceptionFromSortStation("Registered")).toBe(true);
    expect(canRouteParcelToExceptionFromSortStation("ReceivedAtDepot")).toBe(true);
  });

  it("allows GraphQL-style enum names", () => {
    expect(canRouteParcelToExceptionFromSortStation("REGISTERED")).toBe(true);
    expect(canRouteParcelToExceptionFromSortStation("RECEIVED_AT_DEPOT")).toBe(true);
  });

  it("rejects other lifecycle statuses", () => {
    expect(canRouteParcelToExceptionFromSortStation("Sorted")).toBe(false);
    expect(canRouteParcelToExceptionFromSortStation(undefined)).toBe(false);
  });
});

describe("sortStationExceptionDescription", () => {
  it("maps known block codes", () => {
    expect(sortStationExceptionDescription(PARCEL_SORT_BLOCK_CODES.NO_TARGET_BINS)).toContain(
      "no target bins",
    );
    expect(sortStationExceptionDescription(PARCEL_SORT_BLOCK_CODES.WRONG_STATUS)).toContain(
      "not ready to sort",
    );
  });

  it("uses default for unknown codes", () => {
    expect(sortStationExceptionDescription("OTHER")).toContain("exception area");
    expect(sortStationExceptionDescription(null)).toContain("exception area");
  });
});

describe("getSortTargetBinsCaption", () => {
  it("describes populated bin list", () => {
    expect(
      getSortTargetBinsCaption({
        targetBinCount: 2,
        canSort: true,
        blockReasonCode: null,
      }),
    ).toContain("2 active bin");
  });

  it("uses NO_TARGET_BINS copy when blocked for bins", () => {
    expect(
      getSortTargetBinsCaption({
        targetBinCount: 0,
        canSort: false,
        blockReasonCode: PARCEL_SORT_BLOCK_CODES.NO_TARGET_BINS,
      }),
    ).toContain("No bins configured");
  });

  it("explains empty list when blocked for other reasons", () => {
    expect(
      getSortTargetBinsCaption({
        targetBinCount: 0,
        canSort: false,
        blockReasonCode: PARCEL_SORT_BLOCK_CODES.WRONG_STATUS,
      }),
    ).toContain("received at this depot");
  });
});
