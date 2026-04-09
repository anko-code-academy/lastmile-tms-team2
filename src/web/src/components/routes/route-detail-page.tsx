"use client";

import { use } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  CalendarClock,
  Gauge,
  MapPin,
  Package,
  PencilLine,
  Route as RouteIcon,
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
import { buttonVariants } from "@/components/ui/button";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { getErrorMessage } from "@/lib/network/error-message";
import { cn } from "@/lib/utils";
import {
  ROUTE_STATUS_LABELS,
  STAGING_AREA_LABELS,
  routeStatusBadgeClass,
} from "@/lib/labels/routes";
import { useRoute } from "@/queries/routes";

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

export default function RouteDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { status: sessionStatus } = useSession();
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
              </>
            }
            badge={
              <span className={routeStatusBadgeClass(route.status)}>
                {ROUTE_STATUS_LABELS[route.status]}
              </span>
            }
            actions={
              <>
                {route.status === "PLANNED" && (
                  <Link
                    href={`/routes/${id}/edit`}
                    className={cn(buttonVariants({ variant: "default", size: "sm" }))}
                  >
                    <PencilLine className="mr-2 size-4" aria-hidden />
                    Edit assignment
                  </Link>
                )}
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
                label: "Start",
                value: new Date(route.startDate).toLocaleString(undefined, {
                  dateStyle: "medium",
                  timeStyle: "short",
                }),
                icon: <CalendarClock className="size-5" aria-hidden />,
              },
              {
                label: "Total distance",
                value: `${route.totalMileage.toLocaleString()} km`,
                icon: <Gauge className="size-5" aria-hidden />,
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
              {
                label: "Staging area",
                value: STAGING_AREA_LABELS[route.stagingArea],
                icon: <MapPin className="size-5" aria-hidden />,
              },
            ]}
          />

          <DetailPanel
            className="detail-panel-animate"
            section="route"
            title="Route details"
            description="Schedule, odometer readings, and dispatch assignment."
          >
            <DetailFieldGrid>
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
              <DetailField label="Parcels delivered">
                {route.parcelsDelivered} of {route.parcelCount}
              </DetailField>
              <DetailField label="Recorded">{formatCreatedAt(route.createdAt)}</DetailField>
              <DetailField label="Last modified">
                {route.updatedAt ? new Date(route.updatedAt).toLocaleString() : ""}
              </DetailField>
            </DetailFieldGrid>
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
        </DetailPageSectionProvider>
      </DetailContainer>
    </DetailShell>
  );
}
