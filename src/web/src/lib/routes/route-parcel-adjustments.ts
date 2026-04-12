type RouteParcelAdjustmentLike = {
  action?: string | null;
  trackingNumber?: string | null;
  reason?: string | null;
  changedAt?: string | null;
};

function isRemovedAction(action: string | null | undefined): boolean {
  return action?.toUpperCase() === "REMOVED";
}

export function getRouteParcelAdjustmentTitle(
  adjustment: RouteParcelAdjustmentLike | null | undefined,
): string {
  if (!adjustment) {
    return "Route updated";
  }

  return isRemovedAction(adjustment.action)
    ? "Parcel removed from route"
    : "Parcel added to route";
}

export function getRouteParcelAdjustmentDescription(
  adjustment: RouteParcelAdjustmentLike | null | undefined,
): string {
  if (!adjustment) {
    return "The route changed after dispatch. Refresh the route details for the latest stop list.";
  }

  const action = isRemovedAction(adjustment.action)
    ? "removed from"
    : "added to";
  const parts = [
    adjustment.trackingNumber
      ? `Parcel ${adjustment.trackingNumber} was ${action} the route.`
      : "The route changed after dispatch.",
    adjustment.reason ? `Reason: ${adjustment.reason}.` : "",
  ];

  return parts.filter(Boolean).join(" ");
}

export function formatRouteParcelAdjustmentChangedAt(
  changedAt: string | null | undefined,
): string {
  if (!changedAt) {
    return "";
  }

  const date = new Date(changedAt);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}
