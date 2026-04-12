import type { Route } from "@/types/routes";

export type DriverRouteNoticeTone = "planned" | "ready" | "active" | "neutral";

type DriverRouteNoticeSource = Pick<Route, "status" | "startDate" | "dispatchedAt">;

export type DriverRouteNotice = {
  title: string;
  description: string;
  tone: DriverRouteNoticeTone;
};

function formatRouteDateTime(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return date.toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export function getDriverRouteNotice(
  route: DriverRouteNoticeSource,
): DriverRouteNotice {
  const scheduledDeparture = formatRouteDateTime(route.startDate);
  const dispatchedAt = formatRouteDateTime(route.dispatchedAt);

  switch (route.status) {
    case "DRAFT":
      return {
        title: "Scheduled",
        description: scheduledDeparture
          ? `Planned departure is ${scheduledDeparture}. Final dispatch is still pending.`
          : "This route is planned and waiting for final dispatch.",
        tone: "planned",
      };
    case "DISPATCHED":
      return {
        title: "Ready to leave",
        description: dispatchedAt
          ? `Dispatched at ${dispatchedAt}. The route is ready to leave the depot.`
          : "This route has been dispatched and is ready to leave the depot.",
        tone: "ready",
      };
    case "IN_PROGRESS":
      return {
        title: "Route in progress",
        description: "Deliveries are underway on this route.",
        tone: "active",
      };
    case "COMPLETED":
      return {
        title: "Route completed",
        description: "This route has already been completed.",
        tone: "neutral",
      };
    case "CANCELLED":
      return {
        title: "Route cancelled",
        description: "This route was cancelled and is no longer active.",
        tone: "neutral",
      };
    default:
      return {
        title: "Route status updated",
        description: "Check the route details for the latest assignment state.",
        tone: "neutral",
      };
  }
}
