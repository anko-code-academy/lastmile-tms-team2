import type { DepotAddress, DepotGeoLocation } from "@/types/depots";

function compactAddressParts(address: DepotAddress): string[] {
  return [
    address.street1,
    address.street2,
    address.city,
    address.state,
    address.postalCode,
    address.countryCode,
  ].filter((value): value is string => Boolean(value?.trim()));
}

export function formatDepotGeocodingQuery(address: DepotAddress): string {
  return compactAddressParts(address).join(", ");
}

export async function geocodeDepotAddress(
  address: DepotAddress,
  accessToken: string,
  signal?: AbortSignal,
): Promise<DepotGeoLocation | null> {
  const query = formatDepotGeocodingQuery(address);
  if (!query) {
    return null;
  }

  const url = new URL(
    `https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(query)}.json`,
  );
  url.searchParams.set("access_token", accessToken);
  url.searchParams.set("limit", "1");
  url.searchParams.set("permanent", "false");

  const response = await fetch(url, { signal, cache: "no-store" });
  if (!response.ok) {
    return null;
  }

  const payload = (await response.json()) as {
    features?: Array<{ center?: [number, number] }>;
  };

  const center = payload.features?.[0]?.center;
  if (!center || center.length < 2) {
    return null;
  }

  return {
    longitude: center[0],
    latitude: center[1],
  };
}
