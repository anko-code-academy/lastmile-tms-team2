"use client";

import { BellRing } from "lucide-react";

import {
  formatRouteParcelAdjustmentChangedAt,
  getRouteParcelAdjustmentDescription,
  getRouteParcelAdjustmentTitle,
} from "@/lib/routes/route-parcel-adjustments";
import { cn } from "@/lib/utils";
import type { RouteParcelAdjustmentAuditEntry } from "@/types/routes";

export function RouteAdjustmentBanner({
  adjustment,
  label = "Route update",
  compact = false,
}: {
  adjustment: RouteParcelAdjustmentAuditEntry | null | undefined;
  label?: string;
  compact?: boolean;
}) {
  if (!adjustment) {
    return null;
  }

  const changedAt = formatRouteParcelAdjustmentChangedAt(adjustment.changedAt);

  return (
    <div
      className={cn(
        "rounded-3xl border border-sky-300/60 bg-sky-50/80 text-sky-950 shadow-sm",
        compact ? "px-4 py-3" : "px-5 py-4",
      )}
    >
      <div className="flex items-start gap-3">
        <div className="rounded-2xl bg-background/60 p-2">
          <BellRing className="size-5" aria-hidden />
        </div>
        <div className="space-y-1">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] opacity-80">
            {label}
          </p>
          <h2 className={cn("font-semibold", compact ? "text-base" : "text-lg")}>
            {getRouteParcelAdjustmentTitle(adjustment)}
          </h2>
          <p className="text-sm opacity-90">
            {getRouteParcelAdjustmentDescription(adjustment)}
          </p>
          {changedAt ? (
            <p className="text-xs font-medium uppercase tracking-[0.12em] opacity-70">
              {changedAt}
            </p>
          ) : null}
        </div>
      </div>
    </div>
  );
}
