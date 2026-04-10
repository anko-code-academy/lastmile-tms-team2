"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, CalendarDays, Route, Sparkles } from "lucide-react";
import {
  DetailBreadcrumb,
  DetailFormField,
  DetailFormPageShell,
  DetailPanel,
  FORM_PAGE_FORM_COLUMN_CLASS,
  FormActionsBar,
} from "@/components/detail";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { DateTimePicker } from "@/components/form/date-time-picker";
import { NaturalNumberInput } from "@/components/form/natural-number-input";
import { SelectDropdown } from "@/components/form/select-dropdown";
import { ListPageHeader } from "@/components/list";
import { Button, buttonVariants } from "@/components/ui/button";
import { driverSelectOptions } from "@/lib/forms/drivers";
import { vehicleSelectOptionsForRoute } from "@/lib/forms/vehicles";
import {
  ROUTE_ASSIGNMENT_MODE_LABELS,
  ROUTE_STATUS_LABELS,
  ROUTE_STOP_MODE_LABELS,
  STAGING_AREA_LABELS,
} from "@/lib/labels/routes";
import { getErrorMessage } from "@/lib/network/error-message";
import { cn } from "@/lib/utils";
import { routeCreateFormSchema } from "@/lib/validation/routes";
import { zodErrorToFieldMap } from "@/lib/validation/zod-field-errors";
import { useCreateRoute, useRouteAssignmentCandidates, useRoutePlanPreview } from "@/queries/routes";
import type { RoutePlanCandidateParcel } from "@/types/routes";
import { useZones } from "@/queries/zones";
import { RouteMap } from "./route-map";
import {
  type ManualStopState,
  reconcileManualStops,
  RouteManualStopEditor,
  sameManualStops,
} from "./route-manual-stop-editor";

const stagingAreaOptions = [
  { value: "A", label: STAGING_AREA_LABELS.A },
  { value: "B", label: STAGING_AREA_LABELS.B },
] as const;

const assignmentModeOptions = [
  { value: "MANUAL_PARCELS", label: ROUTE_ASSIGNMENT_MODE_LABELS.MANUAL_PARCELS },
  { value: "AUTO_BY_ZONE", label: ROUTE_ASSIGNMENT_MODE_LABELS.AUTO_BY_ZONE },
] as const;

const stopModeOptions = [
  { value: "AUTO", label: ROUTE_STOP_MODE_LABELS.AUTO },
  { value: "MANUAL", label: ROUTE_STOP_MODE_LABELS.MANUAL },
] as const;

function toServiceDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function toManualStopDrafts(stops: ManualStopState[]) {
  return stops.map((stop, index) => ({ sequence: index + 1, parcelIds: stop.parcelIds }));
}

function formatDistance(meters: number) {
  return `${(meters / 1000).toFixed(1)} km`;
}

function formatDuration(seconds: number) {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.round((seconds % 3600) / 60);
  if (hours <= 0) return `${minutes} min`;
  if (minutes <= 0) return `${hours} hr`;
  return `${hours} hr ${minutes} min`;
}

export default function NewRoutePage() {
  const router = useRouter();
  const createRoute = useCreateRoute();
  const { data: zones = [], isLoading: zonesLoading } = useZones();
  const [manualStops, setManualStops] = useState<ManualStopState[]>([]);
  const [autoSelectedParcelIds, setAutoSelectedParcelIds] = useState<string[]>([]);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [formData, setFormData] = useState({
    zoneId: "",
    vehicleId: "",
    driverId: "",
    stagingArea: "" as "" | "A" | "B",
    startDate: new Date().toISOString().slice(0, 16),
    startMileage: 0,
    assignmentMode: "MANUAL_PARCELS" as "MANUAL_PARCELS" | "AUTO_BY_ZONE",
    stopMode: "AUTO" as "AUTO" | "MANUAL",
    parcelIds: [] as string[],
  });

  const activeZones = useMemo(() => zones.filter((zone) => zone.isActive), [zones]);
  const zoneOptions = useMemo(
    () => activeZones.map((zone) => ({ value: zone.id, label: zone.depotName ? `${zone.name} | ${zone.depotName}` : zone.name })),
    [activeZones],
  );
  const selectedZone = useMemo(() => activeZones.find((zone) => zone.id === formData.zoneId) ?? null, [activeZones, formData.zoneId]);
  const serviceDate = useMemo(() => toServiceDate(formData.startDate), [formData.startDate]);

  const { data: assignmentCandidates, isLoading: assignmentLoading, error: assignmentError } =
    useRouteAssignmentCandidates(serviceDate, formData.zoneId || undefined);
  const vehicles = assignmentCandidates?.vehicles ?? [];
  const drivers = assignmentCandidates?.drivers ?? [];
  const effectiveVehicleId = assignmentLoading || vehicles.some((v) => v.id === formData.vehicleId) ? formData.vehicleId : "";
  const selectedVehicle = vehicles.find((vehicle) => vehicle.id === effectiveVehicleId) ?? null;
  const availableDrivers = selectedVehicle ? drivers.filter((driver) => driver.depotId === selectedVehicle.depotId) : drivers;
  const effectiveDriverId =
    assignmentLoading || availableDrivers.some((driver) => driver.id === formData.driverId) ? formData.driverId : "";
  const selectedDriver = availableDrivers.find((driver) => driver.id === effectiveDriverId) ?? null;

  const selectedIdsForManualStops =
    formData.assignmentMode === "MANUAL_PARCELS" ? formData.parcelIds : autoSelectedParcelIds;
  const manualStopsForRequest =
    formData.stopMode === "MANUAL"
      ? toManualStopDrafts(reconcileManualStops(manualStops, selectedIdsForManualStops))
      : [];

  const previewRequest =
    serviceDate && formData.zoneId
      ? {
          zoneId: formData.zoneId,
          vehicleId: effectiveVehicleId || null,
          driverId: effectiveDriverId || null,
          startDate: serviceDate,
          assignmentMode: formData.assignmentMode,
          stopMode: formData.stopMode,
          parcelIds: formData.assignmentMode === "MANUAL_PARCELS" ? formData.parcelIds : [],
          stops: manualStopsForRequest,
        }
      : null;

  const { data: preview, isLoading: previewLoading, error: previewError } = useRoutePlanPreview(previewRequest);
  const selectedParcels = (preview?.candidateParcels ?? []).filter(
    (parcel: RoutePlanCandidateParcel) => parcel.isSelected,
  );
  const selectedParcelIds = selectedParcels.map((parcel) => parcel.id);
  const parcelsById = useMemo(() => new Map(selectedParcels.map((parcel) => [parcel.id, parcel])), [selectedParcels]);

  useEffect(() => {
    if (formData.assignmentMode === "AUTO_BY_ZONE") {
      setAutoSelectedParcelIds((current) =>
        JSON.stringify(current) === JSON.stringify(selectedParcelIds)
          ? current
          : selectedParcelIds,
      );
      return;
    }
    setAutoSelectedParcelIds((current) => (current.length === 0 ? current : []));
  }, [formData.assignmentMode, selectedParcelIds]);

  useEffect(() => {
    if (formData.stopMode !== "MANUAL") return;
    setManualStops((current) => {
      const next = reconcileManualStops(current, selectedIdsForManualStops);
      return sameManualStops(current, next) ? current : next;
    });
  }, [formData.stopMode, selectedIdsForManualStops]);

  const totalWeightKg = selectedParcels.reduce(
    (sum, parcel) => sum + (parcel.weightUnit === "LB" ? parcel.weight * 0.453592 : parcel.weight),
    0,
  );
  const parcelCapacityOk = !selectedVehicle || selectedParcels.length <= selectedVehicle.parcelCapacity;
  const weightCapacityOk = !selectedVehicle || totalWeightKg <= selectedVehicle.weightCapacity;
  const canCreateRoute =
    !!formData.zoneId && !!effectiveVehicleId && !!effectiveDriverId && !!formData.stagingArea && !!serviceDate && parcelCapacityOk && weightCapacityOk && selectedParcelIds.length > 0;

  function clearError(key: string) {
    setErrors((prev) => {
      if (!(key in prev)) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    const parsed = routeCreateFormSchema.safeParse({
      zoneId: formData.zoneId,
      vehicleId: effectiveVehicleId,
      driverId: effectiveDriverId,
      stagingArea: formData.stagingArea,
      startDate: formData.startDate,
      startMileage: formData.startMileage,
      assignmentMode: formData.assignmentMode,
      stopMode: formData.stopMode,
      parcelIds: selectedParcelIds,
      stops: manualStopsForRequest,
    });

    if (!parsed.success) {
      setErrors(zodErrorToFieldMap(parsed.error));
      return;
    }
    if (selectedParcelIds.length === 0) {
      setErrors((prev) => ({ ...prev, parcelIds: "Select at least one parcel for this route." }));
      return;
    }

    setErrors({});
    await createRoute.mutateAsync({ ...parsed.data, startDate: new Date(parsed.data.startDate).toISOString() });
    router.push("/routes");
  }

  return (
    <DetailFormPageShell variant="route">
      <DetailBreadcrumb className="form-page-breadcrumb-animate" variant="route" items={[{ label: "Routes", href: "/routes" }, { label: "New route" }]} />
      <ListPageHeader
        variant="route"
        eyebrow="Dispatch"
        title="Create route"
        description="Plan by zone, choose manual or auto parcel assignment, and preview stops on the map before saving."
        icon={<Route strokeWidth={1.75} />}
        action={<Link href="/routes" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}><ArrowLeft className="mr-2 size-4" aria-hidden />All routes</Link>}
      />

      <div className={cn(FORM_PAGE_FORM_COLUMN_CLASS, "form-page-body-animate")}>
        <form onSubmit={handleSubmit} className="space-y-6">
          <DetailPanel className="form-page-panel-animate" section="route" title="Planning" description="Service date, zone, route mode, and dispatch assignment.">
            <div className="grid gap-6 sm:grid-cols-2">
              <DetailFormField label="Service date" htmlFor="start" error={errors.startDate}>
                <DateTimePicker value={formData.startDate} invalid={!!errors.startDate} onChange={(value) => { clearError("startDate"); setFormData((prev) => ({ ...prev, startDate: value, vehicleId: "", driverId: "" })); }} />
              </DetailFormField>
              <DetailFormField label="Zone" htmlFor="zone" error={errors.zoneId}>
                <SelectDropdown id="zone" options={zoneOptions} value={formData.zoneId} invalid={!!errors.zoneId} onChange={(value) => { clearError("zoneId"); setFormData((prev) => ({ ...prev, zoneId: value, vehicleId: "", driverId: "", parcelIds: [] })); setManualStops([]); }} disabled={zonesLoading} placeholder={zonesLoading ? "Loading zones" : "Select zone"} />
                {selectedZone ? <p className="text-xs text-muted-foreground">{selectedZone.depotName ? `Depot: ${selectedZone.depotName}` : "Zone depot not available"}</p> : null}
              </DetailFormField>
              <DetailFormField label="Assignment mode" htmlFor="assignment-mode" error={errors.assignmentMode}>
                <SelectDropdown id="assignment-mode" options={[...assignmentModeOptions]} value={formData.assignmentMode} invalid={!!errors.assignmentMode} onChange={(value) => { clearError("assignmentMode"); setFormData((prev) => ({ ...prev, assignmentMode: value, parcelIds: value === "AUTO_BY_ZONE" ? [] : prev.parcelIds })); if (value === "AUTO_BY_ZONE") setManualStops([]); }} />
              </DetailFormField>
              <DetailFormField label="Stop mode" htmlFor="stop-mode" error={errors.stopMode}>
                <SelectDropdown id="stop-mode" options={[...stopModeOptions]} value={formData.stopMode} invalid={!!errors.stopMode} onChange={(value) => { clearError("stopMode"); setFormData((prev) => ({ ...prev, stopMode: value })); }} />
              </DetailFormField>
              <DetailFormField label="Vehicle" htmlFor="vehicle" error={errors.vehicleId}>
                <SelectDropdown id="vehicle" options={vehicleSelectOptionsForRoute(vehicles)} value={effectiveVehicleId} invalid={!!errors.vehicleId} onChange={(value) => { clearError("vehicleId"); setFormData((prev) => ({ ...prev, vehicleId: value, driverId: "" })); }} disabled={!serviceDate || !formData.zoneId || assignmentLoading} placeholder={!serviceDate || !formData.zoneId ? "Choose date and zone first" : assignmentLoading ? "Loading vehicles" : "Select vehicle"} />
                {selectedVehicle ? <p className="text-xs text-muted-foreground">{selectedVehicle.depotName ? `${selectedVehicle.depotName} | ` : ""}Capacity: {selectedVehicle.parcelCapacity} parcels, {selectedVehicle.weightCapacity} kg</p> : null}
              </DetailFormField>
              <DetailFormField label="Driver" htmlFor="driver" error={errors.driverId}>
                <SelectDropdown id="driver" options={driverSelectOptions(availableDrivers)} value={effectiveDriverId} invalid={!!errors.driverId} onChange={(value) => { clearError("driverId"); setFormData((prev) => ({ ...prev, driverId: value })); }} disabled={!selectedVehicle || assignmentLoading} placeholder={!selectedVehicle ? "Select vehicle first" : assignmentLoading ? "Loading drivers" : "Select driver"} />
              </DetailFormField>
              <DetailFormField label="Staging area" htmlFor="staging-area" error={errors.stagingArea}>
                <SelectDropdown id="staging-area" options={[...stagingAreaOptions]} value={formData.stagingArea} invalid={!!errors.stagingArea} onChange={(value) => { clearError("stagingArea"); setFormData((prev) => ({ ...prev, stagingArea: value })); }} placeholder="Select staging area" />
              </DetailFormField>
              <DetailFormField label="Start mileage (km)" htmlFor="mileage" error={errors.startMileage}>
                <NaturalNumberInput id="mileage" value={formData.startMileage} onChange={(value) => { clearError("startMileage"); setFormData((prev) => ({ ...prev, startMileage: value })); }} />
              </DetailFormField>
            </div>

            {selectedDriver ? (
              <div className="mt-6 rounded-2xl border border-border/60 bg-background/60 p-4">
                <div className="flex items-start gap-3">
                  <div className="rounded-xl bg-muted/70 p-2 text-muted-foreground"><CalendarDays className="size-4" aria-hidden /></div>
                  <div className="space-y-2">
                    <p className="text-sm font-semibold">Driver workload</p>
                    {selectedDriver.workloadRoutes.length === 0 ? <p className="text-sm text-muted-foreground">No other same-day routes recorded.</p> : selectedDriver.workloadRoutes.map((route) => <div key={route.routeId} className="rounded-xl border border-border/60 bg-muted/30 px-3 py-2 text-sm"><div className="font-medium">Route {route.routeId.slice(0, 8)} | {route.vehiclePlate}</div><div className="text-xs text-muted-foreground">{new Date(route.startDate).toLocaleString()} | {ROUTE_STATUS_LABELS[route.status]}</div></div>)}
                  </div>
                </div>
              </div>
            ) : null}

            {assignmentError ? <QueryErrorAlert title="Could not load assignment candidates" message={getErrorMessage(assignmentError)} /> : null}
          </DetailPanel>

          <DetailPanel className="form-page-panel-animate-delay" section="route" title="Parcels" description="Manual mode lets you choose parcels. Auto mode takes every eligible zone parcel.">
            {!previewRequest ? <p className="text-sm text-muted-foreground">Choose a service date and zone to load parcels.</p> : previewLoading ? <p className="text-sm text-muted-foreground">Loading route preview...</p> : previewError ? <QueryErrorAlert title="Could not build route preview" message={getErrorMessage(previewError)} /> : formData.assignmentMode === "AUTO_BY_ZONE" ? (
              <div className="space-y-3">
                <div className="rounded-2xl border border-emerald-500/20 bg-emerald-500/5 p-4"><div className="flex items-center gap-3"><Sparkles className="size-4 text-emerald-700" aria-hidden /><p className="text-sm">Auto-by-zone selected {selectedParcels.length} eligible parcel{selectedParcels.length === 1 ? "" : "s"} from {preview?.zoneName}.</p></div></div>
                <div className="max-h-60 space-y-2 overflow-y-auto rounded-xl border border-border/60 bg-background/50 p-3">{selectedParcels.map((parcel) => <div key={parcel.id} className="rounded-lg border border-border/40 bg-muted/20 px-3 py-2 text-sm"><p className="font-medium">{parcel.trackingNumber} | {parcel.recipientLabel}</p><p className="text-xs text-muted-foreground">{parcel.addressLine}</p></div>)}{selectedParcels.length === 0 ? <p className="text-sm text-muted-foreground">No eligible parcels were found for this zone and date.</p> : null}</div>
              </div>
            ) : (
              <div className="max-h-72 space-y-1 overflow-y-auto rounded-xl border border-border/60 bg-background/50 p-3">{preview?.candidateParcels.map((parcel) => <label key={parcel.id} className="flex cursor-pointer items-start gap-3 rounded-lg p-2 transition-colors hover:bg-muted/50"><input type="checkbox" checked={parcel.isSelected} onChange={() => { clearError("parcelIds"); setFormData((prev) => ({ ...prev, parcelIds: prev.parcelIds.includes(parcel.id) ? prev.parcelIds.filter((id) => id !== parcel.id) : [...prev.parcelIds, parcel.id] })); }} className="mt-0.5 size-4 rounded border-input" /><span className="flex-1 text-sm"><span className="font-medium">{parcel.trackingNumber} | {parcel.recipientLabel}</span><span className="mt-1 block text-xs text-muted-foreground">{parcel.addressLine} | {parcel.weight} {parcel.weightUnit === "LB" ? "Lb" : "Kg"}</span></span></label>)}</div>
            )}
            {errors.parcelIds ? <p className="mt-3 text-sm text-destructive">{errors.parcelIds}</p> : null}
            <div className="mt-4 space-y-2 text-sm">
              <p className={!parcelCapacityOk ? "font-medium text-destructive" : "text-foreground"}>Selected: {selectedParcels.length}{selectedVehicle ? ` / ${selectedVehicle.parcelCapacity} max` : ""}</p>
              <p className={!weightCapacityOk ? "font-medium text-destructive" : "text-foreground"}>Total weight: {totalWeightKg.toFixed(2)} kg{selectedVehicle ? ` / ${selectedVehicle.weightCapacity} kg max` : ""}</p>
            </div>
          </DetailPanel>

          {formData.stopMode === "MANUAL" && selectedParcels.length > 0 ? (
            <DetailPanel className="form-page-panel-animate-delay" section="route" title="Manual stops" description="Reorder stop groups and merge or split parcel drops.">
              <RouteManualStopEditor stops={reconcileManualStops(manualStops, selectedIdsForManualStops)} parcelsById={parcelsById} onChange={setManualStops} />
            </DetailPanel>
          ) : null}

          <DetailPanel className="form-page-panel-animate-delay" section="route" title="Preview" description="Live route metrics, warnings, and read-only map from the depot through the route and back.">
            {!previewRequest ? <p className="text-sm text-muted-foreground">Route preview will appear once service date and zone are selected.</p> : previewLoading ? <p className="text-sm text-muted-foreground">Building route preview...</p> : previewError || !preview ? <QueryErrorAlert title="Could not build route preview" message={getErrorMessage(previewError)} /> : (
              <div className="space-y-5">
                <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
                  <div className="rounded-2xl border border-border/60 bg-background/60 p-4"><p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Zone</p><p className="mt-2 text-sm font-semibold">{preview.zoneName}</p></div>
                  <div className="rounded-2xl border border-border/60 bg-background/60 p-4"><p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Depot</p><p className="mt-2 text-sm font-semibold">{preview.depotName}</p><p className="mt-1 text-xs text-muted-foreground">{preview.depotAddressLine}</p></div>
                  <div className="rounded-2xl border border-border/60 bg-background/60 p-4"><p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Stops</p><p className="mt-2 text-sm font-semibold">{preview.estimatedStopCount}</p></div>
                  <div className="rounded-2xl border border-border/60 bg-background/60 p-4"><p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Planned distance</p><p className="mt-2 text-sm font-semibold">{formatDistance(preview.plannedDistanceMeters)}</p></div>
                  <div className="rounded-2xl border border-border/60 bg-background/60 p-4"><p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Planned duration</p><p className="mt-2 text-sm font-semibold">{formatDuration(preview.plannedDurationSeconds)}</p></div>
                </div>
                {preview.warnings.length > 0 ? <div className="rounded-2xl border border-amber-500/30 bg-amber-500/8 p-4"><p className="text-sm font-semibold text-amber-900 dark:text-amber-200">Planner warnings</p><ul className="mt-2 space-y-1 text-sm text-amber-900/90 dark:text-amber-200/90">{preview.warnings.map((warning) => <li key={warning}>{warning}</li>)}</ul></div> : null}
                <RouteMap path={preview.path} stops={preview.stops} depot={{ name: preview.depotName, addressLine: preview.depotAddressLine, longitude: preview.depotLongitude, latitude: preview.depotLatitude }} emptyMessage="No preview geometry is available for the current selection." />
              </div>
            )}
          </DetailPanel>

          <FormActionsBar>
            <Link href="/routes" className={cn(buttonVariants({ variant: "outline", size: "default" }), "w-full justify-center sm:w-auto")}>Cancel</Link>
            <Button type="submit" className="w-full sm:w-auto" disabled={createRoute.isPending || !canCreateRoute}>{createRoute.isPending ? "Creating" : "Create route"}</Button>
          </FormActionsBar>
        </form>
      </div>
    </DetailFormPageShell>
  );
}
