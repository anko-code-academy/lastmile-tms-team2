"use client";

import dynamic from "next/dynamic";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Map as MapIcon, Pencil, Plus, Redo2, Trash2, Undo2 } from "lucide-react";
import { useSession } from "next-auth/react";
import {
  ListDataTable,
  ListPageHeader,
  ListPageLoading,
  ListPageStatsStrip,
  listDataTableBodyRowClass,
  listDataTableHeadRowClass,
  listDataTableTdClass,
  listDataTableThClass,
  listDataTableThRightClass,
} from "@/components/list";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getMapboxAccessToken, getMapboxConfigurationError } from "@/lib/mapbox/config";
import { formatDepotGeocodingQuery, geocodeDepotAddress } from "@/lib/mapbox/geocoding";
import {
  createGeometryHistory,
  pushGeometrySnapshot,
  redoGeometryHistory,
  undoGeometryHistory,
} from "@/lib/zones/geometry-history";
import { parseZonePolygon } from "@/lib/zones/map-data";
import { getErrorMessage } from "@/lib/network/error-message";
import { useDepots } from "@/queries/depots";
import {
  useCreateZone,
  useDeleteZone,
  useUpdateZone,
  useZones,
} from "@/queries/zones";
import { cn } from "@/lib/utils";
import type { Depot, DepotGeoLocation } from "@/types/depots";
import type { CreateZoneRequest, UpdateZoneRequest, Zone } from "@/types/zones";

const ZoneMapEditor = dynamic(() => import("./zone-map-editor"), {
  ssr: false,
  loading: () => (
    <div className="flex h-[28rem] items-center justify-center rounded-[1.75rem] border border-border/60 bg-slate-100 text-sm font-medium text-slate-600 sm:h-[34rem]">
      Loading zone map…
    </div>
  ),
});

type EditorMode = "idle" | "create" | "edit";

interface ZoneFormState {
  name: string;
  depotId: string;
  isActive: boolean;
}

function defaultForm(depots: Depot[] | undefined): ZoneFormState {
  return {
    name: "",
    depotId: depots?.[0]?.id ?? "",
    isActive: true,
  };
}

function zoneToForm(zone: Zone): ZoneFormState {
  return {
    name: zone.name,
    depotId: zone.depotId,
    isActive: zone.isActive,
  };
}

function zoneById(zones: Zone[] | undefined, zoneId: string | null): Zone | null {
  if (!zones || !zoneId) {
    return null;
  }

  return zones.find((zone) => zone.id === zoneId) ?? null;
}

export default function ZonesPage() {
  const { status } = useSession();
  const { data: zones, isLoading: zonesLoading, error: zonesError } = useZones();
  const {
    data: depots,
    isLoading: depotsLoading,
    error: depotsError,
  } = useDepots();
  const createZone = useCreateZone();
  const updateZone = useUpdateZone();
  const deleteZone = useDeleteZone();

  const mapboxToken = getMapboxAccessToken();
  const mapboxConfigurationError = getMapboxConfigurationError();

  const [mode, setMode] = useState<EditorMode>("idle");
  const [selectedZoneId, setSelectedZoneId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Zone | null>(null);
  const [submitError, setSubmitError] = useState<string | undefined>();
  const [form, setForm] = useState<ZoneFormState>(defaultForm(undefined));
  const [draftHistory, setDraftHistory] = useState(createGeometryHistory(null));

  const isLoading = zonesLoading || depotsLoading;
  const queryError = zonesError ?? depotsError;
  const depotMap = new Map((depots ?? []).map((depot) => [depot.id, depot.name]));
  const totalCount = zones?.length ?? 0;
  const selectedZone = zoneById(zones, selectedZoneId);
  const activeDepot = depots?.find((depot) => depot.id === form.depotId) ?? null;
  const activeDepotAddress = activeDepot?.address ?? null;
  const shouldGeocodeDepot =
    status === "authenticated"
    && mode !== "idle"
    && Boolean(activeDepotAddress)
    && !activeDepotAddress?.geoLocation
    && Boolean(mapboxToken);
  const draftGeometry = draftHistory.present;
  const isBusy =
    createZone.isPending || updateZone.isPending || deleteZone.isPending;

  const activeDepotGeocodingQuery = useQuery<DepotGeoLocation | null>({
    queryKey: [
      "zones",
      "depot-fallback-geocode",
      activeDepot?.id ?? null,
      activeDepotAddress ? formatDepotGeocodingQuery(activeDepotAddress) : null,
      mapboxToken ?? null,
    ],
    queryFn: ({ signal }) =>
      geocodeDepotAddress(activeDepotAddress as NonNullable<typeof activeDepotAddress>, mapboxToken as string, signal),
    enabled: shouldGeocodeDepot,
    staleTime: 1000 * 60 * 15,
  });
  const fallbackDepotLocation = shouldGeocodeDepot
    ? activeDepotGeocodingQuery.data ?? null
    : null;
  const isGeocodingDepot = shouldGeocodeDepot && activeDepotGeocodingQuery.isPending;

  if (status === "loading" || isLoading) {
    return <ListPageLoading />;
  }

  if (queryError) {
    return (
      <QueryErrorAlert
        title="Could not load zones"
        message={getErrorMessage(queryError)}
      />
    );
  }

  function resetEditor(nextMode: EditorMode = "idle") {
    setMode(nextMode);
    setSelectedZoneId(null);
    setSubmitError(undefined);
    setDraftHistory(createGeometryHistory(null));
    setForm(defaultForm(depots));
  }

  function startCreateFlow() {
    setMode("create");
    setSelectedZoneId(null);
    setSubmitError(undefined);
    setDraftHistory(createGeometryHistory(null));
    setForm(defaultForm(depots));
  }

  function startEditFlow(zone: Zone) {
    setMode("edit");
    setSelectedZoneId(zone.id);
    setSubmitError(undefined);
    setForm(zoneToForm(zone));
    setDraftHistory(createGeometryHistory(parseZonePolygon(zone.boundaryGeoJson)));
  }

  async function saveZone() {
    if (!draftGeometry) {
      setSubmitError("Draw a polygon boundary on the map before saving.");
      return;
    }

    if (!form.name.trim()) {
      setSubmitError("Zone name is required.");
      return;
    }

    if (!form.depotId) {
      setSubmitError("Choose a depot before saving.");
      return;
    }

    const request: CreateZoneRequest | UpdateZoneRequest = {
      name: form.name.trim(),
      depotId: form.depotId,
      isActive: form.isActive,
      boundaryGeoJson: JSON.stringify(draftGeometry),
    };

    try {
      if (mode === "create") {
        await createZone.mutateAsync(request as CreateZoneRequest);
      } else if (mode === "edit" && selectedZoneId) {
        await updateZone.mutateAsync({
          id: selectedZoneId,
          data: request as UpdateZoneRequest,
        });
      }

      resetEditor();
    } catch (error) {
      setSubmitError(getErrorMessage(error));
    }
  }

  async function confirmDeleteZone() {
    if (!deleteTarget) {
      return;
    }

    try {
      await deleteZone.mutateAsync(deleteTarget.id);
      if (deleteTarget.id === selectedZoneId) {
        resetEditor();
      }
      setDeleteTarget(null);
      setSubmitError(undefined);
    } catch (error) {
      setSubmitError(getErrorMessage(error));
    }
  }

  const canUndo = draftHistory.past.length > 0;
  const canRedo = draftHistory.future.length > 0;
  const canSave = Boolean(
    mode !== "idle" && form.name.trim() && form.depotId && draftGeometry,
  );

  return (
    <>
      {deleteTarget ? (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4">
          <div className="mt-[15vh] w-full max-w-sm rounded-2xl border border-border bg-card p-6 shadow-xl">
            <h2 className="mb-2 text-lg font-semibold">Delete zone</h2>
            <p className="mb-6 text-sm text-muted-foreground">
              Are you sure you want to delete <strong>{deleteTarget.name}</strong>?
              This action cannot be undone.
            </p>
            <div className="flex justify-end gap-3">
              <Button
                variant="outline"
                onClick={() => {
                  setDeleteTarget(null);
                  setSubmitError(undefined);
                }}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                disabled={deleteZone.isPending}
                onClick={() => void confirmDeleteZone()}
              >
                {deleteZone.isPending ? "Deleting..." : "Delete"}
              </Button>
            </div>
            {submitError ? (
              <p className="mt-3 text-sm text-destructive">{submitError}</p>
            ) : null}
          </div>
        </div>
      ) : null}

      <ListPageHeader
        variant="route"
        eyebrow="Coverage"
        title="Zones"
        description="Draw, review, and maintain delivery boundaries directly on the map while keeping each zone linked to a depot."
        icon={<MapIcon strokeWidth={1.75} aria-hidden />}
        action={
          <Button
            onClick={startCreateFlow}
            className="gap-2"
            disabled={!depots?.length || Boolean(mapboxConfigurationError)}
          >
            <Plus className="size-4" aria-hidden />
            Add zone
          </Button>
        }
      />

      <ListPageStatsStrip
        totalLabel="Total zones"
        totalCount={totalCount}
        rangeEntityLabel="zones"
        from={totalCount === 0 ? 0 : 1}
        to={totalCount}
        page={1}
        totalPages={1}
        pageSize={Math.max(totalCount, 1)}
        filterCardLabel="View"
        filterCardHint="Hover or select a polygon to inspect it faster"
        activeFilterDisplay="All zones"
      />

      {mapboxConfigurationError ? (
        <div className="mb-8 rounded-2xl border border-amber-200 bg-amber-50 px-5 py-4 text-sm text-amber-900">
          <p className="font-semibold">Mapbox is not configured</p>
          <p className="mt-1">
            Set <code>NEXT_PUBLIC_MAPBOX_ACCESS_TOKEN</code> to enable polygon drawing and depot geocoding on this page.
          </p>
        </div>
      ) : (
        <div className="mb-8 grid gap-6 xl:grid-cols-[minmax(22rem,24rem)_minmax(0,1fr)]">
          <div className="rounded-[1.75rem] border border-border/60 bg-card/95 p-5 shadow-[0_16px_48px_-28px_rgba(15,23,42,0.35)]">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-muted-foreground">
                  {mode === "create"
                    ? "Create Zone"
                    : mode === "edit"
                      ? "Edit Zone"
                      : "Map Controls"}
                </p>
                <h2 className="mt-2 text-xl font-semibold">
                  {mode === "create"
                    ? "Draw a new delivery boundary"
                    : mode === "edit"
                      ? `Editing ${selectedZone?.name ?? "selected zone"}`
                      : "Select a zone or start drawing"}
                </h2>
              </div>
            </div>

            <div className="mt-5 grid gap-3 sm:grid-cols-2">
              <Button
                type="button"
                variant="outline"
                className="justify-start gap-2"
                disabled={!canUndo || isBusy}
                onClick={() => setDraftHistory((current) => undoGeometryHistory(current))}
              >
                <Undo2 className="size-4" aria-hidden />
                Undo
              </Button>
              <Button
                type="button"
                variant="outline"
                className="justify-start gap-2"
                disabled={!canRedo || isBusy}
                onClick={() => setDraftHistory((current) => redoGeometryHistory(current))}
              >
                <Redo2 className="size-4" aria-hidden />
                Redo
              </Button>
              <Button
                type="button"
                variant="outline"
                className="justify-start gap-2"
                disabled={mode === "idle" || isBusy}
                onClick={() =>
                  setDraftHistory((current) => pushGeometrySnapshot(current, null))
                }
              >
                <Trash2 className="size-4" aria-hidden />
                Clear draft
              </Button>
              <Button
                type="button"
                variant="outline"
                className="justify-start gap-2 border-destructive/25 text-destructive hover:bg-destructive/10 hover:text-destructive"
                disabled={!selectedZone || isBusy}
                onClick={() => setDeleteTarget(selectedZone)}
              >
                <Trash2 className="size-4" aria-hidden />
                Delete zone
              </Button>
            </div>

            <div className="mt-5 rounded-2xl border border-border/60 bg-muted/35 p-4 text-sm">
              <p className="font-medium text-foreground">Boundary status</p>
              <p className="mt-1 text-muted-foreground">
                {draftGeometry
                  ? "Polygon ready. Move vertices directly on the map or draw again to replace it."
                  : mode === "create"
                    ? "Use the map toolbar to start polygon mode and sketch a new zone."
                    : mode === "edit"
                      ? "The selected zone boundary is loaded into edit mode on the map."
                      : "Choose an existing polygon from the map or table to start editing."}
              </p>
              {activeDepot?.address?.geoLocation ? (
                <p className="mt-3 text-xs text-emerald-700">
                  Depot marker is using stored backend coordinates.
                </p>
              ) : fallbackDepotLocation ? (
                <p className="mt-3 text-xs text-amber-700">
                  Depot marker is using a temporary geocoded fallback for the selected depot.
                </p>
              ) : isGeocodingDepot ? (
                <p className="mt-3 text-xs text-muted-foreground">
                  Looking up a fallback marker for the selected depot…
                </p>
              ) : null}
            </div>

            <div className="mt-6 space-y-4">
              <div>
                <Label htmlFor="zone-name">Zone name</Label>
                <Input
                  id="zone-name"
                  value={form.name}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, name: event.target.value }))
                  }
                  placeholder="North metro"
                  disabled={mode === "idle"}
                />
              </div>

              <div>
                <Label htmlFor="zone-depot">Depot</Label>
                <select
                  id="zone-depot"
                  value={form.depotId}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, depotId: event.target.value }))
                  }
                  disabled={mode === "idle"}
                  className="flex h-10 w-full items-center rounded-xl border border-input/90 bg-background px-3 py-2 text-sm"
                >
                  <option value="">Select depot...</option>
                  {(depots ?? []).map((depot) => (
                    <option key={depot.id} value={depot.id}>
                      {depot.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <Label htmlFor="zone-active">Status</Label>
                <select
                  id="zone-active"
                  value={form.isActive ? "true" : "false"}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      isActive: event.target.value === "true",
                    }))
                  }
                  disabled={mode === "idle"}
                  className="flex h-10 w-full items-center rounded-xl border border-input/90 bg-background px-3 py-2 text-sm"
                >
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
            </div>

            {submitError ? (
              <p className="mt-4 text-sm text-destructive">{submitError}</p>
            ) : null}

            <div className="mt-6 flex flex-wrap justify-end gap-3 border-t border-border/60 pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => resetEditor()}
                disabled={mode === "idle" || isBusy}
              >
                Cancel
              </Button>
              <Button
                type="button"
                onClick={() => void saveZone()}
                disabled={!canSave || isBusy}
                className="gap-2"
              >
                <Pencil className="size-4" aria-hidden />
                {isBusy
                  ? "Saving..."
                  : mode === "create"
                    ? "Create zone"
                    : "Save changes"}
              </Button>
            </div>
          </div>

          <ZoneMapEditor
            accessToken={mapboxToken ?? ""}
            activeDepotId={form.depotId || null}
            depots={depots ?? []}
            draftGeometry={draftGeometry}
            editableZoneId={mode === "edit" ? selectedZoneId : null}
            fallbackDepotLocation={fallbackDepotLocation}
            mode={mode}
            onDraftGeometryChange={(geometry) =>
              setDraftHistory((current) => pushGeometrySnapshot(current, geometry))
            }
            onSelectZone={(zoneId) => {
              const zone = zoneById(zones, zoneId);
              if (zone) {
                startEditFlow(zone);
              }
            }}
            selectedZoneId={selectedZoneId}
            zones={zones ?? []}
          />
        </div>
      )}

      {zones && zones.length > 0 ? (
        <ListDataTable minWidthClassName="min-w-[980px]">
          <thead>
            <tr className={listDataTableHeadRowClass}>
              <th className={listDataTableThClass}>Zone</th>
              <th className={listDataTableThClass}>Depot</th>
              <th className={cn(listDataTableThClass, "min-w-[220px]")}>
                Boundary
              </th>
              <th className={listDataTableThClass}>Status</th>
              <th className={listDataTableThRightClass}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {zones.map((zone) => (
              <tr
                key={zone.id}
                className={cn(
                  listDataTableBodyRowClass,
                  !mapboxConfigurationError && "cursor-pointer transition-colors",
                  selectedZoneId === zone.id && "bg-primary/5",
                )}
                onClick={() => {
                  if (!mapboxConfigurationError) {
                    startEditFlow(zone);
                  }
                }}
              >
                <td className={cn(listDataTableTdClass, "font-medium")}>
                  {zone.name}
                </td>
                <td className={cn(listDataTableTdClass, "text-muted-foreground")}>
                  {depotMap.get(zone.depotId) ?? zone.depotName ?? "-"}
                </td>
                <td
                  className={cn(
                    listDataTableTdClass,
                    "max-w-[320px] truncate font-mono text-xs text-muted-foreground",
                  )}
                  title={zone.boundary}
                >
                  {zone.boundary || "GeoJSON polygon available"}
                </td>
                <td className={listDataTableTdClass}>
                  <span
                    className={cn(
                      "inline-flex rounded-full px-2.5 py-1 text-xs font-medium",
                      zone.isActive
                        ? "bg-emerald-100 text-emerald-700"
                        : "bg-amber-100 text-amber-700",
                    )}
                  >
                    {zone.isActive ? "Active" : "Inactive"}
                  </span>
                </td>
                <td className={cn(listDataTableTdClass, "text-right")}>
                  <div className="flex justify-end gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      disabled={Boolean(mapboxConfigurationError)}
                      onClick={(event) => {
                        event.stopPropagation();
                        startEditFlow(zone);
                      }}
                    >
                      <Pencil className="size-3.5" aria-hidden />
                      Edit
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="border-destructive/25 text-destructive hover:bg-destructive/10 hover:text-destructive"
                      onClick={(event) => {
                        event.stopPropagation();
                        setDeleteTarget(zone);
                      }}
                    >
                      <Trash2 className="size-3.5" aria-hidden />
                      Delete
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </ListDataTable>
      ) : (
        <div className="rounded-2xl border border-dashed border-border p-12 text-center">
          <p className="font-medium">No zones yet</p>
          <p className="mt-1 text-sm text-muted-foreground">
            Use the Add zone action to create your first delivery zone on the map.
          </p>
        </div>
      )}
    </>
  );
}
