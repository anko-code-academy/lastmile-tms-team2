"use client";

import { use } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  CalendarClock,
  Gauge,
  MapPin,
  Package,
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
import { ROUTE_STATUS_LABELS, routeStatusBadgeClass } from "@/lib/labels/routes";
import { useRoute } from "@/queries/routes";

/** Backend default DateTimeOffset was not set on create for older routes year 0001 */
function formatCreatedAt(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime()) || d.getUTCFullYear() < 2000) {
    return "";
  }
  return d.toLocaleString();
}

export default function RouteDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { status: sessionStatus } = useSession();
  const { data: route, isLoading, error } = useRoute(id);

  if (sessionStatus === "loading" || isLoading)
    return <DetailPageSkeleton variant="route" />;
  if (error)
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
  if (!route)
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

  const shortId = id.slice(0, 8);

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
                {" · "}
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
          ]}
        />

        <DetailPanel
          className="detail-panel-animate"
          section="route"
          title="Route details"
          description="Schedule, odometer readings, and audit."
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
            <DetailField label="Driver">{route.driverName}</DetailField>
            <DetailField label="Start date">
              {new Date(route.startDate).toLocaleString()}
            </DetailField>
            <DetailField label="End date">
              {route.endDate
                ? new Date(route.endDate).toLocaleString()
                : ""}
            </DetailField>
            <DetailField label="Start mileage">
              {route.startMileage.toLocaleString()} km
            </DetailField>
            <DetailField label="End mileage">
              {route.endMileage > 0
                ? `${route.endMileage.toLocaleString()} km`
                : ""}
            </DetailField>
            <DetailField label="Parcels delivered">
              {route.parcelsDelivered} of {route.parcelCount}
            </DetailField>
            <DetailField label="Recorded">
              {formatCreatedAt(route.createdAt)}
            </DetailField>
          </DetailFieldGrid>
        </DetailPanel>
        </DetailPageSectionProvider>
      </DetailContainer>
    </DetailShell>
  );
}
