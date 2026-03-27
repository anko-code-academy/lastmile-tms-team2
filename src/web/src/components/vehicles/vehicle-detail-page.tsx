"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import {
  ArrowLeft,
  CircleCheck,
  Gauge,
  Package,
  Pencil,
  Route,
  Eye,
  Truck,
} from "lucide-react";
import { useSession } from "next-auth/react";
import { Tooltip } from "radix-ui";

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
import {
  ListDataTable,
  listDataTableBodyRowClass,
  listDataTableHeadRowClass,
  listDataTableTdClass,
  listDataTableThClass,
  listDataTableThRightClass,
  listTableIconLinkClass,
} from "@/components/list";
import { buttonVariants } from "@/components/ui/button";
import { OverflowTooltipCell } from "@/components/list/overflow-tooltip-cell";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { cn } from "@/lib/utils";
import { getErrorMessage } from "@/lib/network/error-message";
import {
  ROUTE_STATUS_LABELS,
  routeStatusBadgeClass as routeRowStatusBadgeClass,
} from "@/lib/labels/routes";
import {
  VEHICLE_STATUS_LABELS,
  VEHICLE_TYPE_LABELS,
  vehicleStatusBadgeClass,
} from "@/lib/labels/vehicles";
import { useVehicle } from "@/queries/vehicles";
import { useRoutes } from "@/queries/routes";

function VehicleRouteHistory({ vehicleId }: { vehicleId: string }) {
  const { data: routes = [], isLoading: historyLoading } = useRoutes({
    vehicleId,
  });

  return (
    <DetailPanel
      className="detail-panel-animate"
      title="Route history"
      description="Recent routes for this vehicle. Open a row to see full details."
    >
      {historyLoading && (
        <p className="text-sm text-muted-foreground">Loading routes…</p>
      )}
      {!historyLoading && routes.length === 0 && (
        <p className="text-sm text-muted-foreground">
          No routes recorded for this vehicle yet.
        </p>
      )}
      {!historyLoading && routes.length > 0 && (
        <ListDataTable
          minWidthClassName="min-w-[720px]"
          className="bg-background/50"
        >
          <thead>
            <tr className={listDataTableHeadRowClass}>
              <th
                className={cn(
                  listDataTableThClass,
                  "min-w-[140px] whitespace-nowrap",
                )}
              >
                Start date
              </th>
              <th className={listDataTableThClass}>Driver</th>
              <th className={listDataTableThClass}>Status</th>
              <th className={cn(listDataTableThClass, "whitespace-nowrap")}>
                Mileage
              </th>
              <th className={cn(listDataTableThClass, "whitespace-nowrap")}>
                Parcels
              </th>
              <th className={listDataTableThRightClass}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {routes.map((route) => (
              <tr key={route.id} className={listDataTableBodyRowClass}>
                <td className={cn(listDataTableTdClass, "tabular-nums text-muted-foreground")}>
                  <OverflowTooltipCell
                    fullText={new Date(route.startDate).toLocaleString()}
                  />
                </td>
                <td className={cn(listDataTableTdClass, "max-w-[200px]")}>
                  <OverflowTooltipCell fullText={route.driverName} />
                </td>
                <td className={cn(listDataTableTdClass, "max-w-[140px] align-middle")}>
                  <OverflowTooltipCell
                    shrinkToContent
                    fullText={ROUTE_STATUS_LABELS[route.status]}
                    className={routeRowStatusBadgeClass(route.status)}
                  />
                </td>
                <td className={cn(listDataTableTdClass, "tabular-nums")}>
                  {route.totalMileage > 0
                    ? `${route.totalMileage} km`
                    : `${route.startMileage} (start)`}
                </td>
                <td className={cn(listDataTableTdClass, "tabular-nums")}>
                  {route.parcelsDelivered}/{route.parcelCount}
                </td>
                <td className={cn(listDataTableTdClass, "min-w-32 text-right align-middle")}>
                  <div className="flex justify-end">
                    <Tooltip.Root>
                      <Tooltip.Trigger asChild>
                        <Link
                          href={`/routes/${route.id}`}
                          className={listTableIconLinkClass}
                          aria-label={`View route on ${new Date(route.startDate).toLocaleDateString()}`}
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
      )}
    </DetailPanel>
  );
}

export default function VehicleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { status: sessionStatus } = useSession();

  const { data: vehicle, isLoading, error } = useVehicle(id);

  if (sessionStatus === "loading" || isLoading)
    return <DetailPageSkeleton variant="vehicle" />;
  if (error)
    return (
      <DetailShell variant="vehicle">
        <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
          <QueryErrorAlert
            title="Could not load vehicle"
            message={getErrorMessage(error)}
          />
        </DetailContainer>
      </DetailShell>
    );
  if (!vehicle)
    return (
      <DetailShell variant="vehicle">
        <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
          <DetailBreadcrumb
            variant="vehicle"
            items={[{ label: "Vehicles", href: "/vehicles" }, { label: "Not found" }]}
          />
          <DetailEmptyState
            title="Vehicle not found"
            message="This vehicle may have been removed or the link is incorrect."
          />
        </DetailContainer>
      </DetailShell>
    );

  return (
    <DetailShell variant="vehicle">
      <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
        <DetailPageSectionProvider section="vehicle">
          <DetailBreadcrumb
            variant="vehicle"
            items={[
              { label: "Vehicles", href: "/vehicles" },
              { label: vehicle.registrationPlate },
            ]}
          />

          <DetailHero
            section="vehicle"
            eyebrow="Fleet"
            icon={<Truck strokeWidth={1.75} />}
            title={vehicle.registrationPlate}
            subtitle={
              <>
                {VEHICLE_TYPE_LABELS[vehicle.type]}
                {vehicle.depotName ? (
                  <>
                    {" · "}
                    <span className="text-foreground/80">{vehicle.depotName}</span>
                  </>
                ) : null}
              </>
            }
            badge={
              <span className={vehicleStatusBadgeClass(vehicle.status)}>
                {VEHICLE_STATUS_LABELS[vehicle.status]}
              </span>
            }
            actions={
              <>
                <Link
                  href={`/vehicles/${id}/edit`}
                  className={cn(buttonVariants({ variant: "default", size: "sm" }))}
                >
                  <Pencil className="mr-2 size-4" aria-hidden />
                  Edit
                </Link>
                <Link
                  href="/vehicles"
                  className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                >
                  <ArrowLeft className="mr-2 size-4" aria-hidden />
                  All vehicles
                </Link>
              </>
            }
          />

          <DetailMetricStrip
            items={[
              {
                label: "Total routes",
                value: (vehicle.totalRoutes ?? 0).toLocaleString(),
                icon: <Route className="size-5" aria-hidden />,
              },
              {
                label: "Completed",
                value: (vehicle.routesCompleted ?? 0).toLocaleString(),
                icon: <CircleCheck className="size-5" aria-hidden />,
              },
              {
                label: "Total mileage",
                value: `${(vehicle.totalMileage ?? 0).toLocaleString()} km`,
                icon: <Gauge className="size-5" aria-hidden />,
              },
              {
                label: "Parcel capacity",
                value: `${vehicle.parcelCapacity}`,
                hint: `${vehicle.weightCapacity} kg max load`,
                icon: <Package className="size-5" aria-hidden />,
              },
            ]}
          />

          <DetailPanel
            className="detail-panel-animate"
            section="vehicle"
            title="Vehicle details"
            description="Identifiers, depot assignment, and audit timestamps."
          >
            <DetailFieldGrid>
              <DetailField label="Type">{VEHICLE_TYPE_LABELS[vehicle.type]}</DetailField>
              <DetailField label="Status">
                <span className={vehicleStatusBadgeClass(vehicle.status)}>
                  {VEHICLE_STATUS_LABELS[vehicle.status]}
                </span>
              </DetailField>
              <DetailField label="Depot">
                {vehicle.depotName || vehicle.depotId}
              </DetailField>
              <DetailField label="Parcel capacity">
                {vehicle.parcelCapacity} parcels
              </DetailField>
              <DetailField label="Weight capacity">
                {vehicle.weightCapacity} kg
              </DetailField>
              <DetailField label="Created">
                {new Date(vehicle.createdAt).toLocaleString()}
              </DetailField>
              {vehicle.lastModifiedAt && (
                <DetailField label="Last modified">
                  {new Date(vehicle.lastModifiedAt).toLocaleString()}
                </DetailField>
              )}
            </DetailFieldGrid>
          </DetailPanel>

          <VehicleRouteHistory key={id} vehicleId={id} />
        </DetailPageSectionProvider>
      </DetailContainer>
    </DetailShell>
  );
}
