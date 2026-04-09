"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  CheckCircle2,
  Package,
  Route as RouteIcon,
  ScanLine,
  Truck,
  User,
  AlertTriangle,
} from "lucide-react";
import { useSession } from "next-auth/react";

import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  ROUTE_STATUS_LABELS,
  STAGING_AREA_LABELS,
  routeStatusBadgeClass,
} from "@/lib/labels/routes";
import { getErrorMessage } from "@/lib/network/error-message";
import { cn } from "@/lib/utils";
import { getParcelDetailPath } from "@/lib/parcels/paths";
import {
  useLoadOutRoutes,
  useRouteLoadOutBoard,
  useLoadParcelForRoute,
  useCompleteLoadOut,
} from "@/queries/parcels";
import type {
  RouteLoadOutBoard,
  LoadParcelForRouteResult,
  CompleteLoadOutResult,
} from "@/types/parcels";

function CountCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
      <p className="text-xs uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </p>
      <p className="mt-2 text-3xl font-semibold text-foreground">{value}</p>
    </div>
  );
}

function ScanMessage({
  result,
}: {
  result: LoadParcelForRouteResult | null;
}) {
  if (!result) return null;

  const toneClass =
    result.outcome === "LOADED" || result.outcome === "ALREADY_LOADED"
      ? "border-emerald-300/60 bg-emerald-50/70 text-emerald-950"
      : "border-amber-300/60 bg-amber-50/80 text-amber-950";

  return (
    <div className={cn("rounded-2xl border px-4 py-3 text-sm shadow-sm", toneClass)}>
      <p className="font-medium">{result.message}</p>
      {result.trackingNumber ? (
        <p className="mt-1 font-mono text-xs uppercase tracking-[0.14em]">
          {result.trackingNumber}
        </p>
      ) : null}
      {result.conflictingStagingArea ? (
        <p className="mt-1 text-xs">
          Assigned staging area:{" "}
          <span className="font-semibold">
            {STAGING_AREA_LABELS[result.conflictingStagingArea]}
          </span>
        </p>
      ) : null}
    </div>
  );
}

function ParcelLoadRow({
  parcel,
}: {
  parcel: RouteLoadOutBoard["expectedParcels"][number];
}) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-2xl border border-border/60 bg-background/75 px-4 py-3">
      <div className="min-w-0">
        <Link
          href={getParcelDetailPath(parcel.trackingNumber)}
          className="font-mono text-sm font-medium text-primary underline-offset-4 hover:underline"
        >
          {parcel.trackingNumber}
        </Link>
        <p className="mt-1 text-xs text-muted-foreground">{parcel.status}</p>
      </div>
      <span
        className={cn(
          "shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold",
          parcel.isLoaded
            ? "bg-emerald-100 text-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-200"
            : "bg-muted text-muted-foreground",
        )}
      >
        {parcel.isLoaded ? "Loaded" : "Pending"}
      </span>
    </div>
  );
}

function CompletionSummary({
  result,
  board,
}: {
  result: CompleteLoadOutResult;
  board: RouteLoadOutBoard;
}) {
  return (
    <div className="space-y-6">
      <div className="rounded-2xl border border-emerald-300/60 bg-emerald-50/70 p-6 shadow-sm">
        <div className="flex items-center gap-3">
          <CheckCircle2 className="size-6 text-emerald-600" />
          <h2 className="text-xl font-semibold text-emerald-950">
            Load-out completed
          </h2>
        </div>
        <p className="mt-2 text-sm text-emerald-800">{result.message}</p>
      </div>

      <div className="rounded-2xl border border-border/60 bg-card/80 p-5 shadow-sm">
        <h3 className="text-sm uppercase tracking-[0.18em] text-muted-foreground">
          Route summary
        </h3>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          <div>
            <p className="text-xs text-muted-foreground">Route</p>
            <p className="font-medium">{board.vehiclePlate}</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Driver</p>
            <p className="font-medium">{board.driverName}</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Vehicle</p>
            <p className="font-medium">{board.vehiclePlate}</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Status</p>
            <span className={routeStatusBadgeClass(board.status)}>
              {ROUTE_STATUS_LABELS[board.status]}
            </span>
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <CountCard label="Total expected" value={result.totalCount} />
        <CountCard label="Loaded" value={result.loadedCount} />
        <CountCard label="Skipped" value={result.skippedCount} />
      </div>

      <div className="flex gap-3">
        <Link href="/parcels" className={cn(buttonVariants({ variant: "outline" }))}>
          <ArrowLeft className="size-4" aria-hidden />
          Back to parcels
        </Link>
      </div>
    </div>
  );
}

function ShortLoadWarningDialog({
  remaining,
  total,
  loaded,
  onConfirm,
  onCancel,
  isPending,
}: {
  remaining: number;
  total: number;
  loaded: number;
  onConfirm: () => void;
  onCancel: () => void;
  isPending: boolean;
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-zinc-950/55 backdrop-blur-[3px]" onClick={onCancel} />
      <div className="relative w-full max-w-md rounded-2xl border border-border bg-card p-6 shadow-lg">
        <div className="flex items-center gap-3">
          <AlertTriangle className="size-6 text-amber-500" />
          <h3 className="text-lg font-semibold text-foreground">
            Short load warning
          </h3>
        </div>
        <p className="mt-3 text-sm text-muted-foreground">
          {remaining} of {total} parcels have not been loaded. Only {loaded} of{" "}
          {total} parcels are confirmed loaded.
        </p>
        <p className="mt-2 text-sm text-muted-foreground">
          Do you want to force complete the load-out with missing parcels?
        </p>
        <div className="mt-6 flex justify-end gap-3">
          <Button variant="outline" onClick={onCancel} disabled={isPending}>
            Go back
          </Button>
          <Button onClick={onConfirm} disabled={isPending}>
            {isPending ? "Completing..." : "Force complete"}
          </Button>
        </div>
      </div>
    </div>
  );
}

export function RouteLoadOutPage() {
  const { status: sessionStatus } = useSession();
  const { data: loadOutRoutes = [], isLoading, error } = useLoadOutRoutes();

  const [selectedRouteId, setSelectedRouteId] = useState<string>("");
  const effectiveSelectedRouteId = useMemo(() => {
    if (selectedRouteId && loadOutRoutes.some((r) => r.id === selectedRouteId)) {
      return selectedRouteId;
    }
    return loadOutRoutes[0]?.id ?? "";
  }, [selectedRouteId, loadOutRoutes]);

  const selectedRoute = useMemo(
    () =>
      loadOutRoutes.find((r) => r.id === effectiveSelectedRouteId) ??
      loadOutRoutes[0] ??
      null,
    [effectiveSelectedRouteId, loadOutRoutes],
  );

  if (sessionStatus === "loading" || isLoading) {
    return (
      <div className="rounded-2xl border border-border/60 bg-card/80 p-6 shadow-sm">
        Loading load-out routes...
      </div>
    );
  }

  if (error) {
    return (
      <QueryErrorAlert
        title="Could not load load-out routes"
        message={getErrorMessage(error)}
      />
    );
  }

  if (loadOutRoutes.length === 0) {
    return (
      <div className="space-y-6">
        <div className="rounded-3xl border border-border/60 bg-card/85 p-6 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div className="space-y-2">
              <div className="inline-flex items-center gap-2 rounded-full border border-border/60 bg-background/60 px-3 py-1 text-xs uppercase tracking-[0.18em] text-muted-foreground">
                <Package className="size-3.5" aria-hidden />
                Vehicle load-out
              </div>
              <h1 className="text-3xl font-semibold tracking-tight text-foreground">
                Route load-out
              </h1>
              <p className="max-w-3xl text-sm text-muted-foreground">
                Scan staged parcels to confirm they are loaded onto the vehicle
                before dispatch.
              </p>
            </div>
            <Link
              href="/parcels"
              className={cn(buttonVariants({ variant: "ghost" }), "self-start")}
            >
              <ArrowLeft className="size-4" aria-hidden />
              Back to parcels
            </Link>
          </div>
        </div>

        <div className="rounded-2xl border border-dashed border-border p-10 text-center">
          <p className="font-medium">No routes are waiting for load-out</p>
          <p className="mt-2 text-sm text-muted-foreground">
            Planned routes with staged parcels will appear here.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-border/60 bg-card/85 p-6 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="space-y-2">
            <div className="inline-flex items-center gap-2 rounded-full border border-border/60 bg-background/60 px-3 py-1 text-xs uppercase tracking-[0.18em] text-muted-foreground">
              <Package className="size-3.5" aria-hidden />
              Vehicle load-out
            </div>
            <h1 className="text-3xl font-semibold tracking-tight text-foreground">
              Route load-out
            </h1>
            <p className="max-w-3xl text-sm text-muted-foreground">
              Confirm that each parcel is loaded onto the vehicle before dispatch.
            </p>
          </div>
          <Link
            href="/parcels"
            className={cn(buttonVariants({ variant: "ghost" }), "self-start")}
          >
            <ArrowLeft className="size-4" aria-hidden />
            Back to parcels
          </Link>
        </div>
      </div>

      <div className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
        <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-end">
          <div className="space-y-2">
            <Label htmlFor="route-select">Delivery route</Label>
            <select
              id="route-select"
              className="h-10 w-full rounded-xl border border-input bg-background px-3 text-sm shadow-sm"
              value={effectiveSelectedRouteId}
              onChange={(event) => setSelectedRouteId(event.target.value)}
            >
              {loadOutRoutes.map((route) => (
                <option key={route.id} value={route.id}>
                  {route.vehiclePlate} | {route.driverName}
                </option>
              ))}
            </select>
          </div>

          {selectedRoute ? (
            <div className="rounded-2xl border border-border/60 bg-background/70 px-4 py-3 text-sm shadow-sm">
              <div className="flex flex-wrap items-center gap-3">
                <span className="inline-flex items-center gap-2 font-medium text-foreground">
                  <Truck className="size-4" aria-hidden />
                  {selectedRoute.vehiclePlate}
                </span>
                <span className="inline-flex items-center gap-2 text-muted-foreground">
                  <User className="size-4" aria-hidden />
                  {selectedRoute.driverName}
                </span>
              </div>
            </div>
          ) : null}
        </div>
      </div>

      {selectedRoute ? (
        <RouteLoadOutBoardPanel key={selectedRoute.id} routeId={selectedRoute.id} />
      ) : null}
    </div>
  );
}

export default RouteLoadOutPage;

function RouteLoadOutBoardPanel({ routeId }: { routeId: string }) {
  const loadParcel = useLoadParcelForRoute();
  const completeLoadOut = useCompleteLoadOut();

  const [barcode, setBarcode] = useState("");
  const [boardSnapshot, setBoardSnapshot] = useState<RouteLoadOutBoard | null>(null);
  const [lastScanResult, setLastScanResult] = useState<LoadParcelForRouteResult | null>(null);
  const [completionResult, setCompletionResult] = useState<CompleteLoadOutResult | null>(null);
  const [shortLoadDialogOpen, setShortLoadDialogOpen] = useState(false);

  const { data: queriedBoard, isLoading, error } = useRouteLoadOutBoard(routeId);
  const board = boardSnapshot?.id === routeId ? boardSnapshot : queriedBoard ?? null;

  async function handleScanSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!barcode.trim()) return;

    const result = await loadParcel.mutateAsync({
      routeId,
      barcode: barcode.trim(),
    });

    setBoardSnapshot(result.board);
    setLastScanResult(result);
    setBarcode("");
  }

  async function handleComplete() {
    if (!board) return;

    if (board.remainingParcelCount > 0) {
      setShortLoadDialogOpen(true);
      return;
    }

    const result = await completeLoadOut.mutateAsync({ routeId, force: false });
    if (result.success) {
      setCompletionResult(result);
    }
  }

  async function handleForceComplete() {
    const result = await completeLoadOut.mutateAsync({ routeId, force: true });
    setShortLoadDialogOpen(false);
    if (result.success) {
      setCompletionResult(result);
    }
  }

  if (completionResult && board) {
    return <CompletionSummary result={completionResult} board={board} />;
  }

  if (error) {
    return (
      <QueryErrorAlert
        title="Could not load the load-out board"
        message={getErrorMessage(error)}
      />
    );
  }

  if (isLoading && !board) {
    return (
      <div className="rounded-2xl border border-border/60 bg-card/80 p-6 shadow-sm">
        Loading load-out board...
      </div>
    );
  }

  if (!board) return null;

  return (
    <>
      {shortLoadDialogOpen && board ? (
        <ShortLoadWarningDialog
          remaining={board.remainingParcelCount}
          total={board.expectedParcelCount}
          loaded={board.loadedParcelCount}
          onConfirm={() => void handleForceComplete()}
          onCancel={() => setShortLoadDialogOpen(false)}
          isPending={completeLoadOut.isPending}
        />
      ) : null}

      <div className="rounded-2xl border border-border/60 bg-card/80 p-5 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="space-y-2">
            <p className="text-xs uppercase tracking-[0.18em] text-muted-foreground">
              Staging area
            </p>
            <div className="flex flex-wrap items-center gap-3">
              <h2 className="text-2xl font-semibold tracking-tight text-foreground">
                {STAGING_AREA_LABELS[board.stagingArea]}
              </h2>
              <span className={routeStatusBadgeClass(board.status)}>
                {ROUTE_STATUS_LABELS[board.status]}
              </span>
            </div>
          </div>
          <p className="text-sm text-muted-foreground">
            Departure: {new Date(board.startDate).toLocaleString()}
          </p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <CountCard label="Expected parcels" value={board.expectedParcelCount} />
        <CountCard label="Loaded parcels" value={board.loadedParcelCount} />
        <CountCard label="Remaining parcels" value={board.remainingParcelCount} />
      </div>

      <div className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
        <form className="space-y-3" onSubmit={(event) => void handleScanSubmit(event)}>
          <div className="space-y-2">
            <Label htmlFor="scan-barcode">Scan barcode</Label>
            <Input
              id="scan-barcode"
              autoFocus
              autoComplete="off"
              value={barcode}
              onChange={(event) => setBarcode(event.target.value)}
              placeholder="Scan or type the parcel barcode"
            />
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <Button
              type="submit"
              disabled={loadParcel.isPending || !barcode.trim()}
            >
              <ScanLine className="size-4" aria-hidden />
              {loadParcel.isPending ? "Loading..." : "Load parcel"}
            </Button>
            {board.loadedParcelCount > 0 ? (
              <Button
                type="button"
                variant="secondary"
                disabled={completeLoadOut.isPending}
                onClick={() => void handleComplete()}
              >
                <RouteIcon className="size-4" aria-hidden />
                Complete load-out
              </Button>
            ) : null}
          </div>
        </form>
      </div>

      <ScanMessage result={lastScanResult} />

      <section className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
        <div className="flex items-center justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-foreground">
              Parcel list
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Parcels assigned to this route.
            </p>
          </div>
        </div>
        <div className="mt-4 space-y-3">
          {board.expectedParcels.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No parcels are assigned to this route.
            </p>
          ) : (
            board.expectedParcels.map((parcel) => (
              <ParcelLoadRow key={parcel.parcelId} parcel={parcel} />
            ))
          )}
        </div>
      </section>
    </>
  );
}
