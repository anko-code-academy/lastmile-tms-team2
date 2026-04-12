"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { ArrowUpDown, ArrowUp, ArrowDown, FileText, MapPin, Package, PackagePlus, Printer, Route, ScanSearch, Search, X } from "lucide-react";
import { useSession } from "next-auth/react";

import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
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
import { OverflowTooltipCell } from "@/components/list/overflow-tooltip-cell";
import { CancelParcelDialog } from "@/components/parcels/cancel-parcel-dialog";
import { ParcelRowActions } from "@/components/parcels/parcel-row-actions";
import { ParcelStatusFilter } from "@/components/parcels/status-filter";
import { ParcelTypeFilter } from "@/components/parcels/type-filter";
import { ParcelZoneFilter } from "@/components/parcels/zone-filter";
import { buttonVariants, Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  canManageParcelBeforeLoad,
  formatParcelServiceType,
  formatParcelStatus,
  parcelStatusBadgeClass,
} from "@/lib/labels/parcels";
import {
  getParcelDetailPath,
  getParcelInboundPath,
  getParcelSortPath,
  getParcelStagingPath,
} from "@/lib/parcels/paths";
import { getErrorMessage } from "@/lib/network/error-message";
import { appToast } from "@/lib/toast/app-toast";
import { useDebounce } from "@/hooks/use-debounce";
import { cn } from "@/lib/utils";
import type { ParcelStatus } from "@/graphql/generated";
import {
  useAvailableParcelTypes,
  useCancelParcel,
  usePreLoadParcelsPage,
} from "@/queries/parcels";
import { useZones } from "@/queries/zones";
import { parcelsService } from "@/services/parcels.service";
import { ParcelImportHistoryTable } from "./parcel-import-history-table";
import { ParcelImportPanel } from "./parcel-import-panel";
import { ParcelRegistrationForm } from "./parcel-registration-form";

type PendingCancellation = {
  id: string;
  trackingNumber: string;
} | null;

export default function ParcelsPage() {
  const { status: sessionStatus } = useSession();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<ParcelStatus | undefined>();
  const [zoneFilter, setZoneFilter] = useState<string | undefined>();
  const [typeFilter, setTypeFilter] = useState<string | undefined>();
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [sortField, setSortField] = useState<string | undefined>();
  const [sortDirection, setSortDirection] = useState<"ASC" | "DESC">("ASC");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [pageCursors, setPageCursors] = useState<Record<number, string | null>>({
    1: null,
  });

  const debouncedSearch = useDebounce(search, 300);
  const { data: zones = [] } = useZones();
  const { data: allParcelsForTypes = [] } = useAvailableParcelTypes();
  const currentPageCursor = pageCursors[page] ?? null;

  const { data, isLoading, error } = usePreLoadParcelsPage(
    debouncedSearch || undefined,
    (statusFilter !== undefined || zoneFilter !== undefined || typeFilter !== undefined || dateFrom !== "" || dateTo !== "")
      ? {
          ...(statusFilter !== undefined ? { status: { in: [statusFilter] } } : {}),
          ...(zoneFilter !== undefined ? { zoneId: { eq: zoneFilter } } : {}),
          ...(typeFilter !== undefined ? { parcelType: { eq: typeFilter } } : {}),
          ...(dateFrom !== "" || dateTo !== ""
            ? {
                estimatedDeliveryDate: {
                  gte: dateFrom !== "" ? new Date(`${dateFrom}T00:00:00Z`).toISOString() : undefined,
                  lte: dateTo !== "" ? new Date(`${dateTo}T23:59:59Z`).toISOString() : undefined,
                },
              }
            : {}),
        }
      : undefined,
    sortField
      ? [
          {
            ...(sortField === "TrackingNumber" && { trackingNumber: sortDirection }),
            ...(sortField === "Status" && { status: sortDirection }),
            ...(sortField === "ParcelType" && { parcelType: sortDirection }),
            ...(sortField === "Weight" && { weight: sortDirection }),
            ...(sortField === "CreatedAt" && { createdAt: sortDirection }),
            ...(sortField === "EstimatedDeliveryDate" && { estimatedDeliveryDate: sortDirection }),
            ...(sortField === "RecipientContactName" && { recipientContactName: { contactName: sortDirection } }),
            ...(sortField === "ZoneName" && { zoneName: { name: sortDirection } }),
          },
        ]
      : undefined,
    pageSize,
    currentPageCursor,
  );

  const parcels = useMemo(() => data?.nodes ?? [], [data?.nodes]);
  const total = data?.totalCount ?? 0;
  const pageInfo = data?.pageInfo ?? {
    hasNextPage: false,
    hasPreviousPage: false,
    startCursor: null,
    endCursor: null,
  };
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = total === 0 ? 0 : Math.min((page - 1) * pageSize + parcels.length, total);

  useEffect(() => {
    setPage(1);
    setPageCursors({ 1: null });
  }, [
    debouncedSearch,
    statusFilter,
    zoneFilter,
    typeFilter,
    dateFrom,
    dateTo,
    sortField,
    sortDirection,
    pageSize,
  ]);

  useEffect(() => {
    if (!pageInfo.hasNextPage || !pageInfo.endCursor) {
      return;
    }

    setPageCursors((current) =>
      current[page + 1] === pageInfo.endCursor
        ? current
        : {
            ...current,
            [page + 1]: pageInfo.endCursor,
          },
    );
  }, [page, pageInfo.endCursor, pageInfo.hasNextPage]);

  function handleSort(field: string) {
    if (sortField === field) {
      setSortDirection(sortDirection === "ASC" ? "DESC" : "ASC");
    } else {
      setSortField(field);
      setSortDirection("ASC");
    }
  }

  function SortableTh({ field, label }: { field: string; label: string }) {
    const isActive = sortField === field;
    return (
      <th
        className={cn(listDataTableThClass, "cursor-pointer select-none hover:text-foreground")}
        onClick={() => handleSort(field)}
        aria-sort={isActive ? (sortDirection === "ASC" ? "ascending" : "descending") : "none"}
      >
        <span className="inline-flex items-center gap-1">
          {label}
          {isActive ? (
            sortDirection === "ASC" ? (
              <ArrowUp className="h-3.5 w-3.5" aria-hidden />
            ) : (
              <ArrowDown className="h-3.5 w-3.5" aria-hidden />
            )
          ) : (
            <ArrowUpDown className="h-3.5 w-3.5 text-muted-foreground/50" aria-hidden />
          )}
        </span>
      </th>
    );
  }
  const cancelParcel = useCancelParcel();

  const hasActiveFilters =
    statusFilter !== undefined ||
    zoneFilter !== undefined ||
    typeFilter !== undefined ||
    dateFrom !== "" ||
    dateTo !== "";

  function clearFilters() {
    setStatusFilter(undefined);
    setZoneFilter(undefined);
    setTypeFilter(undefined);
    setDateFrom("");
    setDateTo("");
  }

  const [showForm, setShowForm] = useState(false);
  const [selectedImportId, setSelectedImportId] = useState<string | null>(null);
  const [selectedParcelIds, setSelectedParcelIds] = useState<string[]>([]);
  const [isDownloading, setIsDownloading] = useState<false | "zpl" | "pdf">(
    false,
  );
  const [pendingCancellation, setPendingCancellation] =
    useState<PendingCancellation>(null);

  const allVisibleSelected =
    parcels.length > 0 &&
    parcels.every((parcel) => selectedParcelIds.includes(parcel.id));

  const selectedTrackingNumbers = useMemo(
    () =>
      parcels
        .filter((parcel) => selectedParcelIds.includes(parcel.id))
        .map((parcel) => parcel.trackingNumber),
    [parcels, selectedParcelIds],
  );

  function toggleParcelSelection(parcelId: string) {
    setSelectedParcelIds((current) =>
      current.includes(parcelId)
        ? current.filter((selectedId) => selectedId !== parcelId)
        : [...current, parcelId],
    );
  }

  function toggleSelectAllVisible(checked: boolean) {
    setSelectedParcelIds(checked ? parcels.map((parcel) => parcel.id) : []);
  }

  async function handleBulkDownload(format: "zpl" | "pdf") {
    if (selectedParcelIds.length === 0) {
      appToast.error("Select at least one parcel to download labels.");
      return;
    }

    setIsDownloading(format);

    try {
      await parcelsService.downloadBulkLabels(selectedParcelIds, format);
    } catch (downloadError) {
      appToast.errorFromUnknown(downloadError);
    } finally {
      setIsDownloading(false);
    }
  }

  async function handleCancelParcel(reason: string) {
    if (!pendingCancellation) {
      return;
    }

    await cancelParcel.mutateAsync({
      id: pendingCancellation.id,
      reason,
    });

    setSelectedParcelIds((current) =>
      current.filter((parcelId) => parcelId !== pendingCancellation.id),
    );
    setPendingCancellation(null);
  }

  if (sessionStatus === "loading" || isLoading) {
    return <ListPageLoading />;
  }

  if (error) {
    return (
      <QueryErrorAlert
        title="Could not load parcels"
        message={getErrorMessage(error)}
      />
    );
  }

  if (showForm) {
    return (
      <>
        <ListPageHeader
          title="Register Parcel"
          description="Enter sender, recipient, and parcel details to register a new shipment."
          icon={<PackagePlus strokeWidth={1.75} aria-hidden />}
          action={
            <Button variant="outline" onClick={() => setShowForm(false)}>
              Cancel
            </Button>
          }
        />
        <ParcelRegistrationForm
          onCancel={() => setShowForm(false)}
          onViewQueue={() => setShowForm(false)}
        />
      </>
    );
  }

  return (
    <>
      <ListPageHeader
        title="Parcels"
        description="Track parcels from registration through delivery from one list. Imports, staging, and label printing stay here, while edit and cancellation remain available only before load-out."
        icon={<Package strokeWidth={1.75} aria-hidden />}
        action={
          <>
            <Link
              href={getParcelInboundPath()}
              className={cn(buttonVariants({ variant: "outline" }))}
            >
              <ScanSearch className="h-4 w-4" aria-hidden />
              Inbound Receiving
            </Link>
            <Link
              href={getParcelSortPath()}
              className={cn(buttonVariants({ variant: "outline" }))}
            >
              <MapPin className="h-4 w-4" aria-hidden />
              Sort &amp; zone
            </Link>
            <Link
              href={getParcelStagingPath()}
              className={cn(buttonVariants({ variant: "outline" }))}
            >
              <Route className="h-4 w-4" aria-hidden />
              Route Staging
            </Link>
            <Button
              variant="outline"
              onClick={() => void handleBulkDownload("zpl")}
              disabled={selectedParcelIds.length === 0 || isDownloading !== false}
            >
              <Printer className="h-4 w-4" aria-hidden />
              {isDownloading === "zpl" ? "Preparing ZPL..." : "Download 4x6 ZPL"}
            </Button>
            <Button
              variant="outline"
              onClick={() => void handleBulkDownload("pdf")}
              disabled={selectedParcelIds.length === 0 || isDownloading !== false}
            >
              <FileText className="h-4 w-4" aria-hidden />
              {isDownloading === "pdf" ? "Preparing PDF..." : "Download A4 PDF"}
            </Button>
            <Button onClick={() => setShowForm(true)}>
              <PackagePlus className="h-4 w-4" aria-hidden />
              Register Parcel
            </Button>
          </>
        }
      />

      <div className="space-y-8">
        <ParcelImportPanel
          selectedImportId={selectedImportId}
          onImportSelected={setSelectedImportId}
          showHistory={false}
        />

        <div className="flex flex-col gap-2 rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
          <div className="relative min-w-0">
            <Search className="pointer-events-none absolute left-3 top-2.5 size-4 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search by tracking number, recipient, or address"
              className="pl-9"
            />
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <ParcelStatusFilter
              value={statusFilter}
              onChange={(v) => setStatusFilter(v)}
            />
            <ParcelZoneFilter
              value={zoneFilter}
              onChange={setZoneFilter}
              zones={zones.map((z) => ({ id: z.id, name: z.name ?? "" }))}
            />
            <ParcelTypeFilter
              value={typeFilter}
              onChange={setTypeFilter}
              parcelTypes={allParcelsForTypes.map((p) => p.parcelType).filter(Boolean) as string[]}
            />
            <div className="flex items-center gap-1.5">
              <Label className="text-xs text-muted-foreground whitespace-nowrap">Delivery Date</Label>
              <Input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="w-[148px]"
              />
              <span className="text-muted-foreground text-xs">–</span>
              <Input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="w-[148px]"
              />
            </div>
            {hasActiveFilters && (
              <Button
                variant="outline"
                size="sm"
                onClick={clearFilters}
                className="shrink-0 text-muted-foreground hover:text-foreground"
              >
                <X className="h-3.5 w-3.5 mr-1" aria-hidden />
                Clear filters
              </Button>
            )}
          </div>
        </div>

        {parcels.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-border p-12 text-center">
            <p className="font-medium">
              {hasActiveFilters || debouncedSearch
                ? "No parcels match your filters"
                : "No parcels found"}
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              {hasActiveFilters || debouncedSearch
                ? "Try adjusting your search or filters."
                : "Registered, staged, loaded, out-for-delivery, delivered, and exception parcels will appear here."}
            </p>
          </div>
        ) : (
          <>
            <div className="flex flex-col gap-2 rounded-2xl border border-border/60 bg-card/80 px-5 py-4 text-sm shadow-sm sm:flex-row sm:items-center sm:justify-between">
              <label className="flex items-center gap-3 font-medium text-foreground">
                <input
                  type="checkbox"
                  checked={allVisibleSelected}
                  onChange={(event) =>
                    toggleSelectAllVisible(event.currentTarget.checked)
                  }
                  aria-label="Select all visible parcels"
                  className="h-4 w-4 rounded border-input"
                />
                Select all visible
              </label>
              <div className="flex items-center gap-3">
                <select
                  value={pageSize}
                  onChange={(e) => {
                    setPageSize(Number(e.target.value));
                  }}
                  className="rounded border border-input bg-background px-2 py-1 text-sm"
                  aria-label="Parcels per page"
                >
                  <option value={10}>10 per page</option>
                  <option value={20}>20 per page</option>
                  <option value={50}>50 per page</option>
                  <option value={100}>100 per page</option>
                </select>
                <span className="text-xs text-muted-foreground">
                  {selectedTrackingNumbers.length === 0
                    ? "No parcels selected."
                    : `${selectedTrackingNumbers.length} selected`}
                </span>
              </div>
            </div>

            <ListPageStatsStrip
              totalLabel="Total parcels"
              totalCount={total}
              rangeEntityLabel="parcels"
              from={from}
              to={to}
              page={page}
              totalPages={totalPages}
              pageSize={pageSize}
              filterCardLabel="Active filter"
              filterCardHint="Adjust filters above"
              activeFilterDisplay={
                hasActiveFilters || debouncedSearch
                  ? "Filtered"
                  : "No filters"
              }
            />

            <ListDataTable minWidthClassName="min-w-[1140px]">
              <thead>
                <tr className={listDataTableHeadRowClass}>
                  <th className={cn(listDataTableThClass, "w-14")}>Select</th>
                  <SortableTh field="TrackingNumber" label="Tracking" />
                  <SortableTh field="RecipientContactName" label="Recipient" />
                  <SortableTh field="Weight" label="Weight" />
                  <SortableTh field="ParcelType" label="Type" />
                  <SortableTh field="ZoneName" label="Zone" />
                  <SortableTh field="CreatedAt" label="Created" />
                  <SortableTh field="EstimatedDeliveryDate" label="Delivery Date" />
                  <SortableTh field="Status" label="Status" />
                  <th className={listDataTableThRightClass}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {parcels.map((parcel) => {
                  const canManageBeforeLoad = canManageParcelBeforeLoad(parcel.status ?? "");

                  return (
                    <tr key={parcel.id} className={listDataTableBodyRowClass}>
                      <td className={cn(listDataTableTdClass, "w-14 align-middle")}>
                        <input
                          type="checkbox"
                          checked={selectedParcelIds.includes(parcel.id)}
                          onChange={() => toggleParcelSelection(parcel.id)}
                          aria-label={`Select parcel ${parcel.trackingNumber}`}
                          className="h-4 w-4 rounded border-input"
                        />
                      </td>
                      <td
                        className={cn(
                          listDataTableTdClass,
                          "font-mono text-xs font-medium",
                        )}
                      >
                        <Link
                          href={getParcelDetailPath(parcel.trackingNumber)}
                          className="rounded-sm text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        >
                          {parcel.trackingNumber}
                        </Link>
                      </td>
                      <td className={cn(listDataTableTdClass, "max-w-[220px]")}>
                        <OverflowTooltipCell
                          fullText={[
                            parcel.recipientContactName,
                            parcel.recipientCompanyName,
                            parcel.recipientStreet1,
                            parcel.recipientCity,
                            parcel.recipientPostalCode,
                          ]
                            .filter(Boolean)
                            .join("\n")}
                        >
                          <div className="space-y-0.5">
                            {parcel.recipientContactName ? (
                              <span className="block font-medium">
                                {parcel.recipientContactName}
                              </span>
                            ) : null}
                            {parcel.recipientCompanyName ? (
                              <span className="block text-xs text-muted-foreground">
                                {parcel.recipientCompanyName}
                              </span>
                            ) : null}
                            <span className="block text-xs text-muted-foreground">
                              {[
                                parcel.recipientStreet1,
                                parcel.recipientCity,
                                parcel.recipientPostalCode,
                              ]
                                .filter(Boolean)
                                .join(", ")}
                            </span>
                          </div>
                        </OverflowTooltipCell>
                      </td>
                      <td className={cn(listDataTableTdClass, "tabular-nums")}>
                        {parcel.weight} {parcel.weightUnit}
                      </td>
                      <td className={cn(listDataTableTdClass, "text-muted-foreground")}>
                        <OverflowTooltipCell
                          fullText={
                            parcel.parcelType
                              ? `${parcel.parcelType} | ${formatParcelServiceType(parcel.serviceType ?? "")}`
                              : formatParcelServiceType(parcel.serviceType ?? "")
                          }
                        >
                          {parcel.parcelType
                            ? `${parcel.parcelType} | ${formatParcelServiceType(parcel.serviceType ?? "")}`
                            : formatParcelServiceType(parcel.serviceType ?? "")}
                        </OverflowTooltipCell>
                      </td>
                      <td className={cn(listDataTableTdClass, "text-muted-foreground")}>
                        {parcel.zoneName ?? "-"}
                      </td>
                      <td
                        className={cn(
                          listDataTableTdClass,
                          "tabular-nums text-muted-foreground",
                        )}
                      >
                        {new Date(parcel.createdAt).toLocaleDateString()}
                      </td>
                      <td
                        className={cn(
                          listDataTableTdClass,
                          "tabular-nums text-muted-foreground",
                        )}
                      >
                        {parcel.estimatedDeliveryDate
                          ? new Date(parcel.estimatedDeliveryDate).toLocaleDateString()
                          : "-"}
                      </td>
                      <td
                        className={cn(
                          listDataTableTdClass,
                          "max-w-[160px] align-middle",
                        )}
                      >
                        <OverflowTooltipCell
                          shrinkToContent
                          fullText={formatParcelStatus(parcel.status ?? "")}
                          className={parcelStatusBadgeClass(parcel.status ?? "")}
                        >
                          <span className={parcelStatusBadgeClass(parcel.status ?? "")}>
                            {formatParcelStatus(parcel.status ?? "")}
                          </span>
                        </OverflowTooltipCell>
                      </td>
                      <td className={cn(listDataTableTdClass, "text-right")}>
                        <ParcelRowActions
                          trackingNumber={parcel.trackingNumber}
                          onCancel={() =>
                            setPendingCancellation({
                              id: parcel.id,
                              trackingNumber: parcel.trackingNumber,
                            })
                          }
                          editDisabled={!canManageBeforeLoad}
                          cancelDisabled={cancelParcel.isPending || !canManageBeforeLoad}
                        />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </ListDataTable>

            <ListPagePagination
              page={page}
              totalPages={totalPages}
              setPage={setPage}
            />
          </>
        )}

        <ParcelImportHistoryTable
          selectedImportId={selectedImportId}
          onSelectImport={setSelectedImportId}
        />
      </div>

      <CancelParcelDialog
        open={pendingCancellation !== null}
        onOpenChange={(open) => {
          if (!open) {
            setPendingCancellation(null);
          }
        }}
        trackingNumber={pendingCancellation?.trackingNumber ?? ""}
        onConfirm={handleCancelParcel}
        isPending={cancelParcel.isPending}
      />
    </>
  );
}
