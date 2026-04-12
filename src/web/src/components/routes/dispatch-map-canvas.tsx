"use client";

import { useEffect, useRef, useState } from "react";
import type {
  Feature,
  FeatureCollection,
  LineString,
} from "geojson";
import type mapboxgl from "mapbox-gl";

import { getMapboxAccessToken, getZoneMapStyle } from "@/lib/mapbox/config";
import {
  DISPATCH_MAP_ROUTE_STATUS_COLORS,
  DISPATCH_MAP_STOP_STATUS_COLORS,
  getDispatchMapRouteBounds,
  stopStatusLabel,
} from "@/lib/routes/dispatch-map";
import { ROUTE_STATUS_LABELS, routeStatusBadgeClass } from "@/lib/labels/routes";
import {
  formatParcelStatus,
  parcelStatusBadgeClass,
} from "@/lib/labels/parcels";
import { cn } from "@/lib/utils";
import type { DispatchMapRoute } from "@/types/routes";

const INITIAL_CENTER: [number, number] = [0, 18];
const INITIAL_ZOOM = 1.5;

const ROUTES_SOURCE_ID = "dispatch-map-routes";
const ROUTES_CASING_LAYER_ID = "dispatch-map-routes-casing";
const ROUTES_LAYER_ID = "dispatch-map-routes-line";
const ROUTES_SELECTED_LAYER_ID = "dispatch-map-routes-selected";

type MapboxModule = typeof import("mapbox-gl")["default"];

type RouteLineFeatureProperties = {
  routeId: string;
  status: DispatchMapRoute["status"];
  vehiclePlate: string;
  driverName: string;
  zoneName: string;
  startDate: string;
  stopCount: number;
  parcelCount: number;
};

function escapeHtml(value: string | null | undefined): string {
  return (value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#39;");
}

function buildRouteLineCollection(
  routes: DispatchMapRoute[],
): FeatureCollection<LineString, RouteLineFeatureProperties> {
  const features: Array<Feature<LineString, RouteLineFeatureProperties>> = [];

  for (const route of routes) {
    if (route.path.length < 2) {
      continue;
    }

    features.push({
      type: "Feature",
      properties: {
        routeId: route.id,
        status: route.status,
        vehiclePlate: route.vehiclePlate,
        driverName: route.driverName,
        zoneName: route.zoneName,
        startDate: route.startDate,
        stopCount: route.estimatedStopCount,
        parcelCount: route.parcelCount,
      },
      geometry: {
        type: "LineString",
        coordinates: route.path.map((point) => [point.longitude, point.latitude]),
      },
    });
  }

  return {
    type: "FeatureCollection",
    features,
  };
}

function buildRoutePopupHtml(route: DispatchMapRoute): string {
  return `
    <div class="space-y-2">
      <div class="flex flex-wrap items-center gap-2">
        <div class="text-sm font-semibold">${escapeHtml(route.vehiclePlate)}</div>
        <span class="${routeStatusBadgeClass(route.status)}">${escapeHtml(ROUTE_STATUS_LABELS[route.status])}</span>
      </div>
      <div class="text-xs text-slate-600">${escapeHtml(route.driverName)} · ${escapeHtml(route.zoneName)}</div>
      <div class="text-xs text-slate-500">${escapeHtml(new Date(route.startDate).toLocaleString())}</div>
      <div class="text-xs text-slate-600">${route.estimatedStopCount} stop${route.estimatedStopCount === 1 ? "" : "s"} · ${route.parcelCount} parcel${route.parcelCount === 1 ? "" : "s"}</div>
      <a href="/routes/${escapeHtml(route.id)}" class="inline-flex text-xs font-medium text-blue-700 underline-offset-4 hover:underline">Open route</a>
    </div>
  `;
}

function buildDepotPopupHtml(route: DispatchMapRoute): string {
  return `
    <div class="space-y-2">
      <div class="text-sm font-semibold">${escapeHtml(route.depotName ?? "Depot")}</div>
      ${route.depotAddressLine ? `<div class="text-xs text-slate-600">${escapeHtml(route.depotAddressLine)}</div>` : ""}
      <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
        <div class="text-xs font-semibold text-slate-900">${escapeHtml(route.vehiclePlate)}</div>
        <div class="mt-1 text-xs text-slate-600">${escapeHtml(route.driverName)} В· ${escapeHtml(route.zoneName)}</div>
      </div>
      <div class="text-xs text-slate-500">Route start and finish</div>
      <a href="/routes/${escapeHtml(route.id)}" class="inline-flex text-xs font-medium text-blue-700 underline-offset-4 hover:underline">Open route</a>
    </div>
  `;
}

function buildStopPopupHtml(route: DispatchMapRoute, stop: DispatchMapRoute["stops"][number]): string {
  const parcelItems = stop.parcels
    .map((parcel) => `
      <li class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <a href="/parcels/${escapeHtml(parcel.parcelId)}" class="text-xs font-semibold text-blue-700 underline-offset-4 hover:underline">${escapeHtml(parcel.trackingNumber)}</a>
          <span class="${parcelStatusBadgeClass(parcel.status)}">${escapeHtml(formatParcelStatus(parcel.status))}</span>
        </div>
        <div class="mt-1 text-xs text-slate-700">${escapeHtml(parcel.recipientLabel)}</div>
      </li>
    `)
    .join("");

  return `
    <div class="space-y-3">
      <div class="space-y-1">
        <div class="flex flex-wrap items-center gap-2">
          <div class="text-sm font-semibold">Stop ${stop.sequence}</div>
          <span class="inline-flex rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">${escapeHtml(stopStatusLabel(stop.uiStatus))}</span>
        </div>
        <div class="text-xs text-slate-700">${escapeHtml(stop.recipientLabel)}</div>
        <div class="text-xs text-slate-500">${escapeHtml(stop.addressLine)}</div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
        <div class="text-xs font-semibold text-slate-900">${escapeHtml(route.vehiclePlate)}</div>
        <div class="mt-1 text-xs text-slate-600">${escapeHtml(route.driverName)} · ${escapeHtml(route.zoneName)}</div>
      </div>
      <ul class="space-y-2">${parcelItems}</ul>
    </div>
  `;
}

function extendBoundsWithRoute(
  bounds: mapboxgl.LngLatBounds,
  route: DispatchMapRoute,
) {
  for (const [longitude, latitude] of getDispatchMapRouteBounds(route)) {
    bounds.extend([longitude, latitude]);
  }

  if (
    route.depotLongitude != null
    && route.depotLatitude != null
  ) {
    bounds.extend([route.depotLongitude, route.depotLatitude]);
  }
}

export function DispatchMapCanvas({
  routes,
  selectedRouteId,
  onSelectRoute,
  className,
}: {
  routes: DispatchMapRoute[];
  selectedRouteId: string | null;
  onSelectRoute?: (routeId: string) => void;
  className?: string;
}) {
  const accessToken = getMapboxAccessToken() ?? "";
  const mapStyle = getZoneMapStyle(accessToken);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<mapboxgl.Map | null>(null);
  const mapboxModuleRef = useRef<MapboxModule | null>(null);
  const markersRef = useRef<mapboxgl.Marker[]>([]);
  const routePopupRef = useRef<mapboxgl.Popup | null>(null);
  const routesRef = useRef(routes);
  const onSelectRouteRef = useRef(onSelectRoute);
  const [mapLoaded, setMapLoaded] = useState(false);

  routesRef.current = routes;
  onSelectRouteRef.current = onSelectRoute;

  const hasAnyRoutes = routes.length > 0;
  const hasAnyGeometry = routes.some((route) => route.hasGeometry);

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

      const handleRouteClick = (event: mapboxgl.MapLayerMouseEvent) => {
        const feature = event.features?.[0];
        const routeId = feature?.properties?.routeId;
        if (typeof routeId !== "string") {
          return;
        }

        const route = routesRef.current.find((candidate) => candidate.id === routeId);
        if (!route) {
          return;
        }

        onSelectRouteRef.current?.(routeId);
        routePopupRef.current?.remove();
        routePopupRef.current = new mapbox.Popup({
          closeButton: false,
          offset: 18,
        })
          .setLngLat(event.lngLat)
          .setHTML(buildRoutePopupHtml(route))
          .addTo(map);
      };

      const setPointerCursor = () => {
        map.getCanvas().style.cursor = "pointer";
      };

      const clearPointerCursor = () => {
        map.getCanvas().style.cursor = "";
      };

      map.on("load", () => {
        if (isCancelled) {
          return;
        }

        map.addSource(ROUTES_SOURCE_ID, {
          type: "geojson",
          data: buildRouteLineCollection([]),
        });

        map.addLayer({
          id: ROUTES_CASING_LAYER_ID,
          type: "line",
          source: ROUTES_SOURCE_ID,
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
          id: ROUTES_LAYER_ID,
          type: "line",
          source: ROUTES_SOURCE_ID,
          paint: {
            "line-color": [
              "match",
              ["get", "status"],
              "DRAFT",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.DRAFT,
              "DISPATCHED",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.DISPATCHED,
              "IN_PROGRESS",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.IN_PROGRESS,
              "COMPLETED",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.COMPLETED,
              "CANCELLED",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.CANCELLED,
              DISPATCH_MAP_ROUTE_STATUS_COLORS.DRAFT,
            ],
            "line-width": 4.5,
            "line-opacity": 0.82,
          },
          layout: {
            "line-join": "round",
            "line-cap": "round",
          },
        });

        map.addLayer({
          id: ROUTES_SELECTED_LAYER_ID,
          type: "line",
          source: ROUTES_SOURCE_ID,
          filter: ["==", ["get", "routeId"], ""],
          paint: {
            "line-color": [
              "match",
              ["get", "status"],
              "DRAFT",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.DRAFT,
              "DISPATCHED",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.DISPATCHED,
              "IN_PROGRESS",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.IN_PROGRESS,
              "COMPLETED",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.COMPLETED,
              "CANCELLED",
              DISPATCH_MAP_ROUTE_STATUS_COLORS.CANCELLED,
              DISPATCH_MAP_ROUTE_STATUS_COLORS.DRAFT,
            ],
            "line-width": 8,
            "line-opacity": 1,
          },
          layout: {
            "line-join": "round",
            "line-cap": "round",
          },
        });

        for (const layerId of [ROUTES_LAYER_ID, ROUTES_SELECTED_LAYER_ID]) {
          map.on("click", layerId, handleRouteClick);
          map.on("mouseenter", layerId, setPointerCursor);
          map.on("mouseleave", layerId, clearPointerCursor);
        }

        setMapLoaded(true);
      });

      mapRef.current = map;
    }

    void initializeMap();

    return () => {
      isCancelled = true;
      markersRef.current.forEach((marker) => marker.remove());
      markersRef.current = [];
      routePopupRef.current?.remove();
      routePopupRef.current = null;
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

    const source = map.getSource(ROUTES_SOURCE_ID) as mapboxgl.GeoJSONSource | undefined;
    source?.setData(buildRouteLineCollection(routes));
  }, [mapLoaded, routes]);

  useEffect(() => {
    const map = mapRef.current;
    if (!mapLoaded || !map) {
      return;
    }

    map.setFilter(
      ROUTES_SELECTED_LAYER_ID,
      ["==", ["get", "routeId"], selectedRouteId ?? ""],
    );
  }, [mapLoaded, selectedRouteId]);

  useEffect(() => {
    const map = mapRef.current;
    const mapbox = mapboxModuleRef.current;
    if (!mapLoaded || !map || !mapbox) {
      return;
    }

    markersRef.current.forEach((marker) => marker.remove());
    markersRef.current = [];

    const markers: mapboxgl.Marker[] = [];

    for (const route of routes) {
      if (route.depotLongitude != null && route.depotLatitude != null) {
        const element = document.createElement("button");
        element.type = "button";
        element.className = cn(
          "flex size-9 items-center justify-center rounded-full border-2 border-white bg-emerald-600 text-xs font-black text-white shadow-lg",
          route.id === selectedRouteId ? "ring-2 ring-slate-950/15" : "",
        );
        element.textContent = "D";
        element.setAttribute("aria-label", `${route.depotName ?? "Depot"} for ${route.vehiclePlate}`);
        element.addEventListener("click", () => onSelectRouteRef.current?.(route.id));

        const depotMarker = new mapbox.Marker({
          element,
          anchor: "center",
        })
          .setLngLat([route.depotLongitude, route.depotLatitude])
          .setPopup(
            new mapbox.Popup({
              closeButton: false,
              offset: 18,
            }).setHTML(buildDepotPopupHtml(route)),
          )
          .addTo(map);

        markers.push(depotMarker);
      }

      for (const stop of route.stops) {
        const element = document.createElement("button");
        element.type = "button";
        element.className = cn(
          "flex size-8 items-center justify-center rounded-full border-2 border-white text-[11px] font-bold text-white shadow-lg",
          route.id === selectedRouteId ? "ring-2 ring-slate-950/15" : "",
        );
        element.style.backgroundColor = DISPATCH_MAP_STOP_STATUS_COLORS[stop.uiStatus];
        element.textContent = String(stop.sequence);
        element.setAttribute("aria-label", `${route.vehiclePlate} stop ${stop.sequence}`);
        element.addEventListener("click", () => onSelectRouteRef.current?.(route.id));

        const marker = new mapbox.Marker({
          element,
          anchor: "center",
        })
          .setLngLat([stop.longitude, stop.latitude])
          .setPopup(
            new mapbox.Popup({
              closeButton: false,
              offset: 18,
            }).setHTML(buildStopPopupHtml(route, stop)),
          )
          .addTo(map);

        markers.push(marker);
      }
    }

    markersRef.current = markers;

    return () => {
      markersRef.current.forEach((marker) => marker.remove());
      markersRef.current = [];
    };
  }, [mapLoaded, routes, selectedRouteId]);

  useEffect(() => {
    const map = mapRef.current;
    const mapbox = mapboxModuleRef.current;
    if (!mapLoaded || !map || !mapbox || !hasAnyGeometry) {
      return;
    }

    const targetRoutes =
      selectedRouteId != null
        ? routes.filter((route) => route.id === selectedRouteId && route.hasGeometry)
        : routes.filter((route) => route.hasGeometry);

    if (targetRoutes.length === 0) {
      return;
    }

    const bounds = new mapbox.LngLatBounds();
    for (const route of targetRoutes) {
      extendBoundsWithRoute(bounds, route);
    }

    if (bounds.isEmpty()) {
      return;
    }

    map.fitBounds(bounds, {
      padding: 64,
      duration: 600,
      maxZoom: selectedRouteId ? 14 : 12.5,
    });
  }, [hasAnyGeometry, mapLoaded, routes, selectedRouteId]);

  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-[1.5rem] border border-border/60 bg-slate-100 shadow-[0_24px_64px_-36px_rgba(15,23,42,0.35)]",
        className,
      )}
    >
      <div ref={containerRef} className="h-[34rem] w-full lg:h-[42rem]" />

      {mapLoaded && hasAnyGeometry ? (
        <div className="pointer-events-none absolute left-4 top-4 z-10 flex flex-wrap gap-2">
          {Object.entries(ROUTE_STATUS_LABELS).map(([status, label]) => (
            <div
              key={status}
              className="flex items-center gap-2 rounded-full border border-white/70 bg-white/90 px-3 py-1.5 text-xs font-medium text-slate-700 shadow-lg backdrop-blur-sm"
            >
              <span
                className="block h-2 w-8 rounded-full"
                style={{
                  backgroundColor:
                    DISPATCH_MAP_ROUTE_STATUS_COLORS[status as keyof typeof DISPATCH_MAP_ROUTE_STATUS_COLORS],
                }}
              />
              {label}
            </div>
          ))}
        </div>
      ) : null}

      {!mapLoaded ? (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-slate-200/70 text-sm font-medium text-slate-700 backdrop-blur-sm">
          Loading dispatch map...
        </div>
      ) : null}

      {mapLoaded && !hasAnyRoutes ? (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-slate-950/10 px-6 text-center text-sm font-medium text-slate-700">
          No routes are scheduled for this date.
        </div>
      ) : null}

      {mapLoaded && hasAnyRoutes && !hasAnyGeometry ? (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-slate-950/10 px-6 text-center text-sm font-medium text-slate-700">
          Routes exist for this date, but none have map geometry yet.
        </div>
      ) : null}
    </div>
  );
}

export default DispatchMapCanvas;
