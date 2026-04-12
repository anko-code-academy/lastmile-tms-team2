"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ArrowLeft, PackagePlus, PackageX, PencilLine } from "lucide-react";
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
  FORM_PAGE_FORM_COLUMN_CLASS,
} from "@/components/detail";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { ListPageHeader } from "@/components/list";
import { Button, buttonVariants } from "@/components/ui/button";
import { ROUTE_STATUS_LABELS } from "@/lib/labels/routes";
import { getErrorMessage } from "@/lib/network/error-message";
import { cn } from "@/lib/utils";
import {
  useAddParcelToDispatchedRoute,
  useDispatchedRouteParcelCandidates,
  useRemoveParcelFromDispatchedRoute,
  useRoute,
} from "@/queries/routes";
import type { Route } from "@/types/routes";
import { RouteAdjustmentBanner } from "./route-adjustment-banner";

function RouteAdjustForm({
  routeId,
  route,
}: {
  routeId: string;
  route: Route;
}) {
  const addParcelToRoute = useAddParcelToDispatchedRoute();
  const removeParcelFromRoute = useRemoveParcelFromDispatchedRoute();
  const [addReason, setAddReason] = useState("");
  const [removeReason, setRemoveReason] = useState("");
  const [addError, setAddError] = useState<string | null>(null);
  const [removeError, setRemoveError] = useState<string | null>(null);
  const {
    data: candidates = [],
    isLoading: candidatesLoading,
    error: candidatesError,
  } = useDispatchedRouteParcelCandidates(routeId, route.status === "DISPATCHED");

  const currentParcels = useMemo(
    () =>
      route.stops.flatMap((stop) =>
        stop.parcels.map((parcel) => ({
          ...parcel,
          stopSequence: stop.sequence,
        })),
      ),
    [route.stops],
  );
  const parcelAdjustmentAuditTrail = useMemo(
    () =>
      [...route.parcelAdjustmentAuditTrail].sort(
        (left, right) =>
          new Date(right.changedAt).getTime() - new Date(left.changedAt).getTime(),
      ),
    [route.parcelAdjustmentAuditTrail],
  );

  const handleAddParcel = async (parcelId: string) => {
    const reason = addReason.trim();
    if (!reason) {
      setAddError("A reason is required before adding a parcel.");
      return;
    }

    setAddError(null);
    await addParcelToRoute.mutateAsync({
      id: routeId,
      data: {
        parcelId,
        reason,
      },
    });
    setAddReason("");
  };

  const handleRemoveParcel = async (parcelId: string) => {
    const reason = removeReason.trim();
    if (!reason) {
      setRemoveError("A reason is required before removing a parcel.");
      return;
    }

    setRemoveError(null);
    await removeParcelFromRoute.mutateAsync({
      id: routeId,
      data: {
        parcelId,
        reason,
      },
    });
    setRemoveReason("");
  };

  return (
    <div className="space-y-6">
      {route.latestParcelAdjustment ? (
        <RouteAdjustmentBanner
          adjustment={route.latestParcelAdjustment}
          label="Latest route change"
        />
      ) : null}

      <DetailPanel
        className="form-page-panel-animate"
        section="route"
        title="Current dispatched route"
        description="Adjust one parcel at a time while keeping the current stop order intact."
      >
        <DetailFieldGrid>
          <DetailField label="Status">{ROUTE_STATUS_LABELS[route.status]}</DetailField>
          <DetailField label="Zone">{route.zoneName}</DetailField>
          <DetailField label="Driver">{route.driverName}</DetailField>
          <DetailField label="Vehicle">{route.vehiclePlate}</DetailField>
          <DetailField label="Assigned parcels">{route.parcelCount}</DetailField>
          <DetailField label="Stops">{route.estimatedStopCount}</DetailField>
          <DetailField label="Dispatched at">
            {route.dispatchedAt ? new Date(route.dispatchedAt).toLocaleString() : ""}
          </DetailField>
          <DetailField label="Last updated">
            {route.updatedAt ? new Date(route.updatedAt).toLocaleString() : ""}
          </DetailField>
        </DetailFieldGrid>

        <div className="mt-6 grid gap-3 md:grid-cols-2">
          {route.stops.map((stop) => (
            <div
              key={stop.id}
              className="rounded-2xl border border-border/60 bg-background/60 p-4"
            >
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold">Stop {stop.sequence}</p>
                  <p className="text-sm text-muted-foreground">{stop.recipientLabel}</p>
                </div>
                <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium">
                  {stop.parcels.length} parcel{stop.parcels.length === 1 ? "" : "s"}
                </span>
              </div>
              <p className="mt-3 text-sm">{stop.addressLine}</p>
              <div className="mt-3 space-y-2">
                {stop.parcels.map((parcel) => (
                  <div
                    key={parcel.parcelId}
                    className="rounded-xl border border-border/60 bg-muted/20 px-3 py-2 text-sm"
                  >
                    <p className="font-medium">{parcel.trackingNumber}</p>
                    <p className="text-xs text-muted-foreground">
                      {parcel.recipientLabel}
                    </p>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </DetailPanel>

      <DetailPanel
        className="form-page-panel-animate-delay"
        section="route"
        title="Add staged parcel"
        description="Choose from unassigned staged parcels in the same route zone and depot."
      >
        <div className="space-y-4">
          <DetailFormField
            label="Reason for adding"
            htmlFor="add-route-parcel-reason"
            error={addError ?? undefined}
          >
            <textarea
              id="add-route-parcel-reason"
              rows={3}
              value={addReason}
              onChange={(event) => {
                setAddReason(event.target.value);
                if (addError) {
                  setAddError(null);
                }
              }}
              className={cn(
                "min-h-24 w-full rounded-xl border border-input/90 bg-background px-3.5 py-2.5 text-sm shadow-sm transition-[color,box-shadow,border-color] outline-none",
                "hover:border-input focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/45",
                addError && "border-destructive focus-visible:ring-destructive/30",
              )}
              placeholder="Explain why this parcel needs to be added after dispatch."
            />
          </DetailFormField>

          {candidatesError ? (
            <QueryErrorAlert
              title="Could not load staged parcel candidates"
              message={getErrorMessage(candidatesError)}
            />
          ) : null}

          {candidatesLoading ? (
            <p className="text-sm text-muted-foreground">
              Loading staged parcel candidates...
            </p>
          ) : candidates.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              There are no unassigned staged parcels available for this dispatched route.
            </p>
          ) : (
            <div className="space-y-3">
              {candidates.map((candidate) => (
                <div
                  key={candidate.id}
                  className="flex flex-col gap-3 rounded-2xl border border-border/60 bg-background/60 p-4 lg:flex-row lg:items-center lg:justify-between"
                >
                  <div className="space-y-1">
                    <p className="text-sm font-semibold">{candidate.trackingNumber}</p>
                    <p className="text-sm text-muted-foreground">
                      {candidate.recipientLabel}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {candidate.addressLine}
                    </p>
                  </div>
                  <Button
                    type="button"
                    disabled={addParcelToRoute.isPending}
                    onClick={() => void handleAddParcel(candidate.id)}
                  >
                    <PackagePlus className="mr-2 size-4" aria-hidden />
                    Add parcel
                  </Button>
                </div>
              ))}
            </div>
          )}
        </div>
      </DetailPanel>

      <DetailPanel
        className="form-page-panel-animate-delay"
        section="route"
        title="Remove route parcel"
        description="Removing a parcel returns it to staged status at the depot."
      >
        <div className="space-y-4">
          <DetailFormField
            label="Reason for removal"
            htmlFor="remove-route-parcel-reason"
            error={removeError ?? undefined}
          >
            <textarea
              id="remove-route-parcel-reason"
              rows={3}
              value={removeReason}
              onChange={(event) => {
                setRemoveReason(event.target.value);
                if (removeError) {
                  setRemoveError(null);
                }
              }}
              className={cn(
                "min-h-24 w-full rounded-xl border border-input/90 bg-background px-3.5 py-2.5 text-sm shadow-sm transition-[color,box-shadow,border-color] outline-none",
                "hover:border-input focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/45",
                removeError && "border-destructive focus-visible:ring-destructive/30",
              )}
              placeholder="Explain why this parcel needs to be removed after dispatch."
            />
          </DetailFormField>

          {route.parcelCount <= 1 ? (
            <p className="text-sm text-muted-foreground">
              The final parcel cannot be removed from a dispatched route. Cancel the
              route instead if it should be cleared entirely.
            </p>
          ) : (
            <div className="space-y-3">
              {currentParcels.map((parcel) => (
                <div
                  key={parcel.parcelId}
                  className="flex flex-col gap-3 rounded-2xl border border-border/60 bg-background/60 p-4 lg:flex-row lg:items-center lg:justify-between"
                >
                  <div className="space-y-1">
                    <p className="text-sm font-semibold">{parcel.trackingNumber}</p>
                    <p className="text-sm text-muted-foreground">
                      {parcel.recipientLabel}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Stop {parcel.stopSequence} | {parcel.addressLine}
                    </p>
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    disabled={removeParcelFromRoute.isPending || route.parcelCount <= 1}
                    onClick={() => void handleRemoveParcel(parcel.parcelId)}
                  >
                    <PackageX className="mr-2 size-4" aria-hidden />
                    Remove parcel
                  </Button>
                </div>
              ))}
            </div>
          )}
        </div>
      </DetailPanel>

      <DetailPanel
        className="form-page-panel-animate-delay"
        section="route"
        title="Adjustment log"
        description="Every change captures a timestamp, reason, and the parcel involved."
      >
        {parcelAdjustmentAuditTrail.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No stop adjustments have been applied to this route yet.
          </p>
        ) : (
          <div className="space-y-3">
            {parcelAdjustmentAuditTrail.map((entry) => (
              <div
                key={entry.id}
                className="rounded-2xl border border-border/60 bg-background/60 p-4"
              >
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="text-sm font-semibold">{entry.trackingNumber}</p>
                    <p className="text-xs text-muted-foreground">
                      {new Date(entry.changedAt).toLocaleString()}
                      {entry.changedBy ? ` | ${entry.changedBy}` : ""}
                    </p>
                  </div>
                  <span className="rounded-full bg-sky-100 px-2 py-0.5 text-xs font-medium text-sky-900">
                    {entry.action === "REMOVED" ? "Removed" : "Added"}
                  </span>
                </div>
                <div className="mt-3 grid gap-3 sm:grid-cols-2">
                  <div className="rounded-xl border border-border/60 bg-muted/20 p-3">
                    <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                      Reason
                    </p>
                    <p className="mt-2 text-sm">{entry.reason}</p>
                  </div>
                  <div className="rounded-xl border border-border/60 bg-muted/20 p-3">
                    <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                      Stop sequence after change
                    </p>
                    <p className="mt-2 text-sm">
                      {entry.affectedStopSequence ?? "Removed stop"}
                    </p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </DetailPanel>
    </div>
  );
}

export default function RouteAdjustPage() {
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
            { label: "Adjust stops" },
          ]}
        />
        <div className={FORM_PAGE_FORM_COLUMN_CLASS}>
          <QueryErrorAlert
            title="Could not load dispatched route"
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
            { label: "Adjust stops" },
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

  if (route.status !== "DISPATCHED") {
    return (
      <DetailFormPageShell variant="route">
        <DetailBreadcrumb
          className="form-page-breadcrumb-animate"
          variant="route"
          items={[
            { label: "Routes", href: "/routes" },
            { label: `Route ${id.slice(0, 8)}`, href: `/routes/${id}` },
            { label: "Adjust stops" },
          ]}
        />
        <div className={FORM_PAGE_FORM_COLUMN_CLASS}>
          <DetailEmptyState
            title="Stop adjustments are locked"
            message="Only dispatched routes can add or remove parcels during the day."
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
          { label: "Adjust stops" },
        ]}
      />

      <ListPageHeader
        variant="route"
        eyebrow="Dispatch"
        title="Adjust stops"
        description={`Add staged parcels or remove cancelled parcels from dispatched route ${id.slice(0, 8)}.`}
        icon={<PencilLine strokeWidth={1.75} />}
        action={(
          <Link
            href={`/routes/${id}`}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            <ArrowLeft className="mr-2 size-4" aria-hidden />
            Back to route
          </Link>
        )}
      />

      <div className={cn(FORM_PAGE_FORM_COLUMN_CLASS, "form-page-body-animate")}>
        <RouteAdjustForm routeId={id} route={route} />
      </div>
    </DetailFormPageShell>
  );
}
