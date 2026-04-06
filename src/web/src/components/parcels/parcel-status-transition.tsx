"use client";

import { useMemo, useState } from "react";
import { ArrowRight } from "lucide-react";

import { DetailPanel } from "@/components/detail";
import { SelectDropdown } from "@/components/form/select-dropdown";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatParcelStatus } from "@/lib/labels/parcels";
import { appToast } from "@/lib/toast/app-toast";
import { useTransitionParcelStatus } from "@/queries/parcels";
import type { GraphQLParcelStatus, ParcelDetail } from "@/types/parcels";
import type { SelectOption } from "@/types/forms";

function filterTransitionTargets(parcel: ParcelDetail): GraphQLParcelStatus[] {
  const next = parcel.allowedNextStatuses ?? [];
  return next.filter((s) => !(parcel.canCancel && s === "CANCELLED"));
}

export function ParcelStatusTransition({ parcel }: { parcel: ParcelDetail }) {
  const targets = useMemo(() => filterTransitionTargets(parcel), [parcel]);
  const transition = useTransitionParcelStatus();
  const [nextStatus, setNextStatus] = useState<GraphQLParcelStatus | "">("");
  const [location, setLocation] = useState("");
  const [description, setDescription] = useState("");

  const firstTarget = targets[0];
  const effectiveStatus =
    nextStatus && targets.includes(nextStatus) ? nextStatus : firstTarget;

  if (targets.length === 0 || !firstTarget) {
    return null;
  }

  const options: SelectOption<GraphQLParcelStatus>[] = targets.map((s) => ({
    value: s,
    label: formatParcelStatus(s),
  }));

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!effectiveStatus) {
      return;
    }
    try {
      await transition.mutateAsync({
        parcelId: parcel.id,
        newStatus: effectiveStatus,
        location: location.trim() || undefined,
        description: description.trim() || undefined,
      });
      setLocation("");
      setDescription("");
    } catch (error) {
      appToast.errorFromUnknown(error);
    }
  }

  return (
    <DetailPanel
      title="Update status"
      description="Move this parcel forward in the warehouse lifecycle. Optional location and notes appear on the tracking timeline."
    >
      <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="parcel-next-status">Next status</Label>
            <SelectDropdown
              id="parcel-next-status"
              options={options}
              value={effectiveStatus}
              onChange={(value) => setNextStatus(value)}
              disabled={transition.isPending}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="parcel-transition-location">Location (optional)</Label>
            <Input
              id="parcel-transition-location"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              placeholder="e.g. depot, gate, dock"
              disabled={transition.isPending}
              autoComplete="off"
            />
          </div>
        </div>
        <div className="space-y-2">
          <Label htmlFor="parcel-transition-notes">Notes (optional)</Label>
          <Input
            id="parcel-transition-notes"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Short note for the timeline"
            disabled={transition.isPending}
            autoComplete="off"
          />
        </div>
        <div className="flex justify-end">
          <Button type="submit" disabled={transition.isPending}>
            <ArrowRight className="mr-2 size-4" aria-hidden />
            {transition.isPending ? "Updating…" : "Apply transition"}
          </Button>
        </div>
      </form>
    </DetailPanel>
  );
}
