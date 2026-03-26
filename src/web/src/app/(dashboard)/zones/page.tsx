"use client";

import { useState } from "react";
import { Map as MapIcon, Pencil, Plus, Trash2, X } from "lucide-react";
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
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { QueryErrorAlert } from "@/components/ui/query-error-alert";
import { getErrorMessage } from "@/lib/error-message";
import {
  useCreateZone,
  useDeleteZone,
  useUpdateZone,
  useZones,
} from "@/lib/hooks/useZones";
import { useDepots } from "@/lib/hooks/useDepots";
import { cn } from "@/lib/utils";
import type { CreateZoneRequest, UpdateZoneRequest, Zone } from "@/types/zone";

type BoundaryMode = "wkt" | "geojson" | "coordinates";

interface ZoneFormData {
  name: string;
  depotId: string;
  isActive: boolean;
  boundaryMode: BoundaryMode;
  boundaryWkt: string;
  geoJson: string;
  coordinates: string;
}

function defaultForm(): ZoneFormData {
  return {
    name: "",
    depotId: "",
    isActive: true,
    boundaryMode: "wkt",
    boundaryWkt: "",
    geoJson: "",
    coordinates: "",
  };
}

function ZoneForm({
  initial,
  depots,
  onSubmit,
  onCancel,
  error,
  isPending,
}: {
  initial?: Zone;
  depots: { id: string; name: string }[];
  onSubmit: (data: CreateZoneRequest | UpdateZoneRequest) => void;
  onCancel: () => void;
  error?: string;
  isPending: boolean;
}) {
  const [form, setForm] = useState<ZoneFormData>(() => {
    if (initial) {
      return {
        name: initial.name,
        depotId: initial.depotId,
        isActive: initial.isActive,
        boundaryMode: "wkt",
        boundaryWkt: initial.boundary,
        geoJson: "",
        coordinates: "",
      };
    }

    return defaultForm();
  });

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const boundaryInput = {
      ...(form.boundaryMode === "wkt" && form.boundaryWkt.trim()
        ? { boundaryWkt: form.boundaryWkt.trim() }
        : {}),
      ...(form.boundaryMode === "geojson" && form.geoJson.trim()
        ? { geoJson: form.geoJson.trim() }
        : {}),
      ...(form.boundaryMode === "coordinates" && form.coordinates.trim()
        ? {
            coordinates: form.coordinates
              .trim()
              .split(/\s*;\s*/)
              .map((point) => {
                const [lon, lat] = point.trim().split(/\s*,\s*/);
                return [parseFloat(lon), parseFloat(lat)] as [number, number];
              }),
          }
        : {}),
    };

    onSubmit({
      name: form.name,
      depotId: form.depotId,
      isActive: form.isActive,
      boundaryInput,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <Label htmlFor="zone-name">Zone name</Label>
          <Input
            id="zone-name"
            value={form.name}
            onChange={(event) => setForm({ ...form, name: event.target.value })}
            required
          />
        </div>
        <div>
          <Label htmlFor="zone-depot">Depot</Label>
          <select
            id="zone-depot"
            value={form.depotId}
            onChange={(event) =>
              setForm({ ...form, depotId: event.target.value })
            }
            required
            className="flex h-10 w-full items-center rounded-xl border border-input/90 bg-background px-3 py-2 text-sm"
          >
            <option value="">Select depot...</option>
            {depots.map((depot) => (
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
              setForm({ ...form, isActive: event.target.value === "true" })
            }
            className="flex h-10 w-full items-center rounded-xl border border-input/90 bg-background px-3 py-2 text-sm"
          >
            <option value="true">Active</option>
            <option value="false">Inactive</option>
          </select>
        </div>
      </div>

      <div>
        <p className="mb-2 text-sm font-medium">Zone boundary</p>
        <div className="mb-3 flex flex-wrap gap-4">
          {(["wkt", "geojson", "coordinates"] as BoundaryMode[]).map((mode) => (
            <label
              key={mode}
              className="flex cursor-pointer items-center gap-1.5 text-sm"
            >
              <input
                type="radio"
                name="boundary-mode"
                checked={form.boundaryMode === mode}
                onChange={() => setForm({ ...form, boundaryMode: mode })}
                className="accent-primary"
              />
              {mode === "wkt"
                ? "WKT"
                : mode === "geojson"
                  ? "GeoJSON"
                  : "Coordinates"}
            </label>
          ))}
        </div>
        {form.boundaryMode === "wkt" ? (
          <Input
            placeholder="POLYGON ((lon lat, lon lat, ...))"
            value={form.boundaryWkt}
            onChange={(event) =>
              setForm({ ...form, boundaryWkt: event.target.value })
            }
            className="font-mono text-xs"
          />
        ) : null}
        {form.boundaryMode === "geojson" ? (
          <textarea
            placeholder='{"type":"Polygon","coordinates":[[[lon,lat],...]]}'
            value={form.geoJson}
            onChange={(event) => setForm({ ...form, geoJson: event.target.value })}
            rows={4}
            className="w-full rounded-xl border border-input/90 bg-background px-3 py-2 text-xs font-mono"
          />
        ) : null}
        {form.boundaryMode === "coordinates" ? (
          <Input
            placeholder="145.0, -37.8; 145.1, -37.8; 145.1, -37.7; 145.0, -37.7"
            value={form.coordinates}
            onChange={(event) =>
              setForm({ ...form, coordinates: event.target.value })
            }
            className="font-mono text-xs"
          />
        ) : null}
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      <div className="flex justify-end gap-3 border-t border-border/50 pt-4">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending ? "Saving..." : initial ? "Save changes" : "Create zone"}
        </Button>
      </div>
    </form>
  );
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

  const [showCreate, setShowCreate] = useState(false);
  const [editTarget, setEditTarget] = useState<Zone | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Zone | null>(null);
  const [submitError, setSubmitError] = useState<string | undefined>();

  const isLoading = zonesLoading || depotsLoading;
  const totalCount = zones?.length ?? 0;
  const depotMap = new Map((depots ?? []).map((depot) => [depot.id, depot.name]));
  const queryError = zonesError ?? depotsError;

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
                onClick={async () => {
                  try {
                    await deleteZone.mutateAsync(deleteTarget.id);
                    setDeleteTarget(null);
                    setSubmitError(undefined);
                  } catch (error) {
                    setSubmitError(getErrorMessage(error));
                  }
                }}
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

      {editTarget ? (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4">
          <div className="w-full max-w-2xl rounded-2xl border border-border bg-card p-6 shadow-xl">
            <div className="mb-6 flex items-center justify-between">
              <h2 className="text-lg font-semibold">Edit zone</h2>
              <button
                type="button"
                onClick={() => {
                  setEditTarget(null);
                  setSubmitError(undefined);
                }}
                className="rounded-lg p-1 transition-colors hover:bg-muted"
              >
                <X className="size-5" />
              </button>
            </div>
            <ZoneForm
              initial={editTarget}
              depots={depots ?? []}
              onSubmit={async (data) => {
                try {
                  await updateZone.mutateAsync({
                    id: editTarget.id,
                    data: data as UpdateZoneRequest,
                  });
                  setEditTarget(null);
                  setSubmitError(undefined);
                } catch (error) {
                  setSubmitError(getErrorMessage(error));
                }
              }}
              onCancel={() => {
                setEditTarget(null);
                setSubmitError(undefined);
              }}
              error={submitError}
              isPending={updateZone.isPending}
            />
          </div>
        </div>
      ) : null}

      <ListPageHeader
        variant="route"
        eyebrow="Coverage"
        title="Zones"
        description="Define delivery boundaries and attach them to depots for planning and assignment flows."
        icon={<MapIcon strokeWidth={1.75} aria-hidden />}
        action={
          <Button
            onClick={() => setShowCreate(true)}
            className="gap-2"
            disabled={!depots || depots.length === 0}
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
        filterCardHint="No additional filters on this page"
        activeFilterDisplay="All zones"
      />

      {showCreate ? (
        <div className="mb-8 rounded-2xl border border-border/50 bg-card/80 p-6 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)] dark:bg-card/60">
          <h2 className="mb-4 text-base font-semibold">New zone</h2>
          <ZoneForm
            depots={depots ?? []}
            onSubmit={async (data) => {
              try {
                await createZone.mutateAsync(data as CreateZoneRequest);
                setShowCreate(false);
                setSubmitError(undefined);
              } catch (error) {
                setSubmitError(getErrorMessage(error));
              }
            }}
            onCancel={() => {
              setShowCreate(false);
              setSubmitError(undefined);
            }}
            error={submitError}
            isPending={createZone.isPending}
          />
        </div>
      ) : null}

      {submitError && !showCreate && !editTarget && !deleteTarget ? (
        <p className="mb-4 text-sm text-destructive">{submitError}</p>
      ) : null}

      {zones && zones.length > 0 ? (
        <ListDataTable minWidthClassName="min-w-[920px]">
          <thead>
            <tr className={listDataTableHeadRowClass}>
              <th className={listDataTableThClass}>Zone</th>
              <th className={listDataTableThClass}>Depot</th>
              <th className={cn(listDataTableThClass, "min-w-[280px]")}>
                Boundary
              </th>
              <th className={listDataTableThClass}>Status</th>
              <th className={listDataTableThRightClass}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {zones.map((zone) => (
              <tr key={zone.id} className={listDataTableBodyRowClass}>
                <td className={cn(listDataTableTdClass, "font-medium")}>{zone.name}</td>
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
                  {zone.boundary || "-"}
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
                      onClick={() => setEditTarget(zone)}
                    >
                      <Pencil className="size-3.5" aria-hidden />
                      Edit
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="border-destructive/25 text-destructive hover:bg-destructive/10 hover:text-destructive"
                      onClick={() => setDeleteTarget(zone)}
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
            Use the Add zone action to create your first delivery zone.
          </p>
        </div>
      )}
    </>
  );
}
