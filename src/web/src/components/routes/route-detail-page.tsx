"use client";

import { use, useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  CalendarClock,
  Gauge,
  MapPin,
  Package,
  PencilLine,
  Play,
  Route as RouteIcon,
  Send,
  SquareCheckBig,
  User,
} from "lucide-react";
import { useSession } from "next-auth/react";
import {
  DetailBreadcrumb,
  DetailContainer,
  DetailEmptyState,
  DetailField,
  DetailFieldGrid,
  DetailHero,
  DetailMetricStrip,
  DetailPageSectionProvider,
  DetailPageSkeleton,
  DetailPanel,
  DetailShell,
  DETAIL_PAGE_CONTENT_PADDING,
} from "@/components/detail";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { getErrorMessage } from "@/lib/network/error-message";
import {
  ROUTE_STATUS_LABELS,
  STAGING_AREA_LABELS,
  routeStatusBadgeClass,
} from "@/lib/labels/routes";
import { cn } from "@/lib/utils";
import {
  useCancelRoute,
  useCompleteRoute,
  useDispatchRoute,
  useRoute,
  useStartRoute,
} from "@/queries/routes";
import { CancelRouteDialog } from "./cancel-route-dialog";
import { CompleteRouteDialog } from "./complete-route-dialog";
import { RouteMap } from "./route-map";

function formatCreatedAt(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime()) || date.getUTCFullYear() < 2000) {
    return "";
  }
  return date.toLocaleString();
}

function formatAuditActionLabel(action: "ASSIGNED" | "REASSIGNED") {
  return action === "ASSIGNED" ? "Initial assignment" : "Reassignment";
}

function formatDistance(meters: number): string {
  return `${(meters / 1000).toFixed(1)} km`;
}

function formatDuration(seconds: number): string {
  if (seconds <= 0) {
    return "0 min";
  }

  const hours = Math.floor(seconds / 3600);
  const minutes = Math.round((seconds % 3600) / 60);
  if (hours <= 0) {
    return `${minutes} min`;
  }
  if (minutes <= 0) {
    return `${hours} hr`;
  }
  return `${hours} hr ${minutes} min`;
}

export default function RouteDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { status: sessionStatus } = useSession();
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
  const [completeDialogOpen, setCompleteDialogOpen] = useState(false);
  const cancelRoute = useCancelRoute();
  const dispatchRoute = useDispatchRoute();
  const startRoute = useStartRoute();
  const completeRoute = useCompleteRoute();
  const { data: route, isLoading, error } = useRoute(id);

  if (sessionStatus === "loading" || isLoading) {
    return <DetailPageSkeleton variant="route" />;
  }

  if (error) {
    return (
      <DetailShell variant="route">
        <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
          <QueryErrorAlert
            title="Could not load route"
            message={getErrorMessage(error)}
          />
        </DetailContainer>
      </DetailShell>
    );
  }

  if (!route) {
    return (
      <DetailShell variant="route">
        <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
          <DetailBreadcrumb
            variant="route"
            items={[{ label: "Routes", href: "/routes" }, { label: "Not found" }]}
          />
          <DetailEmptyState
            title="Route not found"
            message="This route may have been removed or the link is incorrect."
          />
        </DetailContainer>
      </DetailShell>
    );
  }

  const shortId = id.slice(0, 8);
  const assignmentAuditTrail = [...route.assignmentAuditTrail].sort(
    (left, right) =>
      new Date(right.changedAt).getTime() - new Date(left.changedAt).getTime(),
  );

  const canEditAssignment = route.status === "DRAFT";
  const canDispatch = route.status === "DRAFT";
  const canCancel = route.status === "DRAFT" || route.status === "DISPATCHED";
  const canStart = route.status === "DISPATCHED";
  const canComplete = route.status === "IN_PROGRESS";

  const handleCancelRoute = async (reason: string) => {
    await cancelRoute.mutateAsync({
      id,
      data: { reason },
    });
    setCancelDialogOpen(false);
  };

  const handleCompleteRoute = async (endMileage: number) => {
    await completeRoute.mutateAsync({
      id,
      data: { endMileage },
    });
    setCompleteDialogOpen(false);
  };

  return (
    <DetailShell variant="route">
      <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
        <DetailPageSectionProvider section="route">
          <DetailBreadcrumb
            variant="route"
            items={[
              { label: "Routes", href: "/routes" },
              { label: `Route ${shortId}` },
            ]}
          />

          <DetailHero
            section="route"
            eyebrow="Dispatch"
            icon={<RouteIcon strokeWidth={1.75} />}
            title={route.vehiclePlate}
            subtitle={
              <>
                Route <span className="font-mono text-foreground/80">{id}</span>
                {" | "}
                {route.driverName}
                {" | "}
                {route.zoneName}
              </>
            }
            badge={
              <span className={routeStatusBadgeClass(route.status)}>
                {ROUTE_STATUS_LABELS[route.status]}
              </span>
            }
            actions={
              <>
                {canEditAssignment ? (
                  <Link
                    href={`/routes/${id}/edit`}
                    className={cn(buttonVariants({ variant: "default", size: "sm" }))}
                  >
                    <PencilLine className="mr-2 size-4" aria-hidden />
                    Edit assignment
                  </Link>
                ) : null}
                {canDispatch ? (
                  <Button
                    size="sm"
                    disabled={dispatchRoute.isPending}
                    onClick={() => dispatchRoute.mutate(id)}
                  >
                    <Send className="mr-2 size-4" aria-hidden />
                    Dispatch
                  </Button>
                ) : null}
                {canStart ? (
                  <Button
                    size="sm"
                    disabled={startRoute.isPending}
                    onClick={() => startRoute.mutate(id)}
                  >
                    <Play className="mr-2 size-4" aria-hidden />
                    Start route
                  </Button>
                ) : null}
                {canComplete ? (
                  <Button
                    size="sm"
                    disabled={completeRoute.isPending}
                    onClick={() => setCompleteDialogOpen(true)}
                  >
                    <SquareCheckBig className="mr-2 size-4" aria-hidden />
                    Complete
                  </Button>
                ) : null}
                {canCancel ? (
                  <Button
                    variant="destructive"
                    size="sm"
                    disabled={cancelRoute.isPending}
                    onClick={() => setCancelDialogOpen(true)}
                  >
                    Cancel route
                  </Button>
                ) : null}
                <Link
                  href={`/vehicles/${route.vehicleId}`}
                  className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                >
                  <MapPin className="mr-2 size-4" aria-hidden />
                  Open vehicle
                </Link>
                <Link
                  href="/routes"
                  className={cn(buttonVariants({ variant: "ghost", size: "sm" }))}
                >
                  <ArrowLeft className="mr-2 size-4" aria-hidden />
                  All routes
                </Link>
              </>
            }
          />

          <DetailMetricStrip
            items={[
              {
                label: "Service date",
                value: new Date(route.startDate).toLocaleString(undefined, {
                  dateStyle: "medium",
                  timeStyle: "short",
                }),
                icon: <CalendarClock className="size-5" aria-hidden />,
              },
              {
                label: "Planned distance",
                value: formatDistance(route.plannedDistanceMeters),
                hint: formatDuration(route.plannedDurationSeconds),
                icon: <Gauge className="size-5" aria-hidden />,
              },
              {
                label: "Stops",
                value: route.estimatedStopCount.toLocaleString(),
                hint: "Planned delivery stops",
                icon: <MapPin className="size-5" aria-hidden />,
              },
              {
                label: "Parcels",
                value: `${route.parcelsDelivered} / ${route.parcelCount}`,
                hint: "Delivered / assigned",
                icon: <Package className="size-5" aria-hidden />,
              },
              {
                label: "Driver",
                value: route.driverName,
                icon: <User className="size-5" aria-hidden />,
              },
            ]}
          />

          <DetailPanel
            className="detail-panel-animate"
            section="route"
            title="Route details"
            description="Zone, depot dispatch data, odometer readings, and live assignment context."
          >
            <DetailFieldGrid>
              <DetailField label="Zone">{route.zoneName}</DetailField>
              <DetailField label="Depot">
                {route.depotName ? (
                  <div className="space-y-1">
                    <div>{route.depotName}</div>
                    {route.depotAddressLine ? <div className="text-xs text-muted-foreground">{route.depotAddressLine}</div> : null}
                  </div>
                ) : ""}
              </DetailField>
              <DetailField label="Vehicle">
                <Link
                  href={`/vehicles/${route.vehicleId}`}
                  className="font-mono text-primary underline-offset-4 hover:underline"
                >
                  {route.vehiclePlate}
                </Link>
              </DetailField>
              <DetailField label="Driver">
                <Link
                  href={`/drivers/${route.driverId}`}
                  className="text-primary underline-offset-4 hover:underline"
                >
                  {route.driverName}
                </Link>
              </DetailField>
              <DetailField label="Staging area">
                {STAGING_AREA_LABELS[route.stagingArea]}
              </DetailField>
              <DetailField label="Start date">
                {new Date(route.startDate).toLocaleString()}
              </DetailField>
              <DetailField label="End date">
                {route.endDate ? new Date(route.endDate).toLocaleString() : ""}
              </DetailField>
              <DetailField label="Start mileage">
                {route.startMileage.toLocaleString()} km
              </DetailField>
              <DetailField label="End mileage">
                {route.endMileage > 0 ? `${route.endMileage.toLocaleString()} km` : ""}
              </DetailField>
              <DetailField label="Recorded">{formatCreatedAt(route.createdAt)}</DetailField>
              <DetailField label="Last modified">
                {route.updatedAt ? new Date(route.updatedAt).toLocaleString() : ""}
              </DetailField>
              {route.cancellationReason ? (
                <DetailField label="Cancellation reason">
                  {route.cancellationReason}
                </DetailField>
              ) : null}
            </DetailFieldGrid>
          </DetailPanel>

          <DetailPanel
            className="detail-panel-animate"
            section="route"
            title="Planned map"
            description="Read-only route geometry from the depot through numbered route stops and back to the depot."
          >
            <div className="space-y-4">
              <RouteMap
                path={route.path}
                stops={route.stops}
                depot={{ name: route.depotName ?? "Depot", addressLine: route.depotAddressLine, longitude: route.depotLongitude, latitude: route.depotLatitude }}
                emptyMessage="No route geometry is available for this route yet."
              />
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                {route.stops.map((stop) => (
                  <div
                    key={stop.id}
                    className="rounded-2xl border border-border/60 bg-background/60 p-4"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold">
                          Stop {stop.sequence}
                        </p>
                        <p className="text-sm text-muted-foreground">
                          {stop.recipientLabel}
                        </p>
                      </div>
                      <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium">
                        {stop.parcels.length} parcel{stop.parcels.length === 1 ? "" : "s"}
                      </span>
                    </div>
                    <p className="mt-3 text-sm">{stop.addressLine}</p>
                  </div>
                ))}
                {route.stops.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    No persisted stops are available on this route.
                  </p>
                ) : null}
              </div>
            </div>
          </DetailPanel>

          <DetailPanel
            className="detail-panel-animate"
            section="route"
            title="Assignment audit"
            description="Driver and vehicle allocation changes captured before dispatch."
          >
            {assignmentAuditTrail.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                No assignment changes have been recorded for this route yet.
              </p>
            ) : (
              <div className="space-y-3">
                {assignmentAuditTrail.map((entry) => (
                  <div
                    key={entry.id}
                    className="rounded-2xl border border-border/60 bg-background/60 p-4"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold">
                          {formatAuditActionLabel(entry.action)}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {new Date(entry.changedAt).toLocaleString()}
                          {entry.changedBy ? ` | ${entry.changedBy}` : ""}
                        </p>
                      </div>
                      <span className={routeStatusBadgeClass(route.status)}>
                        {ROUTE_STATUS_LABELS[route.status]}
                      </span>
                    </div>

                    <div className="mt-3 grid gap-3 sm:grid-cols-2">
                      <div className="rounded-xl border border-border/60 bg-muted/20 p-3">
                        <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                          Driver
                        </p>
                        <p className="mt-2 text-sm">
                          {entry.previousDriverName ?? "Unassigned"}
                          {" -> "}
                          {entry.newDriverName}
                        </p>
                      </div>
                      <div className="rounded-xl border border-border/60 bg-muted/20 p-3">
                        <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                          Vehicle
                        </p>
                        <p className="mt-2 text-sm">
                          {entry.previousVehiclePlate ?? "Unassigned"}
                          {" -> "}
                          {entry.newVehiclePlate}
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </DetailPanel>

          <CancelRouteDialog
            open={cancelDialogOpen}
            onOpenChange={setCancelDialogOpen}
            routeLabel={id}
            onConfirm={handleCancelRoute}
            isPending={cancelRoute.isPending}
          />
          <CompleteRouteDialog
            open={completeDialogOpen}
            onOpenChange={setCompleteDialogOpen}
            routeLabel={id}
            startMileage={route.startMileage}
            isPending={completeRoute.isPending}
            onConfirm={handleCompleteRoute}
          />
        </DetailPageSectionProvider>
      </DetailContainer>
    </DetailShell>
  );
}
