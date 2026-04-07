"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { ArrowLeft, Package, ScanSearch } from "lucide-react";
import { useSession } from "next-auth/react";

import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { buttonVariants, Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getErrorMessage } from "@/lib/network/error-message";
import { appToast } from "@/lib/toast/app-toast";
import { cn } from "@/lib/utils";
import {
  useConfirmInboundReceivingSession,
  useInboundReceivingSession,
  useOpenInboundManifests,
  useScanInboundParcel,
  useStartInboundReceivingSession,
} from "@/queries/parcels";
import type { InboundReceivingSession } from "@/types/parcels";

function CountCard({
  label,
  value,
}: {
  label: string;
  value: number;
}) {
  return (
    <div className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
      <p className="text-xs uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </p>
      <p className="mt-2 text-3xl font-semibold text-foreground">{value}</p>
    </div>
  );
}

export function InboundReceivingPage() {
  const { status: sessionStatus } = useSession();
  const { data: manifests = [], isLoading, error } = useOpenInboundManifests();
  const startSession = useStartInboundReceivingSession();
  const scanParcel = useScanInboundParcel();
  const confirmSession = useConfirmInboundReceivingSession();

  const [selectedManifestId, setSelectedManifestId] = useState<string | null>(null);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [sessionSnapshot, setSessionSnapshot] =
    useState<InboundReceivingSession | null>(null);
  const [barcode, setBarcode] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  const selectedManifest = useMemo(
    () =>
      manifests.find((manifest) => manifest.id === selectedManifestId) ??
      manifests[0] ??
      null,
    [manifests, selectedManifestId],
  );

  const effectiveSessionId = activeSessionId ?? selectedManifest?.openSessionId ?? null;
  const { data: queriedSession } = useInboundReceivingSession(effectiveSessionId);

  const session =
    sessionSnapshot?.manifestId === selectedManifest?.id
      ? sessionSnapshot
      : queriedSession ?? null;
  const manifestHasSession =
    (session?.manifestId === selectedManifest?.id && session?.status === "Open") ||
    Boolean(selectedManifest?.openSessionId);
  const unexpectedParcels =
    session?.exceptions.filter((item) => item.exceptionType === "Unexpected") ??
    [];
  const remainingManifestParcels =
    session?.expectedParcels.filter((item) => !item.isScanned) ?? [];

  async function handleStartOrResume() {
    if (!selectedManifest) {
      return;
    }

    try {
      const nextSession = await startSession.mutateAsync({
        manifestId: selectedManifest.id,
      });
      setActiveSessionId(nextSession.id);
      setSessionSnapshot(nextSession);
      setStatusMessage("");
    } catch (startError) {
      appToast.errorFromUnknown(startError);
    }
  }

  async function handleScan(event: React.FormEvent) {
    event.preventDefault();
    if (!session || !barcode.trim()) {
      return;
    }

    try {
      const result = await scanParcel.mutateAsync({
        sessionId: session.id,
        barcode: barcode.trim(),
      });
      setSessionSnapshot(result.session);
      setBarcode("");
      setStatusMessage(
        result.isExpected
          ? "Expected scan accepted"
          : "Unexpected parcel accepted",
      );
    } catch (scanError) {
      appToast.errorFromUnknown(scanError);
    }
  }

  async function handleConfirm() {
    if (!session) {
      return;
    }

    try {
      const confirmed = await confirmSession.mutateAsync({
        sessionId: session.id,
      });
      setSessionSnapshot(confirmed);
      setStatusMessage("Session confirmed");
    } catch (confirmError) {
      appToast.errorFromUnknown(confirmError);
    }
  }

  if (sessionStatus === "loading" || isLoading) {
    return (
      <div className="rounded-2xl border border-border/60 bg-card/80 p-6 shadow-sm">
        Loading inbound receiving...
      </div>
    );
  }

  if (error) {
    return (
      <QueryErrorAlert
        title="Could not load inbound manifests"
        message={getErrorMessage(error)}
      />
    );
  }

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-border/60 bg-card/85 p-6 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="space-y-2">
            <div className="inline-flex items-center gap-2 rounded-full border border-border/60 bg-background/60 px-3 py-1 text-xs uppercase tracking-[0.18em] text-muted-foreground">
              <ScanSearch className="size-3.5" aria-hidden />
              Inbound Receiving
            </div>
            <h1 className="text-3xl font-semibold tracking-tight text-foreground">
              Scan incoming parcels against the truck manifest
            </h1>
            <p className="max-w-3xl text-sm text-muted-foreground">
              Select the open manifest for your depot, scan each parcel barcode,
              and confirm receipt when unloading is complete.
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

      {manifests.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-border p-10 text-center">
          <p className="font-medium">No open inbound manifests for your depot</p>
          <p className="mt-2 text-sm text-muted-foreground">
            When an upstream manifest is created for your depot, it will appear
            here for receiving.
          </p>
        </div>
      ) : (
        <>
          <div className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
            <div className="flex flex-col gap-3 lg:flex-row lg:items-end">
              <div className="flex-1 space-y-2">
                <Label htmlFor="manifest-select">Inbound manifest</Label>
                <select
                  id="manifest-select"
                  className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm"
                  value={selectedManifest?.id ?? ""}
                  onChange={(event) => {
                    setSelectedManifestId(event.target.value);
                    setActiveSessionId(null);
                    setSessionSnapshot(null);
                    setBarcode("");
                    setStatusMessage("");
                  }}
                >
                  {manifests.map((manifest) => (
                    <option key={manifest.id} value={manifest.id}>
                      {manifest.manifestNumber} |{" "}
                      {manifest.truckIdentifier ?? "Truck pending"} |{" "}
                      {manifest.depotName}
                    </option>
                  ))}
                </select>
              </div>
              <Button onClick={() => void handleStartOrResume()}>
                <Package className="size-4" aria-hidden />
                {manifestHasSession ? "Resume receiving" : "Start receiving"}
              </Button>
            </div>
          </div>

          {session ? (
            <>
              <div className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
                <div className="flex flex-col gap-2 lg:flex-row lg:items-center lg:justify-between">
                  <div>
                    <p className="text-xs uppercase tracking-[0.18em] text-muted-foreground">
                      Active session
                    </p>
                    <div className="mt-1 flex flex-wrap items-center gap-3 text-sm text-foreground">
                      <span className="font-medium">{session.manifestNumber}</span>
                      <span>{session.truckIdentifier ?? "Truck pending"}</span>
                      <span>{session.depotName}</span>
                    </div>
                  </div>
                  <div className="text-sm text-muted-foreground">
                    Status:{" "}
                    <span className="font-medium text-foreground">
                      {session.status}
                    </span>
                  </div>
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-4">
                <CountCard
                  label="Expected parcels"
                  value={session.expectedParcelCount}
                />
                <CountCard
                  label="Expected parcels scanned"
                  value={session.scannedExpectedCount}
                />
                <CountCard
                  label="Unexpected parcels"
                  value={session.scannedUnexpectedCount}
                />
                <CountCard
                  label="Remaining expected"
                  value={session.remainingExpectedCount}
                />
              </div>

              <div className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
                <form className="space-y-3" onSubmit={(event) => void handleScan(event)}>
                  <div className="space-y-2">
                    <Label htmlFor="scan-barcode">Scan barcode</Label>
                    <Input
                      id="scan-barcode"
                      autoFocus
                      autoComplete="off"
                      value={barcode}
                      disabled={session.status !== "Open"}
                      onChange={(event) => setBarcode(event.target.value)}
                      placeholder="Scan or type the parcel barcode"
                    />
                  </div>
                  <div className="flex flex-wrap items-center gap-3">
                    <Button
                      type="submit"
                      disabled={session.status !== "Open" || !barcode.trim()}
                    >
                      Record scan
                    </Button>
                    {statusMessage ? (
                      <span className="text-sm font-medium text-foreground">
                        {statusMessage}
                      </span>
                    ) : null}
                  </div>
                </form>
              </div>

              <div className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
                <div className="space-y-6">
                  <section className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
                    <h2 className="text-lg font-semibold text-foreground">
                      Scanned parcels
                    </h2>
                    <div className="mt-4 space-y-3">
                      {session.scannedParcels.length === 0 ? (
                        <p className="text-sm text-muted-foreground">
                          No parcels scanned yet.
                        </p>
                      ) : (
                        session.scannedParcels.map((scan) => (
                          <div
                            key={scan.id}
                            className="flex items-center justify-between rounded-xl border border-border/60 bg-background/70 px-4 py-3"
                          >
                            <div>
                              <p className="font-mono text-sm font-medium">
                                {scan.trackingNumber}
                              </p>
                              <p className="text-xs text-muted-foreground">
                                {scan.matchType}
                              </p>
                            </div>
                            <p className="text-xs text-muted-foreground">
                              {new Date(scan.scannedAt).toLocaleTimeString()}
                            </p>
                          </div>
                        ))
                      )}
                    </div>
                  </section>

                  <section className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
                    <h2 className="text-lg font-semibold text-foreground">
                      Unexpected parcels
                    </h2>
                    <div className="mt-4 space-y-3">
                      {unexpectedParcels.length === 0 ? (
                        <p className="text-sm text-muted-foreground">
                          No unexpected parcels have been scanned.
                        </p>
                      ) : (
                        unexpectedParcels.map((item) => (
                          <div
                            key={item.id}
                            className="rounded-xl border border-amber-300/60 bg-amber-50/60 px-4 py-3 text-sm text-amber-950"
                          >
                            <p className="font-mono font-medium">
                              {item.trackingNumber}
                            </p>
                            <p className="mt-1 text-xs uppercase tracking-[0.18em]">
                              {item.exceptionType}
                            </p>
                          </div>
                        ))
                      )}
                    </div>
                  </section>
                </div>

                <div className="space-y-6">
                  <section className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
                    <h2 className="text-lg font-semibold text-foreground">
                      Remaining manifest parcels
                    </h2>
                    <div className="mt-4 space-y-3">
                      {remainingManifestParcels.length === 0 ? (
                        <p className="text-sm text-muted-foreground">
                          All expected manifest parcels have been scanned.
                        </p>
                      ) : (
                        remainingManifestParcels.map((item) => (
                          <div
                            key={item.manifestLineId}
                            className="rounded-xl border border-border/60 bg-background/70 px-4 py-3"
                          >
                            <p className="font-mono text-sm font-medium">
                              {item.trackingNumber}
                            </p>
                            <p className="mt-1 text-xs text-muted-foreground">
                              {item.status}
                            </p>
                          </div>
                        ))
                      )}
                    </div>
                  </section>

                  <section className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
                    <h2 className="text-lg font-semibold text-foreground">
                      Session exceptions
                    </h2>
                    <div className="mt-4 space-y-3">
                      {session.exceptions.length === 0 ? (
                        <p className="text-sm text-muted-foreground">
                          No exceptions recorded for this session.
                        </p>
                      ) : (
                        session.exceptions.map((item) => (
                          <div
                            key={item.id}
                            className="rounded-xl border border-border/60 bg-background/70 px-4 py-3"
                          >
                            <p className="font-mono text-sm font-medium">
                              {item.trackingNumber}
                            </p>
                            <p className="mt-1 text-xs uppercase tracking-[0.18em] text-muted-foreground">
                              {item.exceptionType}
                            </p>
                          </div>
                        ))
                      )}
                    </div>
                  </section>

                  <div className="rounded-2xl border border-border/60 bg-card/80 p-4 shadow-sm">
                    <Button
                      className="w-full"
                      disabled={session.status !== "Open"}
                      onClick={() => void handleConfirm()}
                    >
                      Confirm receipt
                    </Button>
                  </div>
                </div>
              </div>
            </>
          ) : null}
        </>
      )}
    </div>
  );
}
