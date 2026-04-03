"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { FileText, Package, PackagePlus, Printer } from "lucide-react";
import { useSession } from "next-auth/react";

import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import {
  ListDataTable,
  ListPageHeader,
  ListPageLoading,
  listDataTableBodyRowClass,
  listDataTableHeadRowClass,
  listDataTableTdClass,
  listDataTableThClass,
  listDataTableThRightClass,
} from "@/components/list";
import { OverflowTooltipCell } from "@/components/list/overflow-tooltip-cell";
import { CancelParcelDialog } from "@/components/parcels/cancel-parcel-dialog";
import { ParcelRowActions } from "@/components/parcels/parcel-row-actions";
import { Button } from "@/components/ui/button";
import {
  formatParcelServiceType,
  formatParcelStatus,
  parcelStatusBadgeClass,
} from "@/lib/labels/parcels";
import { getErrorMessage } from "@/lib/network/error-message";
import { appToast } from "@/lib/toast/app-toast";
import { cn } from "@/lib/utils";
import { useCancelParcel, usePreLoadParcels } from "@/queries/parcels";
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
  const { data = [], isLoading, error } = usePreLoadParcels();
  const cancelParcel = useCancelParcel();

  const [showForm, setShowForm] = useState(false);
  const [selectedImportId, setSelectedImportId] = useState<string | null>(null);
  const [selectedParcelIds, setSelectedParcelIds] = useState<string[]>([]);
  const [isDownloading, setIsDownloading] = useState<false | "zpl" | "pdf">(
    false,
  );
  const [pendingCancellation, setPendingCancellation] =
    useState<PendingCancellation>(null);

  const allVisibleSelected =
    data.length > 0 &&
    data.every((parcel) => selectedParcelIds.includes(parcel.id));

  const selectedTrackingNumbers = useMemo(
    () =>
      data
        .filter((parcel) => selectedParcelIds.includes(parcel.id))
        .map((parcel) => parcel.trackingNumber),
    [data, selectedParcelIds],
  );

  function toggleParcelSelection(parcelId: string) {
    setSelectedParcelIds((current) =>
      current.includes(parcelId)
        ? current.filter((selectedId) => selectedId !== parcelId)
        : [...current, parcelId],
    );
  }

  function toggleSelectAllVisible(checked: boolean) {
    setSelectedParcelIds(checked ? data.map((parcel) => parcel.id) : []);
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
        title="Warehouse Pre-load Queue"
        description="Parcels still eligible for edits or cancellation before they are loaded for delivery. Manage imports, register shipments, print labels, and correct mistakes from one queue."
        icon={<Package strokeWidth={1.75} aria-hidden />}
        action={
          <>
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

        {data.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-border p-12 text-center">
            <p className="font-medium">No parcels are waiting before load</p>
            <p className="mt-1 text-sm text-muted-foreground">
              Registered, received, sorted, or staged parcels will appear here until
              they are loaded for delivery.
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
              <p className="text-muted-foreground">
                {selectedTrackingNumbers.length === 0
                  ? "No parcels selected."
                  : `${selectedTrackingNumbers.length} selected: ${selectedTrackingNumbers.join(", ")}`}
              </p>
            </div>

            <ListDataTable minWidthClassName="min-w-[1140px]">
              <thead>
                <tr className={listDataTableHeadRowClass}>
                  <th className={cn(listDataTableThClass, "w-14")}>Select</th>
                  <th className={listDataTableThClass}>Tracking</th>
                  <th className={listDataTableThClass}>Summary</th>
                  <th className={listDataTableThClass}>Weight</th>
                  <th className={listDataTableThClass}>Type</th>
                  <th className={listDataTableThClass}>Zone</th>
                  <th className={listDataTableThClass}>Created</th>
                  <th className={listDataTableThClass}>Delivery Date</th>
                  <th className={listDataTableThClass}>Status</th>
                  <th className={listDataTableThRightClass}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.map((parcel) => (
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
                        href={`/parcels/${parcel.id}`}
                        className="rounded-sm text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      >
                        {parcel.trackingNumber}
                      </Link>
                    </td>
                    <td className={cn(listDataTableTdClass, "max-w-[220px]")}>
                      <OverflowTooltipCell
                        fullText={
                          parcel.parcelType ??
                          `${formatParcelServiceType(parcel.serviceType)} service`
                        }
                      >
                        {parcel.parcelType ??
                          `${formatParcelServiceType(parcel.serviceType)} service`}
                      </OverflowTooltipCell>
                    </td>
                    <td className={cn(listDataTableTdClass, "tabular-nums")}>
                      {parcel.weight} {parcel.weightUnit}
                    </td>
                    <td className={cn(listDataTableTdClass, "text-muted-foreground")}>
                      <OverflowTooltipCell
                        fullText={
                          parcel.parcelType
                            ? `${parcel.parcelType} | ${formatParcelServiceType(parcel.serviceType)}`
                            : formatParcelServiceType(parcel.serviceType)
                        }
                      >
                        {parcel.parcelType
                          ? `${parcel.parcelType} | ${formatParcelServiceType(parcel.serviceType)}`
                          : formatParcelServiceType(parcel.serviceType)}
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
                        fullText={formatParcelStatus(parcel.status)}
                        className={parcelStatusBadgeClass(parcel.status)}
                      >
                        <span className={parcelStatusBadgeClass(parcel.status)}>
                          {formatParcelStatus(parcel.status)}
                        </span>
                      </OverflowTooltipCell>
                    </td>
                    <td className={cn(listDataTableTdClass, "text-right")}>
                      <ParcelRowActions
                        parcelId={parcel.id}
                        trackingNumber={parcel.trackingNumber}
                        onCancel={() =>
                          setPendingCancellation({
                            id: parcel.id,
                            trackingNumber: parcel.trackingNumber,
                          })
                        }
                        cancelDisabled={cancelParcel.isPending}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </ListDataTable>
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
