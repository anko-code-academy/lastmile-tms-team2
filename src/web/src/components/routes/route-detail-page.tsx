"use client";

import { use, useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  BellRing,
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
import {
  getDriverRouteNotice,
  type DriverRouteNoticeTone,
} from "@/lib/routes/driver-route-notice";
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
  useDriverRouteRealtimeUpdates,
  useMyRoute,
  useRoute,
  useStartRoute,
} from "@/queries/routes";
import { RouteAdjustmentBanner } from "./route-adjustment-banner";
import { CancelRouteDialog } from "./cancel-route-dialog";
import { CompleteRouteDialog } from "./complete-route-dialog";
import { RouteMap } from "./route-map";

type RouteDetailMode = "dispatch" | "driver";

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

function formatParcelAdjustmentActionLabel(action: "ADDED" | "REMOVED") {
  return action === "REMOVED" ? "Parcel removed" : "Parcel added";
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

function formatOptionalDateTime(iso: string | null | undefined): string {
  if (!iso) {
    return "";
  }

  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toLocaleString();
}

function driverNoticeToneClass(tone: DriverRouteNoticeTone): string {
  switch (tone) {
    case "planned":
      return "border-sky-300/60 bg-sky-50/80 text-sky-950";
    case "ready":
      return "border-emerald-300/60 bg-emerald-50/80 text-emerald-950";
    case "active":
      return "border-amber-300/60 bg-amber-50/80 text-amber-950";
    default:
      return "border-border/60 bg-muted/30 text-foreground";
  }
}

export default function RouteDetailPage({
  params,
  mode = "dispatch",
}: {
  params: Promise<{ id: string }>;
  mode?: RouteDetailMode;
}) {
  const { id } = use(params);
  const { status: sessionStatus } = useSession();
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
  const [completeDialogOpen, setCompleteDialogOpen] = useState(false);
  const cancelRoute = useCancelRoute();
  const dispatchRoute = useDispatchRoute();
  const startRoute = useStartRoute();
  const completeRoute = useCompleteRoute();
  const isDriverMode = mode === "driver";
  const routeQuery = useRoute(id, !isDriverMode);
  const myRouteQuery = useMyRoute(id, isDriverMode);
  const { data: route, isLoading, error } = isDriverMode ? myRouteQuery : routeQuery;
  useDriverRouteRealtimeUpdates(isDriverMode);

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
            items={[
              {
                label: isDriverMode ? "My Schedule" : "Routes",
                href: isDriverMode ? "/routes/my" : "/routes",
              },
              { label: "Not found" },
            ]}
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
  const routeNotice = isDriverMode ? getDriverRouteNotice(route) : null;
  const assignmentAuditTrail = [...route.assignmentAuditTrail].sort(
    (left, right) =>
      new Date(right.changedAt).getTime() - new Date(left.changedAt).getTime(),
  );
  const parcelAdjustmentAuditTrail = [...route.parcelAdjustmentAuditTrail].sort(
    (left, right) =>
      new Date(right.changedAt).getTime() - new Date(left.changedAt).getTime(),
  );

  const canEditAssignment = !isDriverMode && route.status === "DRAFT";
  const canAdjustStops = !isDriverMode && route.status === "DISPATCHED";
  const canDispatch = !isDriverMode && route.status === "DRAFT";
  const canCancel =
    !isDriverMode && (route.status === "DRAFT" || route.status === "DISPATCHED");
  const canStart = !isDriverMode && route.status === "DISPATCHED";
  const canComplete = !isDriverMode && route.status === "IN_PROGRESS";

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
              {
                label: isDriverMode ? "My Schedule" : "Routes",
                href: isDriverMode ? "/routes/my" : "/routes",
              },
              { label: `Route ${shortId}` },
            ]}
          />

          <DetailHero
            section="route"
            eyebrow={isDriverMode ? "My schedule" : "Dispatch"}
            icon={<RouteIcon strokeWidth={1.75} />}
            title={route.vehiclePlate}
            subtitle={
              isDriverMode ? (
                <>
                  Route <span className="font-mono text-foreground/80">{id}</span>
                  {" | "}
                  {route.zoneName}
                  {" | "}
                  Departure {formatOptionalDateTime(route.startDate)}
                </>
              ) : (
                <>
                  Route <span className="font-mono text-foreground/80">{id}</span>
                  {" | "}
                  {route.driverName}
                  {" | "}
                  {route.zoneName}
                </>
              )
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
                {canAdjustStops ? (
                  <Link
                    href={`/routes/${id}/adjust`}
                    className={cn(buttonVariants({ variant: "default", size: "sm" }))}
                  >
                    <PencilLine className="mr-2 size-4" aria-hidden />
                    Adjust stops
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
                {!isDriverMode ? (
                  <Link
                    href={`/vehicles/${route.vehicleId}`}
                    className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                  >
                    <MapPin className="mr-2 size-4" aria-hidden />
                    Open vehicle
                  </Link>
                ) : null}
                <Link
                  href={isDriverMode ? "/routes/my" : "/routes"}
                  className={cn(buttonVariants({ variant: "ghost", size: "sm" }))}
                >
                  <ArrowLeft className="mr-2 size-4" aria-hidden />
                  {isDriverMode ? "My schedule" : "All routes"}
                </Link>
              </>
            }
          />

          {route.latestParcelAdjustment ? (
            <RouteAdjustmentBanner
              adjustment={route.latestParcelAdjustment}
              label={isDriverMode ? "Route update" : "Latest route change"}
            />
          ) : null}

          {routeNotice ? (
            <div
              className={cn(
                "detail-panel-animate rounded-3xl border px-5 py-4 shadow-sm",
                driverNoticeToneClass(routeNotice.tone),
              )}
            >
              <div className="flex items-start gap-3">
                <div className="rounded-2xl bg-background/60 p-2">
                  <BellRing className="size-5" aria-hidden />
                </div>
                <div className="space-y-1">
                  <p className="text-sm font-semibold uppercase tracking-[0.18em]">
                    Driver message
                  </p>
                  <h2 className="text-lg font-semibold">{routeNotice.title}</h2>
                  <p className="text-sm opacity-90">{routeNotice.description}</p>
                </div>
              </div>
            </div>
          ) : null}

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
              isDriverMode
                ? {
                    label: "Dispatch",
                    value: route.dispatchedAt
                      ? formatOptionalDateTime(route.dispatchedAt)
                      : "Pending",
                    hint:
                      route.status === "DISPATCHED"
                        ? "Ready to leave"
                        : route.status === "IN_PROGRESS"
                          ? "Route in progress"
                          : "Waiting for dispatch",
                    icon: <Send className="size-5" aria-hidden />,
                  }
                : {
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
            description={
              isDriverMode
                ? "Departure timing, depot context, and assigned route information."
                : "Zone, depot dispatch data, odometer readings, and live assignment context."
            }
          >
            <DetailFieldGrid>
              <DetailField label="Zone">{route.zoneName}</DetailField>
              <DetailField label="Depot">
                {route.depotName ? (
                  <div className="space-y-1">
                    <div>{route.depotName}</div>
                    {route.depotAddressLine ? (
                      <div className="text-xs text-muted-foreground">
                        {route.depotAddressLine}
                      </div>
                    ) : null}
                  </div>
                ) : (
                  ""
                )}
              </DetailField>
              <DetailField label="Vehicle">
                {isDriverMode ? (
                  route.vehiclePlate
                ) : (
                  <Link
                    href={`/vehicles/${route.vehicleId}`}
                    className="font-mono text-primary underline-offset-4 hover:underline"
                  >
                    {route.vehiclePlate}
                  </Link>
                )}
              </DetailField>
              <DetailField label="Driver">
                {isDriverMode ? (
                  route.driverName
                ) : (
                  <Link
                    href={`/drivers/${route.driverId}`}
                    className="text-primary underline-offset-4 hover:underline"
                  >
                    {route.driverName}
                  </Link>
                )}
              </DetailField>
              <DetailField label="Staging area">
                {STAGING_AREA_LABELS[route.stagingArea]}
              </DetailField>
              <DetailField label="Start date">
                {new Date(route.startDate).toLocaleString()}
              </DetailField>
              <DetailField label="Dispatched at">
                {formatOptionalDateTime(route.dispatchedAt)}
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

          {!isDriverMode && (route.status === "DISPATCHED" || parcelAdjustmentAuditTrail.length > 0) ? (
            <DetailPanel
              className="detail-panel-animate"
              section="route"
              title="Route change log"
              description="Adds and removals applied after dispatch, including reason and timestamp."
            >
              {parcelAdjustmentAuditTrail.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  No parcels have been added to or removed from this dispatched route.
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
                          <p className="text-sm font-semibold">
                            {formatParcelAdjustmentActionLabel(entry.action)}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {new Date(entry.changedAt).toLocaleString()}
                            {entry.changedBy ? ` | ${entry.changedBy}` : ""}
                          </p>
                        </div>
                        <span className="rounded-full bg-sky-100 px-2 py-0.5 text-xs font-medium text-sky-900">
                          {entry.trackingNumber}
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
          ) : null}

          {!isDriverMode ? (
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
          ) : null}

          {!isDriverMode ? (
            <>
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
            </>
          ) : null}
        </DetailPageSectionProvider>
      </DetailContainer>
    </DetailShell>
  );
}
