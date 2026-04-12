import { normalizeParcelStatusForFilter } from "@/lib/labels/parcels";

/** Matches backend `GetParcelSortInstruction` block codes (Application layer). */
export const PARCEL_SORT_BLOCK_CODES = {
  NO_TARGET_BINS: "NO_TARGET_BINS",
  ZONE_INACTIVE: "ZONE_INACTIVE",
  WRONG_DEPOT: "WRONG_DEPOT",
  WRONG_STATUS: "WRONG_STATUS",
} as const;

export type ParcelSortBlockReasonCode =
  (typeof PARCEL_SORT_BLOCK_CODES)[keyof typeof PARCEL_SORT_BLOCK_CODES];

/**
 * Domain allows Registered → Exception and ReceivedAtDepot → Exception; keep in sync with API.
 * Status strings may be PascalCase (DTO `.ToString()`) or GraphQL-style UPPER_SNAKE.
 */
export function canRouteParcelToExceptionFromSortStation(status: string | undefined): boolean {
  const n = normalizeParcelStatusForFilter(status ?? "");
  return n === "REGISTERED" || n === "RECEIVED_AT_DEPOT";
}

export function sortStationExceptionDescription(
  blockReasonCode: string | null | undefined,
): string {
  switch (blockReasonCode) {
    case PARCEL_SORT_BLOCK_CODES.NO_TARGET_BINS:
      return "Unsortable from warehouse sort station: no target bins configured for this delivery zone (exception area).";
    case PARCEL_SORT_BLOCK_CODES.ZONE_INACTIVE:
      return "Unsortable from warehouse sort station: delivery zone inactive (exception area).";
    case PARCEL_SORT_BLOCK_CODES.WRONG_DEPOT:
      return "Unsortable from warehouse sort station: depot mismatch (exception area).";
    case PARCEL_SORT_BLOCK_CODES.WRONG_STATUS:
      return "Unsortable from warehouse sort station: parcel not ready to sort or on hold — route to exception for manual handling.";
    default:
      return "Unsortable from warehouse sort station (exception area).";
  }
}

export function getSortTargetBinsCaption(options: {
  targetBinCount: number;
  canSort: boolean;
  blockReasonCode: string | null | undefined;
}): string {
  const { targetBinCount, canSort, blockReasonCode } = options;
  if (targetBinCount > 0) {
    return `${targetBinCount} active bin(s) linked to this zone`;
  }
  if (blockReasonCode === PARCEL_SORT_BLOCK_CODES.NO_TARGET_BINS) {
    return "No bins configured for this zone in this depot";
  }
  if (!canSort) {
    return "Bin list appears after the parcel is received at this depot and ready to sort.";
  }
  return "No bins configured for this zone in this depot";
}
