"use client";

import Link from "next/link";
import { useState } from "react";
import { ArrowLeft, MapPin, Package, ScanSearch } from "lucide-react";
import { useSession } from "next-auth/react";

import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { buttonVariants, Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatParcelStatus } from "@/lib/labels/parcels";
import {
  getParcelDetailPath,
  getParcelInboundPath,
  getParcelSortPath,
} from "@/lib/parcels/paths";
import {
  canRouteParcelToExceptionFromSortStation,
  getSortTargetBinsCaption,
  PARCEL_SORT_BLOCK_CODES,
  sortStationExceptionDescription,
} from "@/lib/parcels/sort-station";
import { getErrorMessage } from "@/lib/network/error-message";
import { appToast } from "@/lib/toast/app-toast";
import { cn } from "@/lib/utils";
import {
  useConfirmParcelSort,
  useParcelSortInstruction,
  useTransitionParcelStatus,
} from "@/queries/parcels";
import { useDepots } from "@/queries/depots";

function SortWrongStatusNextSteps({ trackingNumber, statusLabel }: { trackingNumber: string; statusLabel: string }) {
  return (
    <div className="mt-3 border-t border-amber-500/25 pt-3 text-amber-950/95 dark:text-amber-50/95">
      <p className="font-medium">Next steps</p>
      <p className="mt-1 text-amber-900/90 dark:text-amber-100/90">
        Sorting needs status <strong>Received at depot</strong> (same lifecycle as elsewhere in the app).
        This parcel is still <strong>{statusLabel}</strong>. Use{" "}
        <strong className="whitespace-nowrap">Update status</strong> on the parcel page, or process via{" "}
        <strong className="whitespace-nowrap">Inbound receiving</strong>, then scan again here.
      </p>
      <p className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-sm">
        <Link
          href={getParcelDetailPath(trackingNumber)}
          className="font-medium text-primary underline-offset-4 hover:underline"
        >
          Open parcel
        </Link>
        <Link
          href={getParcelInboundPath()}
          className="font-medium text-primary underline-offset-4 hover:underline"
        >
          Inbound receiving
        </Link>
      </p>
    </div>
  );
}

export function ParcelSortPage() {
  const { status: sessionStatus } = useSession();
  const { data: depots = [] } = useDepots();
  const [barcode, setBarcode] = useState("");
  const [activeTracking, setActiveTracking] = useState<string | null>(null);
  const [selectedDepotId, setSelectedDepotId] = useState("");
  /** When null, bin selection follows server recommendation for the current instruction. */
  const [manualBinId, setManualBinId] = useState<string | null>(null);
  const [prevInstructionParcelId, setPrevInstructionParcelId] = useState<string | null>(null);
  const [misSortMessage, setMisSortMessage] = useState<string | null>(null);

  const depotFilter = selectedDepotId || undefined;
  const { data: instruction, isLoading, error: instructionError } = useParcelSortInstruction(
    activeTracking ?? "",
    depotFilter,
    Boolean(activeTracking),
  );

  const confirmSort = useConfirmParcelSort();
  const transitionStatus = useTransitionParcelStatus();

  const defaultBinId = instruction
    ? (instruction.recommendedBinLocationId ??
        instruction.targetBins[0]?.binLocationId ??
        "")
    : "";

  if (instruction && instruction.parcelId !== prevInstructionParcelId) {
    setPrevInstructionParcelId(instruction.parcelId);
    setManualBinId(null);
    setMisSortMessage(null);
  }

  const selectedBinId = manualBinId ?? defaultBinId;

  const canActOnInstruction = Boolean(
    instruction?.canSort && selectedBinId && instruction.parcelId,
  );

  async function handleLookup(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = barcode.trim();
    if (!trimmed) {
      return;
    }
    setMisSortMessage(null);
    setActiveTracking(trimmed);
  }

  function handleClear() {
    setActiveTracking(null);
    setBarcode("");
    setManualBinId(null);
    setPrevInstructionParcelId(null);
    setMisSortMessage(null);
  }

  async function handleConfirmSort() {
    if (!instruction?.parcelId || !selectedBinId) {
      return;
    }
    try {
      setMisSortMessage(null);
      await confirmSort.mutateAsync({
        parcelId: instruction.parcelId,
        binLocationId: selectedBinId,
      });
      handleClear();
    } catch (err) {
      const msg = getErrorMessage(err);
      if (msg.toLowerCase().includes("mis-sort")) {
        setMisSortMessage(msg);
      } else {
        appToast.errorFromUnknown(err);
      }
    }
  }

  async function handleSendToException() {
    if (
      !instruction?.parcelId ||
      !canRouteParcelToExceptionFromSortStation(instruction.status)
    ) {
      return;
    }
    try {
      await transitionStatus.mutateAsync({
        parcelId: instruction.parcelId,
        newStatus: "EXCEPTION",
        location: instruction.depotName ?? undefined,
        description: sortStationExceptionDescription(instruction.blockReasonCode),
      });
      handleClear();
    } catch (err) {
      appToast.errorFromUnknown(err);
    }
  }

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-[40vh] items-center justify-center text-muted-foreground">
        Loading session…
      </div>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-8 px-4 py-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Link
          href="/parcels"
          className={cn(buttonVariants({ variant: "ghost", size: "sm" }), "gap-2")}
        >
          <ArrowLeft className="size-4" aria-hidden />
          Back to parcels
        </Link>
        <div className="flex flex-wrap gap-2 text-sm">
          <Link
            href={getParcelInboundPath()}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            Inbound receiving
          </Link>
          <span
            className={cn(
              buttonVariants({ variant: "secondary", size: "sm" }),
              "pointer-events-none",
            )}
          >
            Sort &amp; zone
          </span>
        </div>
      </div>

      <header className="space-y-2">
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-muted-foreground">
          Warehouse
        </p>
        <h1 className="flex items-center gap-2 text-2xl font-semibold tracking-tight">
          <ScanSearch className="size-7 text-primary" aria-hidden />
          Sort &amp; zone assignment
        </h1>
        <p className="text-sm text-muted-foreground">
          Scan a parcel to see its delivery zone and target bin. Confirm after you place it in the
          correct bin. Wrong-bin attempts show a mis-sort warning. If a parcel cannot be sorted here,
          send it to exception (available while status is Registered or Received at depot), or move
          the status forward on the parcel page first.
        </p>
      </header>

      <section className="rounded-2xl border border-border/60 bg-card/80 p-5 shadow-sm">
        <Label htmlFor="sort-depot-filter" className="text-sm font-medium">
          Filter by depot (optional)
        </Label>
        <select
          id="sort-depot-filter"
          className="mt-1 flex h-10 w-full rounded-xl border border-input/90 bg-background px-3 py-2 text-sm"
          value={selectedDepotId}
          onChange={(e) => {
            setSelectedDepotId(e.target.value);
            setActiveTracking(null);
            setMisSortMessage(null);
          }}
        >
          <option value="">All depots</option>
          {depots.map((d) => (
            <option key={d.id} value={d.id}>
              {d.name}
            </option>
          ))}
        </select>
      </section>

      <form
        onSubmit={(e) => void handleLookup(e)}
        className="rounded-2xl border border-border/60 bg-card/80 p-5 shadow-sm"
      >
        <Label htmlFor="sort-barcode" className="text-sm font-medium">
          Scan or enter tracking number
        </Label>
        <div className="mt-2 flex flex-col gap-3 sm:flex-row sm:items-end">
          <Input
            id="sort-barcode"
            autoComplete="off"
            placeholder="Tracking number / barcode"
            value={barcode}
            onChange={(e) => setBarcode(e.target.value)}
            className="sm:flex-1"
          />
          <div className="flex gap-2">
            <Button type="submit" disabled={isLoading || !barcode.trim()}>
              {isLoading ? "Looking up…" : "Look up"}
            </Button>
            {activeTracking ? (
              <Button type="button" variant="outline" onClick={handleClear}>
                Clear
              </Button>
            ) : null}
          </div>
        </div>
      </form>

      {instructionError ? (
        <QueryErrorAlert
          title="Could not load sort instruction"
          message={getErrorMessage(instructionError)}
        />
      ) : null}

      {activeTracking && !instruction && !isLoading && !instructionError ? (
        <div
          className="rounded-2xl border border-dashed border-amber-500/40 bg-amber-500/5 p-6 text-center"
          role="status"
        >
          <p className="font-medium">Parcel not found</p>
          <p className="mt-1 text-sm text-muted-foreground">
            No parcel matches <strong>{activeTracking}</strong>. Check the barcode or register the
            parcel first.
          </p>
        </div>
      ) : null}

      {instruction ? (
        <div className="space-y-4 rounded-2xl border border-border/60 bg-card/80 p-5 shadow-sm">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="text-xs uppercase tracking-wide text-muted-foreground">Parcel</p>
              <p className="text-lg font-semibold">{instruction.trackingNumber}</p>
              <p className="text-sm text-muted-foreground">
                Status:{" "}
                <span className="font-medium text-foreground">
                  {formatParcelStatus(instruction.status)}
                </span>
              </p>
            </div>
            <div className="flex items-center gap-2 rounded-xl bg-muted/50 px-3 py-2 text-sm">
              <Package className="size-4 text-muted-foreground" aria-hidden />
              <span>{instruction.depotName}</span>
            </div>
          </div>

          <div className="grid gap-3 rounded-xl border border-border/50 bg-background/60 p-4 sm:grid-cols-2">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Delivery zone
              </p>
              <p className="mt-1 flex items-center gap-2 font-medium">
                <MapPin className="size-4 text-primary" aria-hidden />
                {instruction.deliveryZoneName}
              </p>
              {!instruction.deliveryZoneIsActive ? (
                <p className="mt-1 text-xs text-amber-700 dark:text-amber-400">Zone is inactive</p>
              ) : null}
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Target bins
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                {getSortTargetBinsCaption({
                  targetBinCount: instruction.targetBins.length,
                  canSort: instruction.canSort,
                  blockReasonCode: instruction.blockReasonCode,
                })}
              </p>
            </div>
          </div>

          {instruction.blockReasonMessage ? (
            <div
              className="rounded-xl border border-amber-500/35 bg-amber-500/10 px-4 py-3 text-sm"
              role="status"
            >
              <p className="font-medium text-amber-900 dark:text-amber-100">
                Cannot sort automatically
              </p>
              <p className="mt-1 text-amber-900/90 dark:text-amber-100/90">
                {instruction.blockReasonMessage}
              </p>
              {instruction.blockReasonCode ? (
                <p className="mt-2 text-xs text-amber-800/80 dark:text-amber-200/70">
                  Code: {instruction.blockReasonCode}
                </p>
              ) : null}
              {instruction.blockReasonCode === PARCEL_SORT_BLOCK_CODES.WRONG_STATUS ? (
                <SortWrongStatusNextSteps
                  trackingNumber={instruction.trackingNumber}
                  statusLabel={formatParcelStatus(instruction.status)}
                />
              ) : null}
            </div>
          ) : null}

          {instruction.targetBins.length > 0 ? (
            <div>
              <Label htmlFor="sort-bin-select" className="text-sm font-medium">
                Bin you placed the parcel in
              </Label>
              <select
                id="sort-bin-select"
                className="mt-1 flex h-10 w-full rounded-xl border border-input/90 bg-background px-3 py-2 text-sm"
                value={selectedBinId}
                onChange={(e) => setManualBinId(e.target.value)}
              >
                {instruction.targetBins.map((b) => (
                  <option key={b.binLocationId} value={b.binLocationId}>
                    {b.isRecommended ? "★ " : ""}
                    {b.storagePath}
                  </option>
                ))}
              </select>
            </div>
          ) : null}

          {misSortMessage ? (
            <div
              className="rounded-xl border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive"
              role="alert"
            >
              <p className="font-semibold">Mis-sort</p>
              <p className="mt-1">{misSortMessage}</p>
            </div>
          ) : null}

          <div className="flex flex-wrap gap-3">
            <Button
              type="button"
              onClick={() => void handleConfirmSort()}
              disabled={
                !canActOnInstruction || confirmSort.isPending || transitionStatus.isPending
              }
            >
              {confirmSort.isPending ? "Saving…" : "Confirm sorted"}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => void handleSendToException()}
              disabled={
                !instruction.parcelId ||
                !canRouteParcelToExceptionFromSortStation(instruction.status) ||
                transitionStatus.isPending ||
                confirmSort.isPending
              }
            >
              Send to exception
            </Button>
          </div>
          {!canRouteParcelToExceptionFromSortStation(instruction.status) ? (
            <p className="text-xs text-muted-foreground">
              Exception routing from this screen is only available for parcels in &quot;Registered&quot;
              or &quot;Received at depot&quot; status.
            </p>
          ) : null}
        </div>
      ) : null}

      <p className="text-center text-xs text-muted-foreground">
        Configure bin-to-zone links on{" "}
        <Link href="/bin-locations" className="text-primary underline-offset-4 hover:underline">
          Bin locations
        </Link>
        . This page:{" "}
        <code className="rounded bg-muted px-1 py-0.5">{getParcelSortPath()}</code>
      </p>
    </div>
  );
}
