"use client";

import { useState, useMemo } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Route } from "lucide-react";

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
import { API_RESOURCE_LOAD_ERROR } from "@/lib/network/api-messages";
import { driverSelectOptions } from "@/lib/forms/drivers";
import { vehicleSelectOptionsForRoute } from "@/lib/forms/vehicles";
import {
  formatParcelWeightUnitLabel,
  parcelWeightKg,
} from "@/lib/parcels/display";
import { cn } from "@/lib/utils";
import { routeCreateFormSchema } from "@/lib/validation/routes";
import { zodErrorToFieldMap } from "@/lib/validation/zod-field-errors";
import { useCreateRoute } from "@/queries/routes";
import { useVehicles } from "@/queries/vehicles";
import { useParcelsForRouteCreation } from "@/queries/parcels";
import { useDrivers } from "@/queries/drivers";
import type { VehicleStatus } from "@/types/vehicles";

export default function NewRoutePage() {
  const router = useRouter();
  const createRoute = useCreateRoute();
  const [formData, setFormData] = useState({
    vehicleId: "",
    driverId: "",
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

  const { data: vehiclesData = [] } = useVehicles({
    status: "AVAILABLE",
  });
  const { data: parcels = [], isLoading: parcelsLoading, error: parcelsError } =
    useParcelsForRouteCreation();

  const selectedVehicle = useMemo(
    () => vehiclesData.find((v) => v.id === formData.vehicleId),
    [vehiclesData, formData.vehicleId],
  );

  const { data: drivers = [], isLoading: driversLoading, error: driversError } =
    useDrivers(selectedVehicle?.depotId);

  const vehicleOptions = useMemo(
    () => vehicleSelectOptionsForRoute(vehiclesData),
    [vehiclesData],
  );
  const driverOptions = useMemo(
    () => driverSelectOptions(drivers),
    [drivers],
  );

  const selectedParcels = useMemo(
    () => parcels.filter((p) => formData.parcelIds.includes(p.id)),
    [parcels, formData.parcelIds],
  );

  const totalWeightKg = useMemo(
    () =>
      selectedParcels.reduce(
        (sum, p) => sum + parcelWeightKg(p.weight, p.weightUnit),
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
        ? prev.parcelIds.filter((p) => p !== id)
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
        description="Assign a vehicle, driver, start time, and parcels for this run."
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
          Choose a vehicle first, then a driver from the same depot. Set
          departure time and select parcels that fit capacity.
        </p>
        <DetailPanel
          className="form-page-panel-animate"
          section="route"
          title="Trip"
          description="Vehicle, driver, and odometer at departure."
        >
          <div className="space-y-6">
            <DetailFormField
              label="Vehicle"
              htmlFor="vehicle"
              error={errors.vehicleId}
            >
              <SelectDropdown
                id="vehicle"
                options={vehicleOptions}
                value={formData.vehicleId}
                invalid={!!errors.vehicleId}
                onChange={(v) => {
                  clearError("vehicleId");
                  setFormData({ ...formData, vehicleId: v, driverId: "" });
                }}
                placeholder="Select vehicle"
              />
              {selectedVehicle && (
                <p className="text-xs text-muted-foreground">
                  Capacity: {selectedVehicle.parcelCapacity} parcels,{" "}
                  {selectedVehicle.weightCapacity} kg
                </p>
              )}
            </DetailFormField>

            <DetailFormField
              label="Driver"
              htmlFor="driver"
              error={errors.driverId}
            >
              <SelectDropdown
                id="driver"
                options={driverOptions}
                value={formData.driverId}
                invalid={!!errors.driverId}
                onChange={(v) => {
                  clearError("driverId");
                  setFormData({ ...formData, driverId: v });
                }}
                disabled={!formData.vehicleId || driversLoading}
                placeholder={
                  !formData.vehicleId
                    ? "Select a vehicle first"
                    : driversLoading
                      ? "Loading driversР Р†Р вЂљР’В¦"
                      : "Select driver"
                }
              />
              {driversError && (
                <p className="text-xs text-destructive">
                  Could not load drivers. {API_RESOURCE_LOAD_ERROR}
                </p>
              )}
            </DetailFormField>

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
                    setFormData({ ...formData, startDate: value });
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
                  onChange={(v) => {
                    clearError("startMileage");
                    setFormData({ ...formData, startMileage: v });
                  }}
                />
              </DetailFormField>
            </div>
          </div>
        </DetailPanel>

        <DetailPanel
          className="form-page-panel-animate-delay"
          section="route"
          title="Parcels"
          description="Choose parcels that are sorted or staged. Totals must fit the vehicle limits."
        >
          <div className="space-y-4">
            <div
              className="max-h-52 space-y-1 overflow-y-auto rounded-xl border border-border/60 bg-background/50 p-3"
              role="group"
              aria-label="Available parcels"
            >
              {parcelsLoading && (
                <p className="text-sm text-muted-foreground">Loading parcelsР Р†Р вЂљР’В¦</p>
              )}
              {parcelsError && (
                <p className="text-sm text-destructive">
                  Could not load parcels. {API_RESOURCE_LOAD_ERROR}
                </p>
              )}
              {!parcelsLoading && !parcelsError && parcels.length === 0 && (
                <p className="text-sm text-muted-foreground">
                  No parcels ready for a route (status Sorted or Staged).
                </p>
              )}
              {!parcelsLoading &&
                !parcelsError &&
                parcels.map((p) => (
                  <label
                    key={p.id}
                    className="flex cursor-pointer items-center gap-3 rounded-lg p-2 transition-colors hover:bg-muted/50"
                  >
                    <input
                      type="checkbox"
                      checked={formData.parcelIds.includes(p.id)}
                      onChange={() => toggleParcel(p.id)}
                      className="size-4 rounded border-input"
                    />
                    <span className="flex-1 text-sm">
                      {p.trackingNumber} ({p.weight}{" "}
                      {formatParcelWeightUnitLabel(p.weightUnit)})
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
            {createRoute.isPending ? "CreatingР Р†Р вЂљР’В¦" : "Create route"}
          </Button>
        </FormActionsBar>
        </form>
      </div>
    </DetailFormPageShell>
  );
}
