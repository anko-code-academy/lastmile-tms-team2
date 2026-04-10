"use client";

import { useEffect, useRef, useState } from "react";
import type { Feature, FeatureCollection, LineString } from "geojson";
import type mapboxgl from "mapbox-gl";
import { getMapboxAccessToken, getZoneMapStyle } from "@/lib/mapbox/config";
import { cn } from "@/lib/utils";
import type { RoutePathPoint, RouteStop } from "@/types/routes";

const INITIAL_CENTER: [number, number] = [0, 18];
const INITIAL_ZOOM = 1.5;
const PATH_SOURCE_ID = "route-path";
const PATH_CASING_LAYER_ID = "route-path-casing";
const OUTBOUND_PATH_LAYER_ID = "route-path-outbound";
const RETURN_PATH_LAYER_ID = "route-path-return";
const OUTBOUND_PATH_COLOR = "#0f766e";
const RETURN_PATH_COLOR = "#b45309";

type RoutePathSegment = "outbound" | "return";
type RoutePathFeatureProperties = {
  segment: RoutePathSegment;
};

type MapboxModule = typeof import("mapbox-gl")["default"];

type RouteMapDepot = {
  name: string;
  addressLine?: string | null;
  longitude?: number | null;
  latitude?: number | null;
};

function buildLineFeature(
  path: RoutePathPoint[],
  segment: RoutePathSegment,
): Feature<LineString, RoutePathFeatureProperties> | null {
  if (path.length < 2) {
    return null;
  }

  return {
    type: "Feature",
    properties: {
      segment,
    },
    geometry: {
      type: "LineString",
      coordinates: path.map((point) => [point.longitude, point.latitude]),
    },
  };
}

function findReturnSplitIndex(path: RoutePathPoint[], stops: RouteStop[]): number | null {
  if (path.length < 2 || stops.length === 0) {
    return null;
  }

  const lastStop = stops.reduce((furthestStop, currentStop) =>
    currentStop.sequence > furthestStop.sequence ? currentStop : furthestStop,
  );

  let bestIndex = -1;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (let index = 0; index < path.length; index += 1) {
    const point = path[index];
    const longitudeDelta = point.longitude - lastStop.longitude;
    const latitudeDelta = point.latitude - lastStop.latitude;
    const squaredDistance = longitudeDelta * longitudeDelta + latitudeDelta * latitudeDelta;

    if (
      squaredDistance < bestDistance
      || (Math.abs(squaredDistance - bestDistance) < 0.000000000001 && index > bestIndex)
    ) {
      bestDistance = squaredDistance;
      bestIndex = index;
    }
  }

  if (bestIndex <= 0 || bestIndex >= path.length - 1) {
    return null;
  }

  return bestIndex;
}

function buildPathFeature(
  path: RoutePathPoint[],
  stops: RouteStop[],
): FeatureCollection<LineString, RoutePathFeatureProperties> {
  const splitIndex = findReturnSplitIndex(path, stops);
  const outboundPath = splitIndex == null ? path : path.slice(0, splitIndex + 1);
  const returnPath = splitIndex == null ? [] : path.slice(splitIndex);
  const features = [
    buildLineFeature(outboundPath, "outbound"),
    buildLineFeature(returnPath, "return"),
  ].filter((feature): feature is Feature<LineString, RoutePathFeatureProperties> => feature !== null);

  return {
    type: "FeatureCollection",
    features,
  };
}

export function RouteMap({
  path,
  stops,
  depot,
  className,
  emptyMessage = "Route preview will appear here once stops are planned.",
}: {
  path: RoutePathPoint[];
  stops: RouteStop[];
  depot?: RouteMapDepot | null;
  className?: string;
  emptyMessage?: string;
}) {
  const accessToken = getMapboxAccessToken() ?? "";
  const mapStyle = getZoneMapStyle(accessToken);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<mapboxgl.Map | null>(null);
  const mapboxModuleRef = useRef<MapboxModule | null>(null);
  const markersRef = useRef<mapboxgl.Marker[]>([]);
  const [mapLoaded, setMapLoaded] = useState(false);
  const depotLongitude = depot?.longitude ?? null;
  const depotLatitude = depot?.latitude ?? null;
  const hasDepotCoordinates = depotLongitude != null && depotLatitude != null;
  const hasGeometry = path.length > 0 || stops.length > 0 || hasDepotCoordinates;

  useEffect(() => {
    let isCancelled = false;

    async function initializeMap() {
      if (!containerRef.current || mapRef.current) {
        return;
      }

      const mapboxModule = await import("mapbox-gl");
      if (isCancelled || !containerRef.current) {
        return;
      }

      const mapbox = mapboxModule.default;
      mapbox.accessToken = accessToken;
      mapboxModuleRef.current = mapbox;

      const map = new mapbox.Map({
        container: containerRef.current,
        style: mapStyle,
        center: INITIAL_CENTER,
        zoom: INITIAL_ZOOM,
        attributionControl: true,
        dragRotate: false,
        pitchWithRotate: false,
        projection: "mercator",
        renderWorldCopies: false,
      });

      map.addControl(
        new mapbox.NavigationControl({
          showCompass: false,
        }),
        "top-right",
      );
      map.addControl(
        new mapbox.ScaleControl({
          maxWidth: 120,
          unit: "metric",
        }),
        "bottom-right",
      );

      map.on("load", () => {
        if (isCancelled) {
          return;
        }

        map.addSource(PATH_SOURCE_ID, {
          type: "geojson",
          data: buildPathFeature([], []),
        });

        map.addLayer({
          id: PATH_CASING_LAYER_ID,
          type: "line",
          source: PATH_SOURCE_ID,
          paint: {
            "line-color": "#f8fafc",
            "line-width": 7,
            "line-opacity": 0.95,
          },
          layout: {
            "line-join": "round",
            "line-cap": "round",
          },
        });

        map.addLayer({
          id: OUTBOUND_PATH_LAYER_ID,
          type: "line",
          source: PATH_SOURCE_ID,
          filter: ["==", ["get", "segment"], "outbound"],
          paint: {
            "line-color": OUTBOUND_PATH_COLOR,
            "line-width": 4.5,
            "line-opacity": 0.94,
          },
          layout: {
            "line-join": "round",
            "line-cap": "round",
          },
        });

        map.addLayer({
          id: RETURN_PATH_LAYER_ID,
          type: "line",
          source: PATH_SOURCE_ID,
          filter: ["==", ["get", "segment"], "return"],
          paint: {
            "line-color": RETURN_PATH_COLOR,
            "line-width": 4.5,
            "line-opacity": 0.94,
            "line-dasharray": [1.25, 1.1],
          },
          layout: {
            "line-join": "round",
            "line-cap": "round",
          },
        });

        setMapLoaded(true);
      });

      mapRef.current = map;
    }

    void initializeMap();

    return () => {
      isCancelled = true;
      markersRef.current.forEach((marker) => marker.remove());
      markersRef.current = [];
      setMapLoaded(false);
      mapRef.current?.remove();
      mapRef.current = null;
    };
  }, [accessToken, mapStyle]);

  useEffect(() => {
    const map = mapRef.current;
    if (!mapLoaded || !map) {
      return;
    }

    const source = map.getSource(PATH_SOURCE_ID) as mapboxgl.GeoJSONSource | undefined;
    source?.setData(buildPathFeature(path, stops));
  }, [mapLoaded, path, stops]);

  useEffect(() => {
    const map = mapRef.current;
    const mapbox = mapboxModuleRef.current;
    if (!mapLoaded || !map || !mapbox) {
      return;
    }

    markersRef.current.forEach((marker) => marker.remove());
    const markers: mapboxgl.Marker[] = [];

    if (hasDepotCoordinates) {
      const depotCoordinates: [number, number] = [depotLongitude, depotLatitude];
      const element = document.createElement("button");
      element.type = "button";
      element.className =
        "flex size-9 items-center justify-center rounded-full border-2 border-white bg-emerald-600 text-xs font-black text-white shadow-lg";
      element.textContent = "D";

      markers.push(
        new mapbox.Marker({
          element,
          anchor: "center",
        })
          .setLngLat(depotCoordinates)
          .setPopup(
            new mapbox.Popup({
              closeButton: false,
              offset: 18,
            }).setHTML(
              `<div class="space-y-1">
                <div class="text-sm font-semibold">${depot?.name ?? "Depot"}</div>
                ${depot?.addressLine ? `<div class="text-xs text-slate-600">${depot.addressLine}</div>` : ""}
                <div class="text-xs text-slate-500">Route start and finish</div>
              </div>`,
            ),
          )
          .addTo(map),
      );
    }

    markers.push(...stops.map((stop) => {
      const element = document.createElement("button");
      element.type = "button";
      element.className =
        "flex size-8 items-center justify-center rounded-full border-2 border-white bg-slate-950 text-xs font-bold text-white shadow-lg";
      element.textContent = String(stop.sequence);

      return new mapbox.Marker({
        element,
        anchor: "center",
      })
        .setLngLat([stop.longitude, stop.latitude])
        .setPopup(
          new mapbox.Popup({
            closeButton: false,
            offset: 18,
          }).setHTML(
            `<div class="space-y-1">
              <div class="text-sm font-semibold">${stop.sequence}. ${stop.recipientLabel}</div>
              <div class="text-xs text-slate-600">${stop.addressLine}</div>
              <div class="text-xs text-slate-500">${stop.parcels.length} parcel${stop.parcels.length === 1 ? "" : "s"}</div>
            </div>`,
          ),
        )
        .addTo(map);
    }));

    markersRef.current = markers;

    return () => {
      markersRef.current.forEach((marker) => marker.remove());
      markersRef.current = [];
    };
  }, [depot?.addressLine, depot?.name, depotLatitude, depotLongitude, hasDepotCoordinates, mapLoaded, stops]);

  useEffect(() => {
    const map = mapRef.current;
    const mapbox = mapboxModuleRef.current;
    if (!mapLoaded || !map || !mapbox || !hasGeometry) {
      return;
    }

    const bounds = new mapbox.LngLatBounds();
    for (const point of path) {
      bounds.extend([point.longitude, point.latitude]);
    }
    for (const stop of stops) {
      bounds.extend([stop.longitude, stop.latitude]);
    }
    if (hasDepotCoordinates) {
      const depotCoordinates: [number, number] = [depotLongitude, depotLatitude];
      bounds.extend(depotCoordinates);
    }

    if (bounds.isEmpty()) {
      return;
    }

    map.fitBounds(bounds, {
      padding: 56,
      duration: 600,
      maxZoom: 13,
    });
  }, [depotLatitude, depotLongitude, hasDepotCoordinates, hasGeometry, mapLoaded, path, stops]);

  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-[1.5rem] border border-border/60 bg-slate-100 shadow-[0_24px_64px_-36px_rgba(15,23,42,0.35)]",
        className,
      )}
    >
      <div ref={containerRef} className="h-[22rem] w-full sm:h-[28rem]" />
      {mapLoaded && hasGeometry ? (
        <div className="pointer-events-none absolute left-4 top-4 z-10 flex flex-wrap gap-2">
          <div className="flex items-center gap-2 rounded-full border border-white/70 bg-white/90 px-3 py-1.5 text-xs font-medium text-slate-700 shadow-lg backdrop-blur-sm">
            <span
              className="block h-1.5 w-8 rounded-full"
              style={{ backgroundColor: OUTBOUND_PATH_COLOR }}
            />
            To route stops
          </div>
          <div className="flex items-center gap-2 rounded-full border border-white/70 bg-white/90 px-3 py-1.5 text-xs font-medium text-slate-700 shadow-lg backdrop-blur-sm">
            <span
              className="block h-1.5 w-8 rounded-full"
              style={{
                backgroundColor: RETURN_PATH_COLOR,
                backgroundImage: `repeating-linear-gradient(90deg, ${RETURN_PATH_COLOR} 0 10px, rgba(255,255,255,0) 10px 14px)`,
              }}
            />
            Return to depot
          </div>
        </div>
      ) : null}
      {!mapLoaded ? (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-slate-200/70 text-sm font-medium text-slate-700 backdrop-blur-sm">
          Loading route map...
        </div>
      ) : null}
      {mapLoaded && !hasGeometry ? (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-slate-950/10 px-6 text-center text-sm font-medium text-slate-700">
          {emptyMessage}
        </div>
      ) : null}
    </div>
  );
}

export default RouteMap;
