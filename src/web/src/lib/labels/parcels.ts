import { cn } from "@/lib/utils";

const parcelStatusBadgeBase =
  "inline-flex max-w-full min-w-0 items-center truncate rounded-full px-2 py-0.5 text-xs font-medium";

export const PARCEL_SERVICE_TYPE_LABELS: Record<string, string> = {
  ECONOMY: "Economy",
  STANDARD: "Standard",
  EXPRESS: "Express",
  OVERNIGHT: "Overnight",
};

export function parcelStatusBadgeClass(status: string): string {
  switch (status.toUpperCase()) {
    case "REGISTERED":
      return cn(
        parcelStatusBadgeBase,
        "bg-emerald-100 text-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-200",
      );
    case "RECEIVED_AT_DEPOT":
    case "SORTED":
    case "STAGED":
    case "LOADED":
      return cn(
        parcelStatusBadgeBase,
        "bg-blue-100 text-blue-900 dark:bg-blue-950/50 dark:text-blue-200",
      );
    case "OUT_FOR_DELIVERY":
      return cn(
        parcelStatusBadgeBase,
        "bg-amber-100 text-amber-900 dark:bg-amber-950/50 dark:text-amber-200",
      );
    case "DELIVERED":
      return cn(
        parcelStatusBadgeBase,
        "bg-green-100 text-green-900 dark:bg-green-950/50 dark:text-green-200",
      );
    case "FAILED_ATTEMPT":
    case "EXCEPTION":
      return cn(
        parcelStatusBadgeBase,
        "bg-orange-100 text-orange-900 dark:bg-orange-950/50 dark:text-orange-200",
      );
    case "RETURNED_TO_DEPOT":
    case "CANCELLED":
      return cn(
        parcelStatusBadgeBase,
        "bg-red-100 text-red-900 dark:bg-red-950/50 dark:text-red-200",
      );
    default:
      return parcelStatusBadgeBase;
  }
}

export function formatParcelStatus(status: string): string {
  if (!status) return "-";

  if (!status.includes("_")) {
    return status;
  }

  return status
    .split("_")
    .filter(Boolean)
    .map((segment) => segment[0] + segment.slice(1).toLowerCase())
    .join(" ");
}

export function formatParcelServiceType(serviceType: string): string {
  return PARCEL_SERVICE_TYPE_LABELS[serviceType] ?? serviceType;
}
