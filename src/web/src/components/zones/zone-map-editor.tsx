"use client";

import { useEffect, useRef, useState } from "react";
import { SearchBox } from "@mapbox/search-js-react";
import { Globe2, LocateFixed, Target } from "lucide-react";
import type { FeatureCollection, Polygon } from "geojson";
import type mapboxgl from "mapbox-gl";
import type MapboxDraw from "@mapbox/mapbox-gl-draw";
import { getZoneMapStyle } from "@/lib/mapbox/config";
import { clonePolygonGeometry } from "@/lib/zones/geometry-history";
import {
  buildDepotMarkerData,
  buildViewportBounds,
  buildZoneOverlayData,
} from "@/lib/zones/map-data";
import type { Depot, DepotGeoLocation } from "@/types/depots";
import type { Zone } from "@/types/zones";

const INITIAL_CENTER: [number, number] = [0, 18];
const INITIAL_ZOOM = 1.5;
const OVERLAY_SOURCE_ID = "zones-overlay";
const OVERLAY_FILL_LAYER_ID = "zones-overlay-fill";
const OVERLAY_LINE_LAYER_ID = "zones-overlay-line";

type MapboxModule = typeof import("mapbox-gl")["default"];

export function ZoneMapEditor({
  accessToken,
  activeDepotId,
  depots,
  draftGeometry,
  editableZoneId,
  fallbackDepotLocation,
  mode,
  onDraftGeometryChange,
  onSelectZone,
  selectedZoneId,
  zones,
}: {
  accessToken: string;
  activeDepotId: string | null;
  depots: Depot[];
  draftGeometry: Polygon | null;
  editableZoneId: string | null;
  fallbackDepotLocation: DepotGeoLocation | null;
  mode: "idle" | "create" | "edit";
  onDraftGeometryChange: (geometry: Polygon | null) => void;
  onSelectZone: (zoneId: string) => void;
  selectedZoneId: string | null;
  zones: Zone[];
}) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<mapboxgl.Map | null>(null);
  const mapboxModuleRef = useRef<MapboxModule | null>(null);
  const drawRef = useRef<MapboxDraw | null>(null);
  const drawFeatureIdRef = useRef<string | null>(null);
  const syncingDrawRef = useRef(false);
  const lastViewportKeyRef = useRef<string | null>(null);
  const modeRef = useRef(mode);
  const onDraftGeometryChangeRef = useRef(onDraftGeometryChange);
  const onSelectZoneRef = useRef(onSelectZone);
  const overlayPolygonsRef = useRef<FeatureCollection<Polygon>>({
    type: "FeatureCollection",
    features: [],
  });
  const zoneLabelMarkersRef = useRef<mapboxgl.Marker[]>([]);
  const depotMarkersRef = useRef<mapboxgl.Marker[]>([]);
  const geolocateControlRef = useRef<mapboxgl.GeolocateControl | null>(null);
  const [mapLoaded, setMapLoaded] = useState(false);
  const [hoveredZoneId, setHoveredZoneId] = useState<string | null>(null);
  const [searchValue, setSearchValue] = useState("");
  const zoneMapStyle = getZoneMapStyle(accessToken);
  const usesMapboxStandard = typeof zoneMapStyle === "string";

  const overlayData = buildZoneOverlayData(zones, {
    editableZoneId,
    hoveredZoneId,
    selectedZoneId,
  });
  const depotMarkers = buildDepotMarkerData(
    depots,
    zones,
    activeDepotId,
    fallbackDepotLocation,
  );
  const viewportBounds = buildViewportBounds(
    overlayData,
    depotMarkers,
    draftGeometry,
  );

  function focusMapToViewport() {
    const map = mapRef.current;
    if (!map || !viewportBounds) {
      return;
    }

    const [minX, minY, maxX, maxY] = viewportBounds;

    if (minX === maxX && minY === maxY) {
      map.flyTo({
        center: [minX, minY],
        zoom: 12,
        essential: true,
      });
      return;
    }

    map.fitBounds(
      [
        [minX, minY],
        [maxX, maxY],
      ],
      {
        padding: 72,
        duration: 700,
        maxZoom: 13,
      },
    );
  }

  function focusMapToOverview() {
    const map = mapRef.current;
    if (!map) {
      return;
    }

    if (!viewportBounds) {
      map.flyTo({
        center: INITIAL_CENTER,
        zoom: INITIAL_ZOOM,
        essential: true,
      });
      return;
    }

    const [minX, minY, maxX, maxY] = viewportBounds;

    if (minX === maxX && minY === maxY) {
      map.flyTo({
        center: [minX, minY],
        zoom: 4,
        essential: true,
      });
      return;
    }

    map.fitBounds(
      [
        [minX, minY],
        [maxX, maxY],
      ],
      {
        padding: 120,
        duration: 700,
        maxZoom: 5,
      },
    );
  }

  function locateUser() {
    geolocateControlRef.current?.trigger();
  }

  useEffect(() => {
    modeRef.current = mode;
    onDraftGeometryChangeRef.current = onDraftGeometryChange;
    onSelectZoneRef.current = onSelectZone;
    overlayPolygonsRef.current = overlayData.polygons;
  }, [mode, onDraftGeometryChange, onSelectZone, overlayData.polygons]);

  useEffect(() => {
    let isCancelled = false;

    async function initializeMap() {
      if (!containerRef.current || mapRef.current) {
        return;
      }

      const mapboxModule = await import("mapbox-gl");
      const drawModule = await import("@mapbox/mapbox-gl-draw");
      if (isCancelled || !containerRef.current) {
        return;
      }

      const mapbox = mapboxModule.default;
      mapboxModuleRef.current = mapbox;
      mapbox.accessToken = accessToken;

      const map = new mapbox.Map({
        container: containerRef.current,
        style: zoneMapStyle,
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
      map.addControl(new mapbox.FullscreenControl(), "top-right");
      const geolocateControl = new mapbox.GeolocateControl({
        positionOptions: {
          enableHighAccuracy: true,
        },
        trackUserLocation: true,
        showUserHeading: true,
        fitBoundsOptions: {
          maxZoom: 13,
        },
      });
      geolocateControlRef.current = geolocateControl;
      map.addControl(geolocateControl, "top-right");
      map.addControl(
        new mapbox.ScaleControl({
          maxWidth: 140,
          unit: "metric",
        }),
        "bottom-right",
      );

      const draw = new drawModule.default({
        displayControlsDefault: false,
        controls: {
          polygon: true,
          trash: true,
        },
      });

      map.addControl(draw, "top-left");

      map.on("load", () => {
        if (isCancelled) {
          return;
        }

        if (usesMapboxStandard) {
          try {
            map.setConfigProperty("basemap", "showPlaceLabels", true);
            map.setConfigProperty("basemap", "showRoadLabels", true);
            map.setConfigProperty("basemap", "showAdminBoundaries", true);
            map.setConfigProperty("basemap", "showPointOfInterestLabels", false);
            map.setConfigProperty("basemap", "showTransitLabels", false);
            map.setConfigProperty("basemap", "show3dObjects", false);
            map.setConfigProperty("basemap", "theme", "faded");
            map.setConfigProperty("basemap", "lightPreset", "day");
          } catch {
            // Keep the map usable if Standard config is unavailable for any reason.
          }
        }

        map.addSource(OVERLAY_SOURCE_ID, {
          type: "geojson",
          data: overlayPolygonsRef.current,
        });

        map.addLayer({
          id: OVERLAY_FILL_LAYER_ID,
          type: "fill",
          source: OVERLAY_SOURCE_ID,
          paint: {
            "fill-color": ["get", "fillColor"],
            "fill-opacity": [
              "case",
              ["boolean", ["get", "isSelected"], false],
              0.42,
              ["boolean", ["get", "isHovered"], false],
              0.28,
              0.14,
            ],
          },
        });

        map.addLayer({
          id: OVERLAY_LINE_LAYER_ID,
          type: "line",
          source: OVERLAY_SOURCE_ID,
          paint: {
            "line-color": ["get", "lineColor"],
            "line-width": [
              "case",
              ["boolean", ["get", "isSelected"], false],
              3.5,
              ["boolean", ["get", "isHovered"], false],
              2.8,
              1.8,
            ],
          },
        });

        map.on("mouseenter", OVERLAY_FILL_LAYER_ID, () => {
          map.getCanvas().style.cursor = "pointer";
        });

        map.on("mouseleave", OVERLAY_FILL_LAYER_ID, () => {
          map.getCanvas().style.cursor = "";
          setHoveredZoneId(null);
        });

        map.on("mousemove", OVERLAY_FILL_LAYER_ID, (event) => {
          const zoneId = event.features?.[0]?.properties?.zoneId;
          setHoveredZoneId(typeof zoneId === "string" ? zoneId : null);
        });

        map.on("click", OVERLAY_FILL_LAYER_ID, (event) => {
          const zoneId = event.features?.[0]?.properties?.zoneId;
          if (typeof zoneId === "string") {
            onSelectZoneRef.current(zoneId);
          }
        });

        setMapLoaded(true);
      });

      const syncFromDraw = () => {
        if (syncingDrawRef.current) {
          return;
        }

        if (modeRef.current === "idle") {
          syncingDrawRef.current = true;
          draw.deleteAll();
          syncingDrawRef.current = false;
          return;
        }

        const polygonFeatures = draw
          .getAll()
          .features
          .filter((feature) => feature.geometry.type === "Polygon");

        if (polygonFeatures.length === 0) {
          drawFeatureIdRef.current = null;
          onDraftGeometryChangeRef.current(null);
          return;
        }

        const latestPolygon = polygonFeatures[polygonFeatures.length - 1];

        if (polygonFeatures.length > 1) {
          syncingDrawRef.current = true;
          draw.deleteAll();
          drawFeatureIdRef.current = draw.add(latestPolygon)[0] ?? null;
          syncingDrawRef.current = false;
        } else {
          drawFeatureIdRef.current = latestPolygon.id
            ? String(latestPolygon.id)
            : drawFeatureIdRef.current;
        }

        onDraftGeometryChangeRef.current(
          clonePolygonGeometry(latestPolygon.geometry as Polygon),
        );
      };

      map.on("draw.create", syncFromDraw);
      map.on("draw.update", syncFromDraw);
      map.on("draw.delete", syncFromDraw);

      mapRef.current = map;
      drawRef.current = draw;

      const windowWithMap = window as typeof window & {
        __zoneMap?: mapboxgl.Map;
        __zoneMapDraw?: MapboxDraw | null;
      };
      windowWithMap.__zoneMap = map;
      windowWithMap.__zoneMapDraw = draw;
    }

    void initializeMap();

    return () => {
      isCancelled = true;
      zoneLabelMarkersRef.current.forEach((marker) => marker.remove());
      depotMarkersRef.current.forEach((marker) => marker.remove());
      zoneLabelMarkersRef.current = [];
      depotMarkersRef.current = [];
      geolocateControlRef.current = null;
      drawRef.current = null;
      drawFeatureIdRef.current = null;
      setMapLoaded(false);
      const windowWithMap = window as typeof window & {
        __zoneMap?: mapboxgl.Map;
        __zoneMapDraw?: MapboxDraw | null;
      };
      windowWithMap.__zoneMap = undefined;
      windowWithMap.__zoneMapDraw = null;
      mapRef.current?.remove();
      mapRef.current = null;
    };
  }, [accessToken, usesMapboxStandard, zoneMapStyle]);

  useEffect(() => {
    const map = mapRef.current;
    if (!mapLoaded || !map) {
      return;
    }

    const source = map.getSource(OVERLAY_SOURCE_ID) as mapboxgl.GeoJSONSource | undefined;
    source?.setData(overlayData.polygons);
  }, [mapLoaded, overlayData.polygons]);

  useEffect(() => {
    const map = mapRef.current;
    const mapbox = mapboxModuleRef.current;
    if (!mapLoaded || !map || !mapbox) {
      return;
    }

    zoneLabelMarkersRef.current.forEach((marker) => marker.remove());
    zoneLabelMarkersRef.current = overlayData.labels.map((label) => {
      const element = document.createElement("button");
      element.type = "button";
      element.className =
        "cursor-pointer select-none rounded-full border border-white/85 bg-white/90 px-2.5 py-1 text-[11px] font-semibold shadow-sm backdrop-blur transition-transform focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/20";
      element.setAttribute("aria-label", `Select zone ${label.name}`);
      element.style.color = label.color;
      element.style.transform = label.isSelected ? "scale(1.04)" : "scale(1)";
      element.textContent = label.name;
      element.addEventListener("click", (event) => {
        event.preventDefault();
        event.stopPropagation();
        onSelectZoneRef.current(label.id);
      });
      return new mapbox.Marker({
        element,
        anchor: "center",
      })
        .setLngLat(label.coordinates)
        .addTo(map);
    });

    return () => {
      zoneLabelMarkersRef.current.forEach((marker) => marker.remove());
      zoneLabelMarkersRef.current = [];
    };
  }, [mapLoaded, overlayData.labels]);

  useEffect(() => {
    const map = mapRef.current;
    const mapbox = mapboxModuleRef.current;
    if (!mapLoaded || !map || !mapbox) {
      return;
    }

    depotMarkersRef.current.forEach((marker) => marker.remove());
    depotMarkersRef.current = depotMarkers.map((markerData) => {
      const element = document.createElement("div");
      element.className =
        "flex size-5 items-center justify-center rounded-full border-2 border-white shadow-lg";
      element.style.background = markerData.isActive ? "#111827" : "#1f6feb";
      element.style.outline = markerData.isFallback ? "2px solid #f59e0b" : "none";

      const inner = document.createElement("div");
      inner.className = "size-2 rounded-full bg-white";
      element.appendChild(inner);

      return new mapbox.Marker({
        element,
        anchor: "center",
      })
        .setLngLat(markerData.coordinates)
        .setPopup(
          new mapbox.Popup({
            closeButton: false,
            offset: 14,
          }).setHTML(
            `<div class="text-sm font-medium">${markerData.name}</div>${
              markerData.isFallback
                ? '<div class="mt-1 text-xs text-amber-600">Approximate geocoded location</div>'
                : ""
            }`,
          ),
        )
        .addTo(map);
    });

    return () => {
      depotMarkersRef.current.forEach((marker) => marker.remove());
      depotMarkersRef.current = [];
    };
  }, [depotMarkers, mapLoaded]);

  useEffect(() => {
    const draw = drawRef.current;
    if (!mapLoaded || !draw) {
      return;
    }

    syncingDrawRef.current = true;
    draw.deleteAll();

    if (draftGeometry) {
      const nextGeometry = clonePolygonGeometry(draftGeometry);
      if (!nextGeometry) {
        syncingDrawRef.current = false;
        return;
      }

      drawFeatureIdRef.current =
        draw.add({
          type: "Feature",
          properties: {},
          geometry: nextGeometry,
        })[0] ?? null;

      window.requestAnimationFrame(() => {
        if (!drawRef.current) {
          syncingDrawRef.current = false;
          return;
        }

        if (mode === "edit" && drawFeatureIdRef.current) {
          try {
            drawRef.current.changeMode("direct_select", {
              featureId: drawFeatureIdRef.current,
            });
          } catch {
            drawRef.current.changeMode("simple_select");
          }
        } else {
          drawRef.current.changeMode("simple_select");
        }

        syncingDrawRef.current = false;
      });

      return;
    }

    drawFeatureIdRef.current = null;

    if (mode === "create") {
      try {
        draw.changeMode("draw_polygon");
      } catch {
        draw.changeMode("simple_select");
      }
    } else {
      draw.changeMode("simple_select");
    }

    syncingDrawRef.current = false;
  }, [draftGeometry, mapLoaded, mode]);

  useEffect(() => {
    const map = mapRef.current;
    if (!mapLoaded || !map || !viewportBounds) {
      return;
    }

    const viewportKey = JSON.stringify({
      bounds: viewportBounds,
      editableZoneId,
      selectedZoneId,
      draftGeometry,
      activeDepotId,
    });
    if (lastViewportKeyRef.current === viewportKey) {
      return;
    }

    lastViewportKeyRef.current = viewportKey;
    const [minX, minY, maxX, maxY] = viewportBounds;

    if (minX === maxX && minY === maxY) {
      map.flyTo({
        center: [minX, minY],
        zoom: 12,
        essential: true,
      });
      return;
    }

    map.fitBounds(
      [
        [minX, minY],
        [maxX, maxY],
      ],
      {
        padding: 56,
        duration: 600,
        maxZoom: 13,
      },
    );
  }, [activeDepotId, draftGeometry, editableZoneId, mapLoaded, selectedZoneId, viewportBounds]);

  return (
    <div className="relative overflow-hidden rounded-[1.75rem] border border-border/60 bg-slate-100 shadow-[0_24px_64px_-36px_rgba(15,23,42,0.35)]">
      <div
        ref={containerRef}
        data-testid="zones-map"
        className="h-[28rem] w-full sm:h-[34rem]"
      />
      {mapLoaded && mapRef.current && mapboxModuleRef.current ? (
        <div className="pointer-events-auto absolute left-1/2 top-4 z-10 w-[min(32rem,calc(100%-7rem))] -translate-x-1/2">
          <SearchBox
            accessToken={accessToken}
            map={mapRef.current}
            mapboxgl={mapboxModuleRef.current}
            marker={false}
            value={searchValue}
            onChange={setSearchValue}
            onClear={() => setSearchValue("")}
            placeholder="Search city, address, depot area..."
            options={{
              language: "en",
            }}
            popoverOptions={{
              placement: "bottom-start",
              flip: true,
              offset: 8,
            }}
          />
        </div>
      ) : null}
      {mapLoaded ? (
        <div className="pointer-events-none absolute left-4 bottom-4 z-10 flex flex-col gap-3">
          <div className="pointer-events-auto flex flex-wrap items-center gap-2 rounded-2xl border border-white/70 bg-white/88 px-3 py-2 shadow-lg backdrop-blur">
            <button
              type="button"
              onClick={focusMapToOverview}
              className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            >
              <Globe2 className="size-3.5" aria-hidden />
              Overview
            </button>
            <button
              type="button"
              onClick={focusMapToViewport}
              className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-slate-900 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-slate-700"
            >
              <Target className="size-3.5" aria-hidden />
              Fit zones
            </button>
            <button
              type="button"
              onClick={locateUser}
              className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
            >
              <LocateFixed className="size-3.5" aria-hidden />
              Locate me
            </button>
          </div>
        </div>
      ) : null}
      {!mapLoaded ? (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-slate-200/70 text-sm font-medium text-slate-700 backdrop-blur-sm">
          Loading zone map…
        </div>
      ) : null}
    </div>
  );
}

export default ZoneMapEditor;
