"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Eye, Plus, Route as RouteIcon } from "lucide-react";
import { useSession } from "next-auth/react";
import { Tooltip } from "radix-ui";

import {
  ListDataTable,
  ListPageHeader,
  ListPageLoading,
  ListPagePagination,
  ListPageStatsStrip,
  listDataTableBodyRowClass,
  listDataTableHeadRowClass,
  listDataTableTdClass,
  listDataTableThClass,
  listDataTableThRightClass,
  listTableIconLinkClass,
} from "@/components/list";
import { buttonVariants } from "@/components/ui/button";
import { OverflowTooltipCell } from "@/components/list/overflow-tooltip-cell";
import { RouteStatusFilter } from "@/components/routes/route-status-filter";
import { getErrorMessage } from "@/lib/network/error-message";
import {
  ROUTE_STATUS_LABELS,
  routeStatusBadgeClass,
} from "@/lib/labels/routes";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { cn } from "@/lib/utils";
import { useRoutes } from "@/queries/routes";
import { RouteStatus } from "@/types/routes";

const PAGE_SIZE = 20;

export default function RoutesPage() {
  const { status: sessionStatus } = useSession();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<RouteStatus | undefined>();

  const { data = [], isLoading, error } = useRoutes({
    status: statusFilter,
  });

  if (sessionStatus === "loading" || isLoading) return <ListPageLoading />;
  if (error)
    return (
      <QueryErrorAlert
        title="Could not load routes"
        message={getErrorMessage(error)}
      />
    );

  const total = data.length;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const from = total === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const to = total === 0 ? 0 : Math.min(page * PAGE_SIZE, total);
  const pagedItems = useMemo(
    () => data.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [data, page],
  );

  return (
    <>
      <ListPageHeader
        variant="route"
        eyebrow="Dispatch"
        title="Routes"
        description="Planned and active delivery runs: vehicle, driver, mileage, and parcel progress. Open a route for full detail."
        icon={<RouteIcon strokeWidth={1.75} aria-hidden />}
        action={
          <Link
            href="/routes/new"
            className={cn(
              buttonVariants({ size: "default" }),
              "shrink-0 gap-2 self-start sm:self-auto",
            )}
          >
            <Plus className="size-4" aria-hidden />
            New route
          </Link>
        }
      />

      <ListPageStatsStrip
        totalLabel="Total routes"
        totalCount={total}
        rangeEntityLabel="in list"
        from={from}
        to={to}
        page={page}
        totalPages={totalPages}
        pageSize={PAGE_SIZE}
        filterCardLabel="Status filter"
        filterCardHint="Filter applies to this table only"
        activeFilterDisplay={
          statusFilter !== undefined
            ? ROUTE_STATUS_LABELS[statusFilter]
            : "All statuses"
        }
      />

      <div className="list-page-filters-animate mb-6">
        <RouteStatusFilter
          value={statusFilter}
          onChange={(value) => {
            setStatusFilter(value);
            setPage(1);
          }}
        />
      </div>

      <ListDataTable minWidthClassName="min-w-[880px]">
        <thead>
          <tr className={listDataTableHeadRowClass}>
            <th className={listDataTableThClass}>Vehicle</th>
            <th className={listDataTableThClass}>Driver</th>
            <th
              className={cn(
                listDataTableThClass,
                "min-w-[160px] whitespace-nowrap",
              )}
            >
              Start Date
            </th>
            <th className={cn(listDataTableThClass, "whitespace-nowrap")}>
              Mileage
            </th>
            <th className={cn(listDataTableThClass, "whitespace-nowrap")}>
              Parcels
            </th>
            <th className={listDataTableThClass}>Status</th>
            <th className={listDataTableThRightClass}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {pagedItems.map((route) => (
            <tr key={route.id} className={listDataTableBodyRowClass}>
              <td className={cn(listDataTableTdClass, "max-w-[180px]")}>
                <OverflowTooltipCell
                  fullText={route.vehiclePlate}
                  className="font-medium"
                >
                  <Link
                    href={`/vehicles/${route.vehicleId}`}
                    className="text-primary underline-offset-4 hover:underline"
                  >
                    {route.vehiclePlate}
                  </Link>
                </OverflowTooltipCell>
              </td>
              <td className={cn(listDataTableTdClass, "max-w-[200px]")}>
                <OverflowTooltipCell fullText={route.driverName} />
              </td>
              <td className={cn(listDataTableTdClass, "tabular-nums text-muted-foreground")}>
                {new Date(route.startDate).toLocaleString()}
              </td>
              <td className={cn(listDataTableTdClass, "tabular-nums")}>
                {route.totalMileage > 0
                  ? `${route.totalMileage} km`
                  : `${route.startMileage} (start)`}
              </td>
              <td className={cn(listDataTableTdClass, "tabular-nums")}>
                {route.parcelsDelivered}/{route.parcelCount}
              </td>
              <td className={cn(listDataTableTdClass, "max-w-[160px] align-middle")}>
                <OverflowTooltipCell
                  shrinkToContent
                  fullText={ROUTE_STATUS_LABELS[route.status]}
                  className={routeStatusBadgeClass(route.status)}
                />
              </td>
              <td className={cn(listDataTableTdClass, "min-w-32 text-right align-middle")}>
                <div className="flex justify-end">
                  <Tooltip.Root>
                    <Tooltip.Trigger asChild>
                      <Link
                        href={`/routes/${route.id}`}
                        className={listTableIconLinkClass}
                        aria-label={`View route ${route.vehiclePlate}`}
                      >
                        <Eye className="size-3.5" strokeWidth={2} aria-hidden />
                      </Link>
                    </Tooltip.Trigger>
                    <Tooltip.Portal>
                      <Tooltip.Content
                        side="top"
                        sideOffset={4}
                        className="z-50 rounded-md border border-border bg-popover px-2 py-1 text-xs text-popover-foreground shadow-md"
                      >
                        View
                        <Tooltip.Arrow className="fill-popover" />
                      </Tooltip.Content>
                    </Tooltip.Portal>
                  </Tooltip.Root>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </ListDataTable>

      {data.length === 0 && (
        <p className="py-12 text-center text-muted-foreground">
          No routes yet.{" "}
          <Link
            href="/routes/new"
            className="text-primary underline-offset-4 hover:underline"
          >
            Create your first route
          </Link>
        </p>
      )}

      {totalPages > 1 && (
        <ListPagePagination
          page={page}
          totalPages={totalPages}
          setPage={setPage}
        />
      )}
    </>
  );
}
