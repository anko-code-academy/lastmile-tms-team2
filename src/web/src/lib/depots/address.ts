import type { DepotAddress } from "@/types/depots";

function compactParts(parts: Array<string | null | undefined>): string[] {
  return parts.filter((value): value is string => Boolean(value?.trim()));
}

export function formatDepotLocation(address: DepotAddress | null | undefined): string | null {
  if (!address) {
    return null;
  }

  const parts = compactParts([
    address.city,
    address.state,
    address.postalCode,
    address.countryCode,
  ]);

  return parts.length > 0 ? parts.join(", ") : null;
}

export function formatDepotAddressLabel(address: DepotAddress | null | undefined): string | null {
  if (!address) {
    return null;
  }

  const location = formatDepotLocation(address);
  const parts = compactParts([
    address.street1,
    address.street2,
    location,
  ]);

  return parts.length > 0 ? parts.join(", ") : null;
}
