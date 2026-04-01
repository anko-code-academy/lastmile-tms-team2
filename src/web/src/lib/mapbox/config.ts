import type { StyleSpecification } from "mapbox-gl";

export const MAPBOX_ZONE_STANDARD_STYLE_URL = "mapbox://styles/mapbox/standard";

const MAPBOX_ZONE_FALLBACK_STYLE: StyleSpecification = {
  version: 8,
  name: "LastMile Zone Studio Fallback",
  sources: {
    "osm-raster": {
      type: "raster",
      tiles: [
        "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
      ],
      tileSize: 256,
      attribution: "&copy; OpenStreetMap contributors",
    },
  },
  layers: [
    {
      id: "osm-raster-layer",
      type: "raster",
      source: "osm-raster",
      paint: {
        "raster-saturation": -0.08,
        "raster-contrast": 0.08,
        "raster-brightness-min": 0.02,
        "raster-brightness-max": 0.98,
      },
    },
  ],
};

export function getZoneMapStyle(accessToken: string): string | StyleSpecification {
  return accessToken.trim().startsWith("pk.")
    ? MAPBOX_ZONE_STANDARD_STYLE_URL
    : MAPBOX_ZONE_FALLBACK_STYLE;
}

export function getMapboxAccessToken(): string | null {
  const token = process.env.NEXT_PUBLIC_MAPBOX_ACCESS_TOKEN?.trim();
  return token ? token : null;
}

export function getMapboxConfigurationError(): string | null {
  return getMapboxAccessToken()
    ? null
    : "NEXT_PUBLIC_MAPBOX_ACCESS_TOKEN is not configured.";
}
