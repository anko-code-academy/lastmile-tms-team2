import type {
  DispatchMapRoute,
  DispatchMapStopStatus,
  ParcelStatus,
  Route,
  RouteStatus,
  RouteStop,
} from "@/types/routes";

const FAILED_STOP_STATUSES = new Set<ParcelStatus>([
  "FAILED_ATTEMPT",
  "EXCEPTION",
  "CANCELLED",
  "RETURNED_TO_DEPOT",
]);

export const DISPATCH_MAP_ROUTE_STATUS_COLORS: Record<RouteStatus, string> = {
  DRAFT: "#64748b",
  DISPATCHED: "#d97706",
  IN_PROGRESS: "#2563eb",
  COMPLETED: "#059669",
  CANCELLED: "#dc2626",
};

export const DISPATCH_MAP_STOP_STATUS_COLORS: Record<DispatchMapStopStatus, string> = {
  WAITING: "#2563eb",
  DELIVERED: "#059669",
  FAILED: "#dc2626",
};

function parseLocalYmd(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value.trim());
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]) - 1;
  const day = Number(match[3]);
  const date = new Date(year, month, day);

  if (
    date.getFullYear() !== year
    || date.getMonth() !== month
    || date.getDate() !== day
  ) {
    return null;
  }

  return date;
}

export function formatDispatchMapDateYmd(date = new Date()): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function buildDispatchMapDayRange(dateYmd: string): { gte: string; lt: string } {
  const start = parseLocalYmd(dateYmd);
  if (!start) {
    throw new Error(`Invalid dispatch map date: ${dateYmd}`);
  }

  const end = new Date(start);
  end.setDate(end.getDate() + 1);

  return {
    gte: start.toISOString(),
    lt: end.toISOString(),
  };
}

export function computeDispatchMapStopStatus(
  parcels: Array<{ status: ParcelStatus }>,
): DispatchMapStopStatus {
  if (parcels.length > 0 && parcels.every((parcel) => parcel.status === "DELIVERED")) {
    return "DELIVERED";
  }

  if (parcels.some((parcel) => FAILED_STOP_STATUSES.has(parcel.status))) {
    return "FAILED";
  }

  return "WAITING";
}

export function hasDispatchMapPathGeometry(route: Pick<Route, "path">): boolean {
  return route.path.length >= 2;
}

export function hasDispatchMapStopGeometry(route: Pick<Route, "stops">): boolean {
  return route.stops.some((stop) => Number.isFinite(stop.longitude) && Number.isFinite(stop.latitude));
}

export function hasDispatchMapDepotGeometry(
  route: Pick<Route, "depotLongitude" | "depotLatitude">,
): boolean {
  return route.depotLongitude != null && route.depotLatitude != null;
}

export function hasDispatchMapGeometry(
  route: Pick<Route, "path" | "stops" | "depotLongitude" | "depotLatitude">,
): boolean {
  return hasDispatchMapPathGeometry(route)
    || hasDispatchMapStopGeometry(route)
    || hasDispatchMapDepotGeometry(route);
}

export function sortDispatchMapRoutes<T extends Pick<Route, "startDate" | "vehiclePlate">>(routes: T[]): T[] {
  return [...routes].sort((left, right) => {
    const startDelta =
      new Date(left.startDate).getTime() - new Date(right.startDate).getTime();
    if (startDelta !== 0) {
      return startDelta;
    }

    return left.vehiclePlate.localeCompare(right.vehiclePlate);
  });
}

export function toDispatchMapRoute(route: Route): DispatchMapRoute {
  const stops = route.stops.map((stop) => ({
    ...stop,
    uiStatus: computeDispatchMapStopStatus(stop.parcels),
  }));
  const hasPathGeometry = hasDispatchMapPathGeometry(route);
  const hasStopGeometry = hasDispatchMapStopGeometry({ stops });
  const hasDepotGeometry = hasDispatchMapDepotGeometry(route);

  return {
    ...route,
    stops,
    hasGeometry: hasPathGeometry || hasStopGeometry || hasDepotGeometry,
    hasPathGeometry,
    hasStopGeometry,
    hasDepotGeometry,
  };
}

export function getDispatchMapRouteBounds(route: Pick<Route, "path" | "stops">): Array<[number, number]> {
  const points: Array<[number, number]> = [];

  for (const point of route.path) {
    points.push([point.longitude, point.latitude]);
  }

  for (const stop of route.stops) {
    points.push([stop.longitude, stop.latitude]);
  }

  return points;
}

export function stopStatusLabel(status: DispatchMapStopStatus): string {
  switch (status) {
    case "DELIVERED":
      return "Delivered";
    case "FAILED":
      return "Failed";
    default:
      return "Waiting";
  }
}

export function routeGeometryHint(
  route: Pick<
    DispatchMapRoute,
    "hasGeometry" | "hasPathGeometry" | "hasStopGeometry" | "hasDepotGeometry"
  >,
): string | null {
  if (route.hasPathGeometry || route.hasStopGeometry) {
    return null;
  }

  if (route.hasDepotGeometry) {
    return "Depot only";
  }

  return route.hasGeometry ? null : "No geometry";
}

export function routeSummaryLabel(route: Pick<RouteStop, "sequence">): string {
  return `Stop ${route.sequence}`;
}
