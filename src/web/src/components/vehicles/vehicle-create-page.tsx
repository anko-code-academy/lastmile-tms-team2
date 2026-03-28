"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Truck } from "lucide-react";

import {
  DetailBreadcrumb,
  DetailFormField,
  DetailFormPageShell,
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
import { vehicleTypeSelectOptions } from "@/lib/labels/vehicles";
import { cn } from "@/lib/utils";
import { depotSelectOptions } from "@/lib/forms/depots";
import { API_RESOURCE_LOAD_ERROR } from "@/lib/network/api-messages";
import {
  parsePositiveDecimalInput,
  sanitizePositiveDecimalInput,
} from "@/lib/validation/positive-decimal";
import { vehicleCreateFormSchema } from "@/lib/validation/vehicles";
import { zodErrorToFieldMap } from "@/lib/validation/zod-field-errors";
import { useDepots } from "@/queries/depots";
import { useCreateVehicle } from "@/queries/vehicles";
import type { VehicleType, VehicleStatus } from "@/types/vehicles";

export default function NewVehiclePage() {
  const router = useRouter();
  const createVehicle = useCreateVehicle();
  const { data: depots = [], isLoading: depotsLoading, error: depotsError } =
    useDepots();
  const depotOptions = useMemo(() => depotSelectOptions(depots), [depots]);

  const [formData, setFormData] = useState({
    registrationPlate: "",
    type: "VAN" as VehicleType,
    parcelCapacity: 0,
    weightInput: "",
    depotId: "",
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const weightCapacity =
      parsePositiveDecimalInput(
        sanitizePositiveDecimalInput(formData.weightInput),
      ) ?? Number.NaN;
    const parsed = vehicleCreateFormSchema.safeParse({
      registrationPlate: formData.registrationPlate,
      type: formData.type,
      parcelCapacity: formData.parcelCapacity,
      weightCapacity,
      status: "AVAILABLE" as VehicleStatus,
      depotId: formData.depotId,
    });
    if (!parsed.success) {
      setErrors(zodErrorToFieldMap(parsed.error));
      return;
    }
    setErrors({});
    try {
      await createVehicle.mutateAsync(parsed.data);
      router.push("/vehicles");
    } catch {
      /* error toast from global MutationCache */
    }
  };

  return (
    <DetailFormPageShell variant="vehicle">
      <DetailBreadcrumb
        className="form-page-breadcrumb-animate"
        variant="vehicle"
        items={[
          { label: "Vehicles", href: "/vehicles" },
          { label: "New vehicle" },
        ]}
      />

      <ListPageHeader
        eyebrow="Fleet"
        title="Add vehicle"
        description="Register a new vehicle and assign it to a depot."
        icon={<Truck strokeWidth={1.75} />}
        action={
          <Link
            href="/vehicles"
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            <ArrowLeft className="mr-2 size-4" aria-hidden />
            All vehicles
          </Link>
        }
      />

      <div
        className={cn(FORM_PAGE_FORM_COLUMN_CLASS, "form-page-body-animate")}
      >
        <form onSubmit={handleSubmit} className="space-y-6">
        <DetailPanel
          className="form-page-panel-animate"
          section="vehicle"
          title="Vehicle details"
          description="Identification, capacity, and operational status."
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
                      treatZeroAsEmpty
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

                <DetailFormField
                  label="Status"
                  description="New vehicles are registered as Available."
                >
                  <p className="rounded-xl border border-input/90 bg-muted/30 px-3.5 py-2 text-sm text-foreground">
                    Available
                  </p>
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
                href="/vehicles"
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
                disabled={createVehicle.isPending}
              >
                {createVehicle.isPending ? "Creating" : "Create vehicle"}
              </Button>
            </FormActionsBar>
        </form>
      </div>
    </DetailFormPageShell>
  );
}
