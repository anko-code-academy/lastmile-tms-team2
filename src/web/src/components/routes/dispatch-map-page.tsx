"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import {
  CalendarDays,
  MapPinned,
  Route as RouteIcon,
} from "lucide-react";

import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { DatePicker } from "@/components/form/date-picker";
import { ListPageHeader } from "@/components/list";
import { buttonVariants } from "@/components/ui/button";
import {
  DISPATCH_MAP_ROUTE_STATUS_COLORS,
  formatDispatchMapDateYmd,
  routeGeometryHint,
} from "@/lib/routes/dispatch-map";
import {
  ROUTE_STATUS_LABELS,
  ROUTE_STATUS_ORDER,
  routeStatusBadgeClass,
} from "@/lib/labels/routes";
import { getErrorMessage } from "@/lib/network/error-message";
import { cn } from "@/lib/utils";
import { useDispatchMapRoutes } from "@/queries/routes";
import { DispatchMapCanvas } from "@/components/routes/dispatch-map-canvas";

export default function DispatchMapPage() {
  const [selectedDate, setSelectedDate] = useState(() => formatDispatchMapDateYmd());
  const [preferredRouteId, setPreferredRouteId] = useState<string | null>(null);
  const { data = [], isLoading, error } = useDispatchMapRoutes(selectedDate);

  const selectedRouteId = useMemo(() => {
    if (data.length === 0) {
      return null;
    }

    if (preferredRouteId && data.some((route) => route.id === preferredRouteId)) {
      return preferredRouteId;
    }

    return data[0].id;
  }, [data, preferredRouteId]);

  const selectedRoute = useMemo(
    () => data.find((route) => route.id === selectedRouteId) ?? null,
    [data, selectedRouteId],
  );

  const statusCounts = useMemo(
    () =>
      ROUTE_STATUS_ORDER.map((status) => ({
        status,
        label: ROUTE_STATUS_LABELS[status],
        color: DISPATCH_MAP_ROUTE_STATUS_COLORS[status],
        count: data.filter((route) => route.status === status).length,
      })),
    [data],
  );

  return (
    <>
      <ListPageHeader
        variant="route"
        eyebrow="Dispatch"
        title="Dispatch Map"
        description="Daily route coverage on one interactive map, with stop markers and route status coloring for the selected service date."
        icon={<MapPinned strokeWidth={1.75} aria-hidden />}
        action={
          <Link
            href="/routes"
            className={cn(buttonVariants({ variant: "outline", size: "default" }), "gap-2")}
          >
            <RouteIcon className="size-4" aria-hidden />
            Open routes
          </Link>
        }
      />

      {error ? (
        <QueryErrorAlert
          title="Could not load dispatch map routes"
          message={getErrorMessage(error)}
        />
      ) : (
        <div className="grid gap-5 xl:grid-cols-[22rem_minmax(0,1fr)]">
          <aside className="space-y-5">
            <section className="rounded-2xl border border-border/50 bg-card/85 p-5 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)]">
              <div className="flex items-start gap-3">
                <div className="flex size-11 items-center justify-center rounded-xl bg-violet-500/12 text-violet-800 ring-1 ring-violet-500/15 dark:bg-violet-400/10 dark:text-violet-200 dark:ring-violet-400/20">
                  <CalendarDays className="size-5" strokeWidth={1.75} aria-hidden />
                </div>
                <div className="min-w-0 flex-1">
                  <h2 className="text-lg font-semibold tracking-tight text-foreground">
                    Service date
                  </h2>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {isLoading ? "Loading routes for the selected day." : `${data.length} routes scheduled`}
                  </p>
                </div>
              </div>
              <div className="mt-4">
                <DatePicker
                  value={selectedDate}
                  onChange={(value) => {
                    setSelectedDate(value || formatDispatchMapDateYmd());
                    setPreferredRouteId(null);
                  }}
                  emptyLabel="Select service date"
                />
              </div>
            </section>

            <section className="rounded-2xl border border-border/50 bg-card/85 p-5 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)]">
              <h2 className="text-lg font-semibold tracking-tight text-foreground">
                Route legend
              </h2>
              <div className="mt-4 space-y-3">
                {statusCounts.map((item) => (
                  <div
                    key={item.status}
                    className="flex items-center justify-between gap-3 rounded-xl border border-border/45 bg-background/80 px-3 py-2.5"
                  >
                    <div className="flex items-center gap-3">
                      <span
                        className="block h-2.5 w-8 rounded-full"
                        style={{ backgroundColor: item.color }}
                        aria-hidden
                      />
                      <span className="text-sm font-medium text-foreground">
                        {item.label}
                      </span>
                    </div>
                    <span className="text-sm font-semibold text-muted-foreground">
                      {item.count}
                    </span>
                  </div>
                ))}
              </div>
            </section>

            <section className="rounded-2xl border border-border/50 bg-card/85 p-5 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)]">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold tracking-tight text-foreground">
                    Routes
                  </h2>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Click a route to focus the map.
                  </p>
                </div>
                <span className="rounded-full bg-muted px-2.5 py-1 text-xs font-medium text-muted-foreground">
                  {data.length}
                </span>
              </div>

              <div className="mt-4 space-y-3">
                {isLoading ? (
                  <p className="text-sm text-muted-foreground">
                    Loading routes for this date...
                  </p>
                ) : data.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    No routes are scheduled for this date.
                  </p>
                ) : (
                  data.map((route) => {
                    const hint = routeGeometryHint(route);

                    return (
                      <button
                        key={route.id}
                        type="button"
                        aria-pressed={route.id === selectedRouteId}
                        onClick={() => setPreferredRouteId(route.id)}
                        className={cn(
                          "w-full rounded-2xl border px-4 py-3 text-left transition-colors",
                          route.id === selectedRouteId
                            ? "border-violet-500/40 bg-violet-500/8 shadow-sm"
                            : "border-border/45 bg-background/80 hover:border-border hover:bg-muted/30",
                        )}
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="min-w-0">
                            <p className="truncate text-sm font-semibold text-foreground">
                              {route.vehiclePlate}
                            </p>
                            <p className="mt-1 truncate text-sm text-muted-foreground">
                              {route.driverName}
                            </p>
                          </div>
                          <span className={routeStatusBadgeClass(route.status)}>
                            {ROUTE_STATUS_LABELS[route.status]}
                          </span>
                        </div>
                        <div className="mt-3 space-y-1 text-sm text-muted-foreground">
                          <p>{route.zoneName}</p>
                          <p>{new Date(route.startDate).toLocaleString()}</p>
                          <p>
                            {route.estimatedStopCount} stop{route.estimatedStopCount === 1 ? "" : "s"}
                            {" · "}
                            {route.parcelCount} parcel{route.parcelCount === 1 ? "" : "s"}
                          </p>
                          {hint ? (
                            <p className="font-medium text-amber-700 dark:text-amber-300">
                              {hint}
                            </p>
                          ) : null}
                        </div>
                      </button>
                    );
                  })
                )}
              </div>
            </section>
          </aside>

          <section className="space-y-5">
            <div className="rounded-2xl border border-border/50 bg-card/85 p-5 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)]">
              <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold tracking-tight text-foreground">
                    Daily route coverage
                  </h2>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Routes, route stops, and parcel summaries for the selected service day.
                  </p>
                </div>
                {selectedRoute ? (
                  <Link
                    href={`/routes/${selectedRoute.id}`}
                    className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                  >
                    View route
                  </Link>
                ) : null}
              </div>

              {selectedRoute ? (
                <div className="mb-4 rounded-2xl border border-border/45 bg-background/80 px-4 py-3">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    Selected route
                  </p>
                  <div className="mt-2 flex flex-wrap items-center gap-3">
                    <p className="text-base font-semibold text-foreground">
                      {selectedRoute.vehiclePlate}
                    </p>
                    <span className={routeStatusBadgeClass(selectedRoute.status)}>
                      {ROUTE_STATUS_LABELS[selectedRoute.status]}
                    </span>
                    <p className="text-sm text-muted-foreground">
                      {selectedRoute.driverName} · {selectedRoute.zoneName}
                    </p>
                  </div>
                </div>
              ) : null}

              <DispatchMapCanvas
                routes={data}
                selectedRouteId={selectedRouteId}
                onSelectRoute={setPreferredRouteId}
              />
            </div>
          </section>
        </div>
      )}
    </>
  );
}
