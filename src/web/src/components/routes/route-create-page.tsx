"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, CalendarDays, Route } from "lucide-react";

import {
  DetailBreadcrumb,
  DetailFormField,
  DetailPanel,
  DetailFormPageShell,
  FormActionsBar,
  FORM_PAGE_FORM_COLUMN_CLASS,
} from "@/components/detail";
import { ListPageHeader } from "@/components/list";
import { Button, buttonVariants } from "@/components/ui/button";
import { DateTimePicker } from "@/components/form/date-time-picker";
import { NaturalNumberInput } from "@/components/form/natural-number-input";
import { SelectDropdown } from "@/components/form/select-dropdown";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { STAGING_AREA_LABELS, ROUTE_STATUS_LABELS } from "@/lib/labels/routes";
import { API_RESOURCE_LOAD_ERROR } from "@/lib/network/api-messages";
import { driverSelectOptions } from "@/lib/forms/drivers";
import { vehicleSelectOptionsForRoute } from "@/lib/forms/vehicles";
import {
  formatParcelWeightUnitLabel,
  parcelWeightKg,
} from "@/lib/parcels/display";
import { getErrorMessage } from "@/lib/network/error-message";
import { cn } from "@/lib/utils";
import { routeCreateFormSchema } from "@/lib/validation/routes";
import { zodErrorToFieldMap } from "@/lib/validation/zod-field-errors";
import {
  useCreateRoute,
  useRouteAssignmentCandidates,
} from "@/queries/routes";
import { useParcelsForRouteCreation } from "@/queries/parcels";

const stagingAreaOptions = [
  { value: "A", label: STAGING_AREA_LABELS.A },
  { value: "B", label: STAGING_AREA_LABELS.B },
] as const;

function toServiceDate(value: string): string | null {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

export default function NewRoutePage() {
  const router = useRouter();
  const createRoute = useCreateRoute();
  const [formData, setFormData] = useState({
    vehicleId: "",
    driverId: "",
    stagingArea: "" as "" | "A" | "B",
    startDate: new Date().toISOString().slice(0, 16),
    startMileage: 0,
    parcelIds: [] as string[],
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const clearError = (key: string) => {
    setErrors((prev) => {
      if (prev[key] === undefined) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  };

  const serviceDate = useMemo(
    () => toServiceDate(formData.startDate),
    [formData.startDate],
  );
  const {
    data: assignmentCandidates,
    isLoading: assignmentLoading,
    error: assignmentError,
  } = useRouteAssignmentCandidates(serviceDate);

  const vehicles = assignmentCandidates?.vehicles ?? [];
  const selectedVehicle = useMemo(
    () => vehicles.find((vehicle) => vehicle.id === formData.vehicleId) ?? null,
    [vehicles, formData.vehicleId],
  );

  const availableDrivers = useMemo(() => {
    const drivers = assignmentCandidates?.drivers ?? [];
    if (!selectedVehicle) {
      return drivers;
    }

    return drivers.filter((driver) => driver.depotId === selectedVehicle.depotId);
  }, [assignmentCandidates?.drivers, selectedVehicle]);

  const selectedDriver = useMemo(
    () => availableDrivers.find((driver) => driver.id === formData.driverId) ?? null,
    [availableDrivers, formData.driverId],
  );

  useEffect(() => {
    if (!serviceDate) {
      setFormData((prev) =>
        prev.vehicleId || prev.driverId || prev.parcelIds.length > 0
          ? { ...prev, vehicleId: "", driverId: "", parcelIds: [] }
          : prev,
      );
      return;
    }

    if (assignmentLoading) {
      return;
    }

    setFormData((prev) => {
      if (!prev.vehicleId) {
        return prev;
      }

      if (vehicles.some((vehicle) => vehicle.id === prev.vehicleId)) {
        return prev;
      }

      return {
        ...prev,
        vehicleId: "",
        driverId: "",
        parcelIds: [],
      };
    });
  }, [assignmentLoading, serviceDate, vehicles]);

  useEffect(() => {
    if (assignmentLoading) {
      return;
    }

    setFormData((prev) => {
      if (!prev.driverId) {
        return prev;
      }

      if (availableDrivers.some((driver) => driver.id === prev.driverId)) {
        return prev;
      }

      return {
        ...prev,
        driverId: "",
        parcelIds: [],
      };
    });
  }, [assignmentLoading, availableDrivers]);

  const {
    data: parcels = [],
    isLoading: parcelsLoading,
    error: parcelsError,
  } = useParcelsForRouteCreation(selectedVehicle?.id, selectedDriver?.id);

  const vehicleOptions = useMemo(
    () => vehicleSelectOptionsForRoute(vehicles),
    [vehicles],
  );
  const driverOptions = useMemo(
    () => driverSelectOptions(availableDrivers),
    [availableDrivers],
  );

  const selectedParcels = useMemo(
    () => parcels.filter((p) => formData.parcelIds.includes(p.id)),
    [parcels, formData.parcelIds],
  );

  const totalWeightKg = useMemo(
    () =>
      selectedParcels.reduce(
        (sum, parcel) => sum + parcelWeightKg(parcel.weight, parcel.weightUnit),
        0,
      ),
    [selectedParcels],
  );

  const parcelCapacityOk =
    !selectedVehicle || selectedParcels.length <= selectedVehicle.parcelCapacity;
  const weightCapacityOk =
    !selectedVehicle || totalWeightKg <= selectedVehicle.weightCapacity;
  const capacityOk = parcelCapacityOk && weightCapacityOk;

  const toggleParcel = (id: string) => {
    setFormData((prev) => ({
      ...prev,
      parcelIds: prev.parcelIds.includes(id)
        ? prev.parcelIds.filter((parcelId) => parcelId !== id)
        : [...prev.parcelIds, id],
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const parsed = routeCreateFormSchema.safeParse(formData);
    if (!parsed.success) {
      setErrors(zodErrorToFieldMap(parsed.error));
      return;
    }
    setErrors({});
    if (!capacityOk) return;
    try {
      await createRoute.mutateAsync({
        ...parsed.data,
        startDate: new Date(parsed.data.startDate).toISOString(),
      });
      router.push("/routes");
    } catch {
      /* error toast from global MutationCache */
    }
  };

  return (
    <DetailFormPageShell variant="route">
      <DetailBreadcrumb
        className="form-page-breadcrumb-animate"
        variant="route"
        items={[
          { label: "Routes", href: "/routes" },
          { label: "New route" },
        ]}
      />

      <ListPageHeader
        variant="route"
        eyebrow="Dispatch"
        title="Create route"
        description="Assign a vehicle, driver, staging area, start time, and expected parcels for this run."
        icon={<Route strokeWidth={1.75} />}
        action={
          <Link
            href="/routes"
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            <ArrowLeft className="mr-2 size-4" aria-hidden />
            All routes
          </Link>
        }
      />

      <div
        className={cn(FORM_PAGE_FORM_COLUMN_CLASS, "form-page-body-animate")}
      >
        <form
          onSubmit={handleSubmit}
          className="space-y-6"
          aria-describedby="form-route-help"
        >
          <p id="form-route-help" className="sr-only">
            Choose a route start date first, then assign an available vehicle and
            driver for that service day before selecting parcels.
          </p>

          <DetailPanel
            className="form-page-panel-animate"
            section="route"
            title="Trip"
            description="Vehicle, driver, staging area, and odometer at departure."
          >
            <div className="space-y-6">
              <div className="grid gap-6 sm:grid-cols-2">
                <DetailFormField
                  label="Start date"
                  htmlFor="start"
                  error={errors.startDate}
                >
                  <DateTimePicker
                    value={formData.startDate}
                    invalid={!!errors.startDate}
                    onChange={(value) => {
                      clearError("startDate");
                      setFormData((prev) => ({
                        ...prev,
                        startDate: value,
                      }));
                    }}
                  />
                </DetailFormField>
                <DetailFormField
                  label="Start mileage (km)"
                  htmlFor="mileage"
                  description="Odometer reading at route start."
                  error={errors.startMileage}
                >
                  <NaturalNumberInput
                    id="mileage"
                    value={formData.startMileage}
                    aria-invalid={errors.startMileage ? true : undefined}
                    onChange={(value) => {
                      clearError("startMileage");
                      setFormData((prev) => ({ ...prev, startMileage: value }));
                    }}
                  />
                </DetailFormField>
              </div>

              <DetailFormField
                label="Vehicle"
                htmlFor="vehicle"
                error={errors.vehicleId}
                description="Vehicles are filtered by the selected service date."
              >
                <SelectDropdown
                  id="vehicle"
                  options={vehicleOptions}
                  value={formData.vehicleId}
                  invalid={!!errors.vehicleId}
                  onChange={(value) => {
                    clearError("vehicleId");
                    setFormData((prev) => ({
                      ...prev,
                      vehicleId: value,
                      driverId: "",
                      parcelIds: [],
                    }));
                  }}
                  disabled={!serviceDate || assignmentLoading}
                  placeholder={
                    !serviceDate
                      ? "Choose a start date first"
                      : assignmentLoading
                        ? "Loading vehicles"
                        : "Select vehicle"
                  }
                />
                {selectedVehicle && (
                  <p className="text-xs text-muted-foreground">
                    {selectedVehicle.depotName
                      ? `${selectedVehicle.depotName} | `
                      : ""}
                    Capacity: {selectedVehicle.parcelCapacity} parcels,{" "}
                    {selectedVehicle.weightCapacity} kg
                  </p>
                )}
                {assignmentError && (
                  <p className="text-xs text-destructive">
                    Could not load route assignment candidates. {API_RESOURCE_LOAD_ERROR}
                  </p>
                )}
              </DetailFormField>

              <DetailFormField
                label="Driver"
                htmlFor="driver"
                error={errors.driverId}
                description="Drivers shown here are available for the service date and selected depot."
              >
                <SelectDropdown
                  id="driver"
                  options={driverOptions}
                  value={formData.driverId}
                  invalid={!!errors.driverId}
                  onChange={(value) => {
                    clearError("driverId");
                    setFormData((prev) => ({
                      ...prev,
                      driverId: value,
                      parcelIds: [],
                    }));
                  }}
                  disabled={!selectedVehicle || assignmentLoading}
                  placeholder={
                    !serviceDate
                      ? "Choose a start date first"
                      : !selectedVehicle
                        ? "Select a vehicle first"
                        : assignmentLoading
                          ? "Loading drivers"
                          : "Select driver"
                  }
                />
                {!assignmentLoading &&
                  selectedVehicle &&
                  availableDrivers.length === 0 && (
                    <p className="text-xs text-muted-foreground">
                      No drivers are currently available for this depot on the selected date.
                    </p>
                  )}
              </DetailFormField>

              {selectedDriver && (
                <div className="rounded-2xl border border-border/60 bg-background/60 p-4">
                  <div className="flex items-start gap-3">
                    <div className="rounded-xl bg-muted/70 p-2 text-muted-foreground">
                      <CalendarDays className="size-4" aria-hidden />
                    </div>
                    <div className="space-y-2">
                      <div>
                        <p className="text-sm font-semibold">Driver workload</p>
                        <p className="text-xs text-muted-foreground">
                          Other routes already assigned on this service date.
                        </p>
                      </div>

                      {selectedDriver.workloadRoutes.length === 0 ? (
                        <p className="text-sm text-muted-foreground">
                          No other same-day routes recorded.
                        </p>
                      ) : (
                        <div className="space-y-2">
                          {selectedDriver.workloadRoutes.map((route) => (
                            <div
                              key={route.routeId}
                              className="rounded-xl border border-border/60 bg-muted/30 px-3 py-2 text-sm"
                            >
                              <div className="font-medium">
                                Route {route.routeId.slice(0, 8)} | {route.vehiclePlate}
                              </div>
                              <div className="text-xs text-muted-foreground">
                                {new Date(route.startDate).toLocaleString()} |{" "}
                                {ROUTE_STATUS_LABELS[route.status]}
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              )}

              <DetailFormField
                label="Staging area"
                htmlFor="staging-area"
                error={errors.stagingArea}
                description="Assign the route to area A or B for warehouse grouping."
              >
                <SelectDropdown
                  id="staging-area"
                  options={[...stagingAreaOptions]}
                  value={formData.stagingArea}
                  invalid={!!errors.stagingArea}
                  onChange={(value) => {
                    clearError("stagingArea");
                    setFormData((prev) => ({ ...prev, stagingArea: value }));
                  }}
                  placeholder="Select staging area"
                />
              </DetailFormField>
            </div>
          </DetailPanel>

          <DetailPanel
            className="form-page-panel-animate-delay"
            section="route"
            title="Parcels"
            description="Choose parcels expected on this route. Totals must fit the vehicle limits, and warehouse operators will stage them later."
          >
            <div className="space-y-4">
              {assignmentError && (
                <QueryErrorAlert
                  title="Could not load assignment options"
                  message={getErrorMessage(assignmentError)}
                />
              )}

              <div
                className="max-h-52 space-y-1 overflow-y-auto rounded-xl border border-border/60 bg-background/50 p-3"
                role="group"
                aria-label="Available parcels"
              >
                {!serviceDate && (
                  <p className="text-sm text-muted-foreground">
                    Choose a route start date to load eligible assignments.
                  </p>
                )}
                {serviceDate && !formData.vehicleId && (
                  <p className="text-sm text-muted-foreground">
                    Select a vehicle first, then choose a driver to load matching parcels.
                  </p>
                )}
                {formData.vehicleId && !formData.driverId && (
                  <p className="text-sm text-muted-foreground">
                    Choose a driver to load parcels from that driver&apos;s zone.
                  </p>
                )}
                {parcelsLoading && (
                  <p className="text-sm text-muted-foreground">Loading parcels</p>
                )}
                {parcelsError && (
                  <p className="text-sm text-destructive">
                    Could not load parcels. {API_RESOURCE_LOAD_ERROR}
                  </p>
                )}
                {!parcelsLoading &&
                  !parcelsError &&
                  formData.vehicleId &&
                  formData.driverId &&
                  parcels.length === 0 && (
                    <p className="text-sm text-muted-foreground">
                      No parcels ready for this driver&apos;s zone (status Sorted or Staged).
                    </p>
                  )}
                {!parcelsLoading &&
                  !parcelsError &&
                  parcels.map((parcel) => (
                    <label
                      key={parcel.id}
                      className="flex cursor-pointer items-center gap-3 rounded-lg p-2 transition-colors hover:bg-muted/50"
                    >
                      <input
                        type="checkbox"
                        checked={formData.parcelIds.includes(parcel.id)}
                        onChange={() => toggleParcel(parcel.id)}
                        className="size-4 rounded border-input"
                      />
                      <span className="flex-1 text-sm">
                        {parcel.trackingNumber} ({parcel.weight}{" "}
                        {formatParcelWeightUnitLabel(parcel.weightUnit)})
                        {parcel.zoneName ? ` - ${parcel.zoneName}` : ""}
                      </span>
                    </label>
                  ))}
              </div>

              <div className="space-y-2 text-sm">
                <p
                  className={
                    !parcelCapacityOk
                      ? "font-medium text-destructive"
                      : "text-foreground"
                  }
                >
                  Selected: {selectedParcels.length} parcels
                  {selectedVehicle && ` / ${selectedVehicle.parcelCapacity} max`}
                </p>
                {!parcelCapacityOk && (
                  <p className="text-destructive">Parcel capacity exceeded.</p>
                )}
                <p
                  className={
                    !weightCapacityOk
                      ? "font-medium text-destructive"
                      : "text-foreground"
                  }
                >
                  Total weight: {totalWeightKg.toFixed(2)} kg
                  {selectedVehicle && ` / ${selectedVehicle.weightCapacity} kg max`}
                </p>
                {!weightCapacityOk && (
                  <p className="text-destructive">Weight capacity exceeded.</p>
                )}
              </div>
            </div>
          </DetailPanel>

          <FormActionsBar>
            <Link
              href="/routes"
              className={cn(
                buttonVariants({ variant: "outline", size: "default" }),
                "w-full justify-center sm:w-auto",
              )}
            >
              Cancel
            </Link>
            <Button
              type="submit"
              className="w-full sm:w-auto"
              disabled={createRoute.isPending || !capacityOk}
            >
              {createRoute.isPending ? "Creating" : "Create route"}
            </Button>
          </FormActionsBar>
        </form>
      </div>
    </DetailFormPageShell>
  );
}
