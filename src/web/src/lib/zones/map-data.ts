import bbox from "@turf/bbox";
import centroid from "@turf/centroid";
import type { Feature, FeatureCollection, Point, Polygon } from "geojson";
import { formatDepotAddressLabel } from "@/lib/depots/address";
import type { Depot, DepotGeoLocation } from "@/types/depots";
import type { Zone } from "@/types/zones";

export interface ZoneOverlayFeatureProperties {
  zoneId: string;
  zoneName: string;
  fillColor: string;
  lineColor: string;
  isHovered: boolean;
  isSelected: boolean;
}

export interface ZoneLabelDatum {
  id: string;
  name: string;
  coordinates: [number, number];
  color: string;
  isSelected: boolean;
}

export interface DepotMarkerDatum {
  id: string;
  name: string;
  locationLabel: string | null;
  coordinates: [number, number];
  isActive: boolean;
  isFallback: boolean;
}

export interface ZoneOverlayData {
  polygons: FeatureCollection<Polygon, ZoneOverlayFeatureProperties>;
  labels: ZoneLabelDatum[];
  bounds: [number, number, number, number] | null;
}

function isPolygonGeometry(value: unknown): value is Polygon {
  return (
    typeof value === "object"
    && value !== null
    && "type" in value
    && "coordinates" in value
    && (value as { type?: string }).type === "Polygon"
  );
}

function hashText(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i += 1) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

export function getZoneColor(id: string): { fillColor: string; lineColor: string } {
  const hue = hashText(id) % 360;
  return {
    fillColor: `hsl(${hue} 78% 56%)`,
    lineColor: `hsl(${hue} 82% 28%)`,
  };
}

export function parseZonePolygon(boundaryGeoJson: string | null | undefined): Polygon | null {
  if (!boundaryGeoJson) {
    return null;
  }

  try {
    const parsed = JSON.parse(boundaryGeoJson) as Polygon | Feature<Polygon>;
    if (isPolygonGeometry(parsed)) {
      return parsed;
    }

    if (parsed.type === "Feature" && isPolygonGeometry(parsed.geometry)) {
      return parsed.geometry;
    }
  } catch {
    return null;
  }

  return null;
}

export function buildZoneOverlayData(
  zones: Zone[],
  options: {
    editableZoneId?: string | null;
    hoveredZoneId?: string | null;
    selectedZoneId?: string | null;
  } = {},
): ZoneOverlayData {
  const polygonFeatures: Array<Feature<Polygon, ZoneOverlayFeatureProperties>> = [];
  const labelData: ZoneLabelDatum[] = [];

  for (const zone of zones) {
    if (options.editableZoneId && zone.id === options.editableZoneId) {
      continue;
    }

    const polygon = parseZonePolygon(zone.boundaryGeoJson);
    if (!polygon) {
      continue;
    }

    const colors = getZoneColor(zone.id);
    const polygonFeature: Feature<Polygon, ZoneOverlayFeatureProperties> = {
      type: "Feature",
      id: zone.id,
      properties: {
        zoneId: zone.id,
        zoneName: zone.name,
        fillColor: colors.fillColor,
        lineColor: colors.lineColor,
        isHovered: zone.id === options.hoveredZoneId,
        isSelected: zone.id === options.selectedZoneId,
      },
      geometry: polygon,
    };

    polygonFeatures.push(polygonFeature);

    const labelPoint = centroid(polygonFeature as Feature<Polygon, ZoneOverlayFeatureProperties>);
    labelData.push({
      id: zone.id,
      name: zone.name,
      coordinates: labelPoint.geometry.coordinates as [number, number],
      color: colors.lineColor,
      isSelected: zone.id === options.selectedZoneId,
    });
  }

  const polygons: FeatureCollection<Polygon, ZoneOverlayFeatureProperties> = {
    type: "FeatureCollection",
    features: polygonFeatures,
  };

  return {
    polygons,
    labels: labelData,
    bounds: polygonFeatures.length > 0 ? (bbox(polygons) as [number, number, number, number]) : null,
  };
}

function getDepotMarkerCoordinates(
  depot: Depot | undefined,
  activeDepotId: string | null,
  fallbackDepotLocation: DepotGeoLocation | null,
): [number, number] | null {
  if (!depot) {
    return null;
  }

  if (depot.address?.geoLocation) {
    return [depot.address.geoLocation.longitude, depot.address.geoLocation.latitude];
  }

  if (fallbackDepotLocation && depot.id === activeDepotId) {
    return [fallbackDepotLocation.longitude, fallbackDepotLocation.latitude];
  }

  return null;
}

export function buildDepotMarkerData(
  depots: Depot[],
  zones: Zone[],
  activeDepotId: string | null,
  fallbackDepotLocation: DepotGeoLocation | null,
): DepotMarkerDatum[] {
  const depotIds = new Set(zones.map((zone) => zone.depotId));
  if (activeDepotId) {
    depotIds.add(activeDepotId);
  }

  const markers: DepotMarkerDatum[] = [];

  for (const depotId of depotIds) {
    const depot = depots.find((candidate) => candidate.id === depotId);
    const coordinates = getDepotMarkerCoordinates(depot, activeDepotId, fallbackDepotLocation);
    if (!depot || !coordinates) {
      continue;
    }

    markers.push({
      id: depot.id,
      name: depot.name,
      locationLabel: formatDepotAddressLabel(depot.address),
      coordinates,
      isActive: depot.id === activeDepotId,
      isFallback: Boolean(!depot.address?.geoLocation && fallbackDepotLocation && depot.id === activeDepotId),
    });
  }

  return markers;
}

export function buildViewportBounds(
  overlayData: ZoneOverlayData,
  depotMarkers: DepotMarkerDatum[],
  draftGeometry: Polygon | null,
): [number, number, number, number] | null {
  const features: Array<Feature<Polygon | Point>> = [];

  for (const feature of overlayData.polygons.features) {
    features.push(feature as Feature<Polygon>);
  }

  if (draftGeometry) {
    features.push({
      type: "Feature",
      geometry: draftGeometry,
      properties: {},
    });
  }

  for (const depotMarker of depotMarkers) {
    features.push({
      type: "Feature",
      geometry: {
        type: "Point",
        coordinates: depotMarker.coordinates,
      },
      properties: {},
    });
  }

  if (features.length === 0) {
    return null;
  }

  return bbox({
    type: "FeatureCollection",
    features,
  }) as [number, number, number, number];
}
