"use client";

import { useMemo } from "react";
import Link from "next/link";
import {
  ArrowRight,
  Building2,
  CalendarClock,
  Play,
  Route as RouteIcon,
  Send,
  Truck,
} from "lucide-react";
import { ListPageHeader, ListPageLoading } from "@/components/list";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { buttonVariants } from "@/components/ui/button";
import {
  getDriverRouteNotice,
  type DriverRouteNoticeTone,
} from "@/lib/routes/driver-route-notice";
import {
  ROUTE_STATUS_LABELS,
  routeStatusBadgeClass,
} from "@/lib/labels/routes";
import { getErrorMessage } from "@/lib/network/error-message";
import { cn } from "@/lib/utils";
import { useMyRoutes } from "@/queries/routes";
import type { Route } from "@/types/routes";

function noticeToneClass(tone: DriverRouteNoticeTone): string {
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

function sortByDeparture(left: Route, right: Route): number {
  return new Date(left.startDate).getTime() - new Date(right.startDate).getTime();
}

export default function DriverSchedulePage() {
  const { data = [], isLoading, error } = useMyRoutes();

  const routes = useMemo(
    () =>
      data
        .filter((route) =>
          route.status === "DRAFT"
          || route.status === "DISPATCHED"
          || route.status === "IN_PROGRESS",
        )
        .sort(sortByDeparture),
    [data],
  );

  const scheduledCount = routes.filter((route) => route.status === "DRAFT").length;
  const readyCount = routes.filter((route) => route.status === "DISPATCHED").length;
  const activeCount = routes.filter((route) => route.status === "IN_PROGRESS").length;

  if (isLoading) {
    return <ListPageLoading />;
  }

  if (error) {
    return (
      <QueryErrorAlert
        title="Could not load your schedule"
        message={getErrorMessage(error)}
      />
    );
  }

  return (
    <div className="space-y-6">
      <ListPageHeader
        variant="route"
        eyebrow="Driver"
        title="My Schedule"
        description="Your assigned routes, departure timing, and dispatch readiness in one place."
        icon={<RouteIcon strokeWidth={1.75} aria-hidden />}
      />

      <div className="grid gap-4 md:grid-cols-3">
        <div className="rounded-3xl border border-border/60 bg-card/85 p-5 shadow-sm">
          <p className="text-xs uppercase tracking-[0.18em] text-muted-foreground">
            Scheduled
          </p>
          <p className="mt-2 text-3xl font-semibold text-foreground">
            {scheduledCount}
          </p>
          <p className="mt-1 text-sm text-muted-foreground">
            Routes still waiting for dispatch.
          </p>
        </div>
        <div className="rounded-3xl border border-emerald-300/60 bg-emerald-50/70 p-5 shadow-sm">
          <p className="text-xs uppercase tracking-[0.18em] text-emerald-800">
            Ready to Leave
          </p>
          <p className="mt-2 text-3xl font-semibold text-emerald-950">
            {readyCount}
          </p>
          <p className="mt-1 text-sm text-emerald-900/80">
            Routes already dispatched from the depot.
          </p>
        </div>
        <div className="rounded-3xl border border-amber-300/60 bg-amber-50/70 p-5 shadow-sm">
          <p className="text-xs uppercase tracking-[0.18em] text-amber-800">
            In Progress
          </p>
          <p className="mt-2 text-3xl font-semibold text-amber-950">
            {activeCount}
          </p>
          <p className="mt-1 text-sm text-amber-900/80">
            Routes currently out on delivery.
          </p>
        </div>
      </div>

      {routes.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-border p-10 text-center">
          <p className="font-medium">No assigned routes right now</p>
          <p className="mt-2 text-sm text-muted-foreground">
            New planned or dispatched routes will appear here automatically.
          </p>
        </div>
      ) : (
        <div className="grid gap-5 xl:grid-cols-2">
          {routes.map((route) => {
            const notice = getDriverRouteNotice(route);

            return (
              <article
                key={route.id}
                className="overflow-hidden rounded-3xl border border-border/60 bg-card/85 shadow-sm"
              >
                <div className="border-b border-border/60 bg-background/40 px-6 py-5">
                  <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                    <div className="space-y-2">
                      <div className="flex flex-wrap items-center gap-3">
                        <h2 className="text-xl font-semibold tracking-tight text-foreground">
                          Route {route.id.slice(0, 8)}
                        </h2>
                        <span className={routeStatusBadgeClass(route.status)}>
                          {ROUTE_STATUS_LABELS[route.status]}
                        </span>
                      </div>
                      <p className="text-sm text-muted-foreground">{route.zoneName}</p>
                    </div>
                    <Link
                      href={`/routes/${route.id}`}
                      className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                    >
                      Open route
                      <ArrowRight className="ml-2 size-4" aria-hidden />
                    </Link>
                  </div>
                </div>

                <div className="space-y-5 px-6 py-5">
                  <div
                    className={cn(
                      "rounded-2xl border px-4 py-3 shadow-sm",
                      noticeToneClass(notice.tone),
                    )}
                  >
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] opacity-80">
                      Driver message
                    </p>
                    <h3 className="mt-2 text-lg font-semibold">{notice.title}</h3>
                    <p className="mt-1 text-sm opacity-90">{notice.description}</p>
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="rounded-2xl border border-border/60 bg-background/50 p-4">
                      <div className="flex items-center gap-2 text-sm font-medium text-foreground">
                        <CalendarClock className="size-4" aria-hidden />
                        Departure
                      </div>
                      <p className="mt-2 text-sm text-muted-foreground">
                        {new Date(route.startDate).toLocaleString(undefined, {
                          dateStyle: "medium",
                          timeStyle: "short",
                        })}
                      </p>
                    </div>

                    <div className="rounded-2xl border border-border/60 bg-background/50 p-4">
                      <div className="flex items-center gap-2 text-sm font-medium text-foreground">
                        <Truck className="size-4" aria-hidden />
                        Vehicle
                      </div>
                      <p className="mt-2 text-sm text-muted-foreground">
                        {route.vehiclePlate}
                      </p>
                    </div>

                    <div className="rounded-2xl border border-border/60 bg-background/50 p-4">
                      <div className="flex items-center gap-2 text-sm font-medium text-foreground">
                        <Building2 className="size-4" aria-hidden />
                        Depot
                      </div>
                      <p className="mt-2 text-sm text-muted-foreground">
                        {route.depotName ?? "Depot information available in route detail"}
                      </p>
                    </div>

                    <div className="rounded-2xl border border-border/60 bg-background/50 p-4">
                      <div className="flex items-center gap-2 text-sm font-medium text-foreground">
                        {route.status === "IN_PROGRESS" ? (
                          <Play className="size-4" aria-hidden />
                        ) : (
                          <Send className="size-4" aria-hidden />
                        )}
                        Dispatch state
                      </div>
                      <p className="mt-2 text-sm text-muted-foreground">
                        {route.dispatchedAt
                          ? `Dispatched ${new Date(route.dispatchedAt).toLocaleString(undefined, {
                              dateStyle: "medium",
                              timeStyle: "short",
                            })}`
                          : "Waiting for dispatch"}
                      </p>
                    </div>
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}
