"use client";

import { useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, PencilLine } from "lucide-react";
import { useSession } from "next-auth/react";

import {
  DetailBreadcrumb,
  DetailEmptyState,
  DetailFormField,
  DetailFormPageShell,
  DetailPageSkeleton,
  DetailPanel,
  FormActionsBar,
  FORM_PAGE_FORM_COLUMN_CLASS,
} from "@/components/detail";
import { ListPageHeader } from "@/components/list";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NaturalNumberInput } from "@/components/form/natural-number-input";
import { SelectDropdown } from "@/components/form/select-dropdown";
import { WeightCapacityInput } from "@/components/form/weight-capacity-input";
import {
  vehicleStatusSelectOptions,
  vehicleTypeSelectOptions,
} from "@/lib/labels/vehicles";
import { depotSelectOptions } from "@/lib/forms/depots";
import { API_RESOURCE_LOAD_ERROR } from "@/lib/network/api-messages";
import { cn } from "@/lib/utils";
import {
  parsePositiveDecimalInput,
  sanitizePositiveDecimalInput,
} from "@/lib/validation/positive-decimal";
import { vehicleEditFormSchema } from "@/lib/validation/vehicles";
import { zodErrorToFieldMap } from "@/lib/validation/zod-field-errors";
import { useDepots } from "@/queries/depots";
import { useVehicle, useUpdateVehicle } from "@/queries/vehicles";
import type { Vehicle } from "@/types/vehicles";

function VehicleEditForm({
  vehicleId,
  vehicle,
}: {
  vehicleId: string;
  vehicle: Vehicle;
}) {
  const router = useRouter();
  const updateVehicle = useUpdateVehicle();
  const { data: depots = [], isLoading: depotsLoading, error: depotsError } =
    useDepots();
  const depotOptions = useMemo(() => depotSelectOptions(depots), [depots]);

  const [formData, setFormData] = useState(() => ({
    registrationPlate: vehicle.registrationPlate,
    type: vehicle.type,
    parcelCapacity: vehicle.parcelCapacity,
    weightInput: sanitizePositiveDecimalInput(String(vehicle.weightCapacity)),
    status: vehicle.status,
    depotId: vehicle.depotId,
  }));
  const [errors, setErrors] = useState<Record<string, string>>({});

  const clearError = (key: string) => {
    setErrors((prev) => {
      if (prev[key] === undefined) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const weightCapacity =
      parsePositiveDecimalInput(
        sanitizePositiveDecimalInput(formData.weightInput),
      ) ?? Number.NaN;
    const parsed = vehicleEditFormSchema.safeParse({
      registrationPlate: formData.registrationPlate,
      type: formData.type,
      parcelCapacity: formData.parcelCapacity,
      weightCapacity,
      status: formData.status,
      depotId: formData.depotId,
    });
    if (!parsed.success) {
      setErrors(zodErrorToFieldMap(parsed.error));
      return;
    }
    setErrors({});
    try {
      await updateVehicle.mutateAsync({ id: vehicleId, data: parsed.data });
      router.push(`/vehicles/${vehicleId}`);
    } catch {
      /* error toast from global MutationCache */
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <DetailPanel
        className="form-page-panel-animate"
        section="vehicle"
        title="Vehicle details"
        description="Update identification, capacity, or depot assignment."
      >
        <div className="space-y-6">
          <DetailFormField
            label="Registration plate"
            htmlFor="plate"
            error={errors.registrationPlate}
          >
            <Input
              id="plate"
              type="text"
              autoComplete="off"
              value={formData.registrationPlate}
              aria-invalid={errors.registrationPlate ? true : undefined}
              onChange={(e) => {
                clearError("registrationPlate");
                setFormData({
                  ...formData,
                  registrationPlate: e.target.value,
                });
              }}
            />
          </DetailFormField>

          <DetailFormField label="Type" htmlFor="type" error={errors.type}>
            <SelectDropdown
              id="type"
              options={vehicleTypeSelectOptions}
              value={formData.type}
              invalid={!!errors.type}
              onChange={(v) => {
                clearError("type");
                setFormData({ ...formData, type: v });
              }}
            />
          </DetailFormField>

          <div className="grid gap-6 sm:grid-cols-2">
            <DetailFormField
              label="Parcel capacity"
              htmlFor="parcel-cap"
              error={errors.parcelCapacity}
            >
              <NaturalNumberInput
                id="parcel-cap"
                value={formData.parcelCapacity}
                aria-invalid={errors.parcelCapacity ? true : undefined}
                onChange={(v) => {
                  clearError("parcelCapacity");
                  setFormData({ ...formData, parcelCapacity: v });
                }}
              />
            </DetailFormField>
            <DetailFormField
              label="Weight capacity (kg)"
              htmlFor="weight-cap"
              description="Maximum load including cargo."
              error={errors.weightCapacity}
            >
              <WeightCapacityInput
                id="weight-cap"
                value={formData.weightInput}
                aria-invalid={errors.weightCapacity ? true : undefined}
                onChange={(raw) => {
                  clearError("weightCapacity");
                  setFormData({ ...formData, weightInput: raw });
                }}
              />
            </DetailFormField>
          </div>

          <DetailFormField label="Status" htmlFor="status" error={errors.status}>
            <SelectDropdown
              id="status"
              options={vehicleStatusSelectOptions}
              value={formData.status}
              invalid={!!errors.status}
              onChange={(v) => {
                clearError("status");
                setFormData({ ...formData, status: v });
              }}
            />
          </DetailFormField>

          <DetailFormField
            label="Depot"
            htmlFor="depot"
            description="Depot where this vehicle is based."
            error={errors.depotId}
          >
            <SelectDropdown
              id="depot"
              options={depotOptions}
              value={formData.depotId}
              invalid={!!errors.depotId}
              onChange={(v) => {
                clearError("depotId");
                setFormData({ ...formData, depotId: v });
              }}
              placeholder={
                depotsLoading ? "Loading depots" : "Select depot"
              }
            />
            {depotsError && (
              <p className="text-xs text-destructive">
                Could not load depots. {API_RESOURCE_LOAD_ERROR}
              </p>
            )}
          </DetailFormField>
        </div>
      </DetailPanel>

      <FormActionsBar>
        <Link
          href={`/vehicles/${vehicleId}`}
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
          disabled={updateVehicle.isPending}
        >
          {updateVehicle.isPending ? "Saving" : "Save changes"}
        </Button>
      </FormActionsBar>
    </form>
  );
}

export default function EditVehiclePage() {
  const { id } = useParams<{ id: string }>();
  const { status: sessionStatus } = useSession();
  const { data: vehicle, isLoading } = useVehicle(id);

  if (sessionStatus === "loading" || isLoading)
    return <DetailPageSkeleton variant="vehicle" />;
  if (!vehicle)
    return (
      <DetailFormPageShell variant="vehicle">
        <DetailBreadcrumb
          className="form-page-breadcrumb-animate"
          variant="vehicle"
          items={[
            { label: "Vehicles", href: "/vehicles" },
            { label: "Edit" },
          ]}
        />
        <div className={FORM_PAGE_FORM_COLUMN_CLASS}>
          <DetailEmptyState
            title="Vehicle not found"
            message="This vehicle may have been removed or the link is incorrect."
          />
        </div>
      </DetailFormPageShell>
    );

  return (
    <DetailFormPageShell variant="vehicle">
      <DetailBreadcrumb
        className="form-page-breadcrumb-animate"
        variant="vehicle"
        items={[
          { label: "Vehicles", href: "/vehicles" },
          { label: vehicle.registrationPlate, href: `/vehicles/${id}` },
          { label: "Edit" },
        ]}
      />

      <ListPageHeader
        eyebrow="Fleet"
        title="Edit vehicle"
        description={`Changes apply to ${vehicle.registrationPlate}.`}
        icon={<PencilLine strokeWidth={1.75} />}
        action={
          <Link
            href={`/vehicles/${id}`}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            <ArrowLeft className="mr-2 size-4" aria-hidden />
            Back to vehicle
          </Link>
        }
      />

      <div
        className={cn(FORM_PAGE_FORM_COLUMN_CLASS, "form-page-body-animate")}
      >
        <VehicleEditForm key={id} vehicleId={id} vehicle={vehicle} />
      </div>
    </DetailFormPageShell>
  );
}
