"use client";

import { useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, CalendarDays, PencilLine } from "lucide-react";
import { useSession } from "next-auth/react";

import {
  DetailBreadcrumb,
  DetailEmptyState,
  DetailField,
  DetailFieldGrid,
  DetailFormField,
  DetailFormPageShell,
  DetailPageSkeleton,
  DetailPanel,
  FormActionsBar,
  FORM_PAGE_FORM_COLUMN_CLASS,
} from "@/components/detail";
import { ListPageHeader } from "@/components/list";
import { Button, buttonVariants } from "@/components/ui/button";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { SelectDropdown } from "@/components/form/select-dropdown";
import { driverSelectOptions } from "@/lib/forms/drivers";
import { vehicleSelectOptionsForRoute } from "@/lib/forms/vehicles";
import { ROUTE_STATUS_LABELS, STAGING_AREA_LABELS } from "@/lib/labels/routes";
import { getErrorMessage } from "@/lib/network/error-message";
import { API_RESOURCE_LOAD_ERROR } from "@/lib/network/api-messages";
import { cn } from "@/lib/utils";
import {
  useRoute,
  useRouteAssignmentCandidates,
  useUpdateRouteAssignment,
} from "@/queries/routes";
import type { Route } from "@/types/routes";

function RouteEditForm({
  routeId,
  route,
}: {
  routeId: string;
  route: Route;
}) {
  const router = useRouter();
  const updateRouteAssignment = useUpdateRouteAssignment();
  const [formData, setFormData] = useState({
    vehicleId: route.vehicleId,
    driverId: route.driverId,
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

  const {
    data: assignmentCandidates,
    isLoading: assignmentLoading,
    error: assignmentError,
  } = useRouteAssignmentCandidates(route.startDate, routeId);

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

    return drivers.filter(
      (driver) =>
        driver.depotId === selectedVehicle.depotId || driver.isCurrentAssignment,
    );
  }, [assignmentCandidates?.drivers, selectedVehicle]);
  const selectedDriver = useMemo(
    () => availableDrivers.find((driver) => driver.id === formData.driverId) ?? null,
    [availableDrivers, formData.driverId],
  );
  const hasChanges =
    formData.vehicleId !== route.vehicleId || formData.driverId !== route.driverId;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const nextErrors: Record<string, string> = {};
    if (!formData.vehicleId) nextErrors.vehicleId = "Select a value from the list.";
    if (!formData.driverId) nextErrors.driverId = "Select a value from the list.";
    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors);
      return;
    }

    setErrors({});

    try {
      await updateRouteAssignment.mutateAsync({
        id: routeId,
        data: {
          vehicleId: formData.vehicleId,
          driverId: formData.driverId,
        },
      });
      router.push(`/routes/${routeId}`);
    } catch {
      /* error toast from global MutationCache */
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <DetailPanel
        className="form-page-panel-animate"
        section="route"
        title="Current route"
        description="Assignment can be changed while the route is still planned."
      >
        <DetailFieldGrid>
          <DetailField label="Status">{ROUTE_STATUS_LABELS[route.status]}</DetailField>
          <DetailField label="Start date">
            {new Date(route.startDate).toLocaleString()}
          </DetailField>
          <DetailField label="Staging area">
            {STAGING_AREA_LABELS[route.stagingArea]}
          </DetailField>
          <DetailField label="Current vehicle">{route.vehiclePlate}</DetailField>
          <DetailField label="Current driver">{route.driverName}</DetailField>
          <DetailField label="Assigned parcels">{route.parcelCount}</DetailField>
        </DetailFieldGrid>
      </DetailPanel>

      <DetailPanel
        className="form-page-panel-animate-delay"
        section="route"
        title="Reassign driver and vehicle"
        description="Availability is based on the route service date."
      >
        <div className="space-y-6">
          <DetailFormField
            label="Vehicle"
            htmlFor="vehicle"
            error={errors.vehicleId}
          >
            <SelectDropdown
              id="vehicle"
              options={vehicleSelectOptionsForRoute(vehicles)}
              value={formData.vehicleId}
              invalid={!!errors.vehicleId}
              onChange={(value) => {
                clearError("vehicleId");
                setFormData((prev) => ({
                  ...prev,
                  vehicleId: value,
                  driverId: "",
                }));
              }}
              disabled={assignmentLoading}
              placeholder={assignmentLoading ? "Loading vehicles" : "Select vehicle"}
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
          </DetailFormField>

          <DetailFormField
            label="Driver"
            htmlFor="driver"
            error={errors.driverId}
          >
            <SelectDropdown
              id="driver"
              options={driverSelectOptions(availableDrivers)}
              value={formData.driverId}
              invalid={!!errors.driverId}
              onChange={(value) => {
                clearError("driverId");
                setFormData((prev) => ({ ...prev, driverId: value }));
              }}
              disabled={!selectedVehicle || assignmentLoading}
              placeholder={
                !selectedVehicle
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
                  No drivers are available for this depot on the route service date.
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
                      Other routes recorded for this driver on the same date.
                    </p>
                  </div>

                  {selectedDriver.workloadRoutes.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                      No other same-day routes recorded.
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {selectedDriver.workloadRoutes.map((workloadRoute) => (
                        <div
                          key={workloadRoute.routeId}
                          className="rounded-xl border border-border/60 bg-muted/30 px-3 py-2 text-sm"
                        >
                          <div className="font-medium">
                            Route {workloadRoute.routeId.slice(0, 8)} |{" "}
                            {workloadRoute.vehiclePlate}
                          </div>
                          <div className="text-xs text-muted-foreground">
                            {new Date(workloadRoute.startDate).toLocaleString()} |{" "}
                            {ROUTE_STATUS_LABELS[workloadRoute.status]}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {assignmentError && (
            <QueryErrorAlert
              title="Could not load assignment candidates"
              message={`${getErrorMessage(assignmentError)} ${API_RESOURCE_LOAD_ERROR}`}
            />
          )}
        </div>
      </DetailPanel>

      <FormActionsBar>
        <Link
          href={`/routes/${routeId}`}
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
          disabled={updateRouteAssignment.isPending || !hasChanges}
        >
          {updateRouteAssignment.isPending ? "Saving" : "Save assignment"}
        </Button>
      </FormActionsBar>
    </form>
  );
}

export default function RouteEditPage() {
  const { id } = useParams<{ id: string }>();
  const { status: sessionStatus } = useSession();
  const { data: route, isLoading, error } = useRoute(id);

  if (sessionStatus === "loading" || isLoading) {
    return <DetailPageSkeleton variant="route" />;
  }

  if (error) {
    return (
      <DetailFormPageShell variant="route">
        <DetailBreadcrumb
          className="form-page-breadcrumb-animate"
          variant="route"
          items={[
            { label: "Routes", href: "/routes" },
            { label: "Edit assignment" },
          ]}
        />
        <div className={FORM_PAGE_FORM_COLUMN_CLASS}>
          <QueryErrorAlert
            title="Could not load route"
            message={getErrorMessage(error)}
          />
        </div>
      </DetailFormPageShell>
    );
  }

  if (!route) {
    return (
      <DetailFormPageShell variant="route">
        <DetailBreadcrumb
          className="form-page-breadcrumb-animate"
          variant="route"
          items={[
            { label: "Routes", href: "/routes" },
            { label: "Edit assignment" },
          ]}
        />
        <div className={FORM_PAGE_FORM_COLUMN_CLASS}>
          <DetailEmptyState
            title="Route not found"
            message="This route may have been removed or the link is incorrect."
          />
        </div>
      </DetailFormPageShell>
    );
  }

  if (route.status !== "PLANNED") {
    return (
      <DetailFormPageShell variant="route">
        <DetailBreadcrumb
          className="form-page-breadcrumb-animate"
          variant="route"
          items={[
            { label: "Routes", href: "/routes" },
            { label: `Route ${id.slice(0, 8)}`, href: `/routes/${id}` },
            { label: "Edit assignment" },
          ]}
        />
        <div className={FORM_PAGE_FORM_COLUMN_CLASS}>
          <DetailEmptyState
            title="Assignment is locked"
            message="Only planned routes can be reassigned before dispatch."
          />
        </div>
      </DetailFormPageShell>
    );
  }

  return (
    <DetailFormPageShell variant="route">
      <DetailBreadcrumb
        className="form-page-breadcrumb-animate"
        variant="route"
        items={[
          { label: "Routes", href: "/routes" },
          { label: `Route ${id.slice(0, 8)}`, href: `/routes/${id}` },
          { label: "Edit assignment" },
        ]}
      />

      <ListPageHeader
        variant="route"
        eyebrow="Dispatch"
        title="Edit assignment"
        description={`Change driver and vehicle allocation for route ${id.slice(0, 8)} before dispatch.`}
        icon={<PencilLine strokeWidth={1.75} />}
        action={
          <Link
            href={`/routes/${id}`}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            <ArrowLeft className="mr-2 size-4" aria-hidden />
            Back to route
          </Link>
        }
      />

      <div className={cn(FORM_PAGE_FORM_COLUMN_CLASS, "form-page-body-animate")}>
        <RouteEditForm key={id} routeId={id} route={route} />
      </div>
    </DetailFormPageShell>
  );
}
