"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Plus } from "lucide-react";
import { useSession } from "next-auth/react";

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
} from "@/components/list";
import { DeleteVehicleDialog } from "@/components/vehicles/delete-vehicle-dialog";
import { VehicleRowActions } from "@/components/vehicles/vehicle-row-actions";
import { buttonVariants } from "@/components/ui/button";
import { getErrorMessage } from "@/lib/network/error-message";
import {
  VEHICLE_STATUS_LABELS,
  VEHICLE_TYPE_LABELS,
  vehicleStatusBadgeClass,
} from "@/lib/labels/vehicles";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { useVehicles, useDeleteVehicle } from "@/queries/vehicles";
import { useDepots } from "@/queries/depots";
import { VehicleStatus } from "@/types/vehicles";
import { DepotFilterDropdown } from "@/components/vehicles/depot-filter-dropdown";
import { VehicleStatusFilter } from "@/components/vehicles/status-filter-dropdown";
import { OverflowTooltipCell } from "@/components/list/overflow-tooltip-cell";
import { cn } from "@/lib/utils";

const PAGE_SIZE = 20;

export default function VehiclesPage() {
  const { status: sessionStatus } = useSession();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<VehicleStatus | undefined>();
  const [depotFilter, setDepotFilter] = useState<string | undefined>();
  const [deleteTarget, setDeleteTarget] = useState<{
    id: string;
    plate: string;
  } | null>(null);

  const { data: depotsData } = useDepots();
  const { data = [], isLoading, error } = useVehicles({
    status: statusFilter,
    depotId: depotFilter,
  });

  const deleteVehicle = useDeleteVehicle();

  const total = data.length;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const from = total === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const to = total === 0 ? 0 : Math.min(page * PAGE_SIZE, total);
  const pagedItems = useMemo(
    () => data.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [data, page],
  );

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await deleteVehicle.mutateAsync({
        id: deleteTarget.id,
        registrationPlate: deleteTarget.plate,
      });
      setDeleteTarget(null);
    } catch {
      /* error toast from global MutationCache */
    }
  };

  if (sessionStatus === "loading" || isLoading) return <ListPageLoading />;
  if (error)
    return (
      <QueryErrorAlert
        title="Could not load vehicles"
        message={getErrorMessage(error)}
      />
    );

  return (
    <>
      <DeleteVehicleDialog
        open={deleteTarget !== null}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        registrationPlate={deleteTarget?.plate ?? ""}
        onConfirm={confirmDelete}
        isPending={deleteVehicle.isPending}
      />

      <ListPageHeader
        title="Vehicles"
        description="Fleet registry: plates, capacity, depot, and utilization. Open a vehicle for route history and edits."
        action={
          <Link
            href="/vehicles/new"
            className={cn(
              buttonVariants({ size: "default" }),
              "shrink-0 gap-2 self-start sm:self-auto",
            )}
          >
            <Plus className="size-4" aria-hidden />
            Add vehicle
          </Link>
        }
      />

      <ListPageStatsStrip
        totalLabel="Total in fleet"
        totalCount={total}
        rangeEntityLabel="vehicles"
        from={from}
        to={to}
        page={page}
        totalPages={totalPages}
        pageSize={PAGE_SIZE}
        filterCardLabel="Status filter"
        filterCardHint="Use the filter below to narrow the list"
        activeFilterDisplay={
          statusFilter !== undefined
            ? VEHICLE_STATUS_LABELS[statusFilter]
            : "All statuses"
        }
      />

      <div className="list-page-filters-animate mb-6 flex gap-3">
        <VehicleStatusFilter
          value={statusFilter}
          onChange={(value) => {
            setStatusFilter(value);
            setPage(1);
          }}
        />
        <DepotFilterDropdown
          value={depotFilter}
          onChange={(value) => {
            setDepotFilter(value);
            setPage(1);
          }}
          depots={depotsData}
        />
      </div>

      <ListDataTable minWidthClassName="min-w-[760px]">
        <thead>
          <tr className={listDataTableHeadRowClass}>
            <th className={listDataTableThClass}>Plate</th>
            <th className={listDataTableThClass}>Type</th>
            <th className={listDataTableThClass}>Capacity</th>
            <th className={listDataTableThClass}>Status</th>
            <th className={cn(listDataTableThClass, "max-w-[160px]")}>
              Depot
            </th>
            <th className={cn(listDataTableThClass, "whitespace-nowrap")}>
              Routes
            </th>
            <th className={cn(listDataTableThClass, "whitespace-nowrap")}>
              km
            </th>
            <th className={listDataTableThRightClass}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {pagedItems.map((vehicle) => (
            <tr key={vehicle.id} className={listDataTableBodyRowClass}>
              <td className={cn(listDataTableTdClass, "max-w-[200px]")}>
                <OverflowTooltipCell
                  fullText={vehicle.registrationPlate}
                  className="font-medium"
                />
              </td>
              <td className={cn(listDataTableTdClass, "max-w-[100px]")}>
                <OverflowTooltipCell
                  fullText={VEHICLE_TYPE_LABELS[vehicle.type]}
                />
              </td>
              <td className={cn(listDataTableTdClass, "max-w-[140px] tabular-nums")}>
                <OverflowTooltipCell
                  fullText={`${vehicle.parcelCapacity} / ${vehicle.weightCapacity} kg`}
                />
              </td>
              <td className={cn(listDataTableTdClass, "max-w-[160px] align-middle")}>
                <OverflowTooltipCell
                  shrinkToContent
                  fullText={VEHICLE_STATUS_LABELS[vehicle.status]}
                  className={vehicleStatusBadgeClass(vehicle.status)}
                />
              </td>
              <td className={cn(listDataTableTdClass, "max-w-[160px] text-muted-foreground")}>
                <OverflowTooltipCell fullText={vehicle.depotName || "вЂ"} />
              </td>
              <td className={cn(listDataTableTdClass, "max-w-[96px] tabular-nums")}>
                <OverflowTooltipCell
                  fullText={String(
                    vehicle.totalRoutes ?? vehicle.routesCompleted ?? 0,
                  )}
                />
              </td>
              <td className={cn(listDataTableTdClass, "max-w-[120px] tabular-nums")}>
                <OverflowTooltipCell
                  fullText={(vehicle.totalMileage ?? 0).toLocaleString()}
                />
              </td>
              <td className={cn(listDataTableTdClass, "min-w-32 text-right align-middle")}>
                <VehicleRowActions
                  vehicleId={vehicle.id}
                  registrationPlate={vehicle.registrationPlate}
                  onDelete={() =>
                    setDeleteTarget({
                      id: vehicle.id,
                      plate: vehicle.registrationPlate,
                    })
                  }
                  deleteDisabled={deleteVehicle.isPending}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </ListDataTable>

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
