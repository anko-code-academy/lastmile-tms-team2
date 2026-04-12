import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  HubConnectionBuilder,
  HubConnectionState,
} from "@microsoft/signalr";
import { useEffect } from "react";
import { useSession } from "next-auth/react";
import type { RouteFilterInput } from "@/graphql/generated";
import { apiBaseUrl } from "@/lib/network/api";
import { getRouteParcelAdjustmentDescription } from "@/lib/routes/route-parcel-adjustments";
import type { MutationToastMeta } from "@/lib/query/mutation-toast-meta";
import { appToast } from "@/lib/toast/app-toast";
import { routesService } from "@/services/routes.service";
import type {
  AdjustRouteParcelRequest,
  CancelRouteRequest,
  CompleteRouteRequest,
  CreateRouteRequest,
  RoutePlanPreviewRequest,
  RouteStatus,
  UpdateRouteAssignmentRequest,
} from "@/types/routes";
import { parcelKeys } from "./parcels";
import { vehicleKeys } from "./vehicles";

export const routeKeys = {
  all: ["routes"] as const,
  lists: () => [...routeKeys.all, "list"] as const,
  list: (where?: RouteFilterInput) => [...routeKeys.lists(), where] as const,
  dispatchMap: (dateYmd: string) => [...routeKeys.all, "dispatchMap", dateYmd] as const,
  myLists: () => [...routeKeys.all, "my-list"] as const,
  myList: () => [...routeKeys.myLists()] as const,
  details: () => [...routeKeys.all, "detail"] as const,
  detail: (id: string) => [...routeKeys.details(), id] as const,
  myDetails: () => [...routeKeys.all, "my-detail"] as const,
  myDetail: (id: string) => [...routeKeys.myDetails(), id] as const,
  assignmentCandidates: (
    serviceDate?: string | null,
    zoneId?: string | null,
    routeId?: string | null,
  ) =>
    [
      ...routeKeys.all,
      "assignmentCandidates",
      serviceDate ?? "",
      zoneId ?? "",
      routeId ?? "",
    ] as const,
  preview: (request?: RoutePlanPreviewRequest | null) =>
    [...routeKeys.all, "preview", request ? JSON.stringify(request) : ""] as const,
  adjustmentCandidates: (routeId?: string | null) =>
    [...routeKeys.all, "adjustmentCandidates", routeId ?? ""] as const,
};

function invalidateRouteCaches(queryClient: ReturnType<typeof useQueryClient>, id?: string) {
  queryClient.invalidateQueries({ queryKey: routeKeys.all });
  if (id) {
    queryClient.invalidateQueries({ queryKey: routeKeys.detail(id) });
  }
  queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
  queryClient.invalidateQueries({ queryKey: parcelKeys.all });
  queryClient.invalidateQueries({ queryKey: parcelKeys.details() });
}

export function useRoutes(params: {
  vehicleId?: string;
  status?: RouteStatus;
}) {
  const { status } = useSession();

  const where: RouteFilterInput | undefined =
    params.vehicleId !== undefined || params.status !== undefined
      ? {
          ...(params.vehicleId !== undefined && {
            vehicleId: { eq: params.vehicleId },
          }),
          ...(params.status !== undefined && {
            status: { eq: params.status },
          }),
        }
      : undefined;

  return useQuery({
    queryKey: routeKeys.list(where),
    queryFn: () => routesService.getAll(where),
    enabled: status === "authenticated",
  });
}

export function useRoute(id: string, enabled = true) {
  const { status } = useSession();
  return useQuery({
    queryKey: routeKeys.detail(id),
    queryFn: () => routesService.getById(id),
    enabled: status === "authenticated" && !!id && enabled,
  });
}

export function useMyRoutes() {
  const { status } = useSession();
  return useQuery({
    queryKey: routeKeys.myList(),
    queryFn: () => routesService.getMine(),
    enabled: status === "authenticated",
    refetchOnWindowFocus: true,
    refetchInterval: 60_000,
  });
}

export function useMyRoute(id: string, enabled = true) {
  const { status } = useSession();
  return useQuery({
    queryKey: routeKeys.myDetail(id),
    queryFn: () => routesService.getMyById(id),
    enabled: status === "authenticated" && !!id && enabled,
    refetchOnWindowFocus: true,
    refetchInterval: 60_000,
  });
}

export function useDispatchMapRoutes(dateYmd: string) {
  const { status } = useSession();

  return useQuery({
    queryKey: routeKeys.dispatchMap(dateYmd),
    queryFn: () => routesService.getDispatchMapRoutes(dateYmd),
    enabled: status === "authenticated" && !!dateYmd,
  });
}

export function useRouteAssignmentCandidates(
  serviceDate?: string | null,
  zoneId?: string | null,
  routeId?: string | null,
) {
  const { status } = useSession();
  return useQuery({
    queryKey: routeKeys.assignmentCandidates(serviceDate, zoneId, routeId),
    queryFn: () =>
      routesService.getAssignmentCandidates(
        serviceDate!,
        zoneId!,
        routeId ?? undefined,
      ),
    enabled: status === "authenticated" && !!serviceDate && !!zoneId,
  });
}

export function useRoutePlanPreview(request?: RoutePlanPreviewRequest | null) {
  const { status } = useSession();
  return useQuery({
    queryKey: routeKeys.preview(request),
    queryFn: () => routesService.getPlanPreview(request!),
    enabled:
      status === "authenticated"
      && !!request?.zoneId
      && !!request?.startDate,
  });
}

export function useDispatchedRouteParcelCandidates(
  routeId?: string | null,
  enabled = true,
) {
  const { status } = useSession();
  return useQuery({
    queryKey: routeKeys.adjustmentCandidates(routeId),
    queryFn: () => routesService.getDispatchedRouteParcelCandidates(routeId!),
    enabled: status === "authenticated" && enabled && !!routeId,
  });
}

export function useCreateRoute() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateRouteRequest) => routesService.create(data),
    meta: {
      successToast: {
        title: "Route created",
        description: "The route appears in the list and is ready for dispatch.",
      },
    } satisfies MutationToastMeta,
    onSuccess: () => {
      invalidateRouteCaches(queryClient);
    },
  });
}

export function useUpdateRouteAssignment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateRouteAssignmentRequest }) =>
      routesService.updateAssignment(id, data),
    meta: {
      successToast: {
        title: "Assignment updated",
        description: "The route assignment was updated before dispatch.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, { id }) => {
      invalidateRouteCaches(queryClient, id);
      queryClient.invalidateQueries({
        queryKey: routeKeys.assignmentCandidates(undefined, undefined, undefined).slice(0, 2),
      });
    },
  });
}

export function useCancelRoute() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CancelRouteRequest }) =>
      routesService.cancel(id, data),
    meta: {
      successToast: {
        title: "Route cancelled",
        description: "The route was cancelled, removed from dispatch, and staged parcels were returned to sorted.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, { id }) => {
      invalidateRouteCaches(queryClient, id);
    },
  });
}

export function useDispatchRoute() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => routesService.dispatch(id),
    meta: {
      successToast: {
        title: "Route dispatched",
        description: "The route is locked and ready for the driver to leave the depot.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, id) => {
      invalidateRouteCaches(queryClient, id);
    },
  });
}

export function useAddParcelToDispatchedRoute() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AdjustRouteParcelRequest }) =>
      routesService.addParcelToDispatchedRoute(id, data),
    meta: {
      successToast: {
        title: "Parcel added to route",
        description: "The dispatched route was updated and the driver will see the change on the web schedule.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, { id }) => {
      invalidateRouteCaches(queryClient, id);
    },
  });
}

export function useRemoveParcelFromDispatchedRoute() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AdjustRouteParcelRequest }) =>
      routesService.removeParcelFromDispatchedRoute(id, data),
    meta: {
      successToast: {
        title: "Parcel removed from route",
        description: "The parcel was returned to staged status and the driver's route was updated on the web schedule.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, { id }) => {
      invalidateRouteCaches(queryClient, id);
    },
  });
}

export function useStartRoute() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => routesService.start(id),
    meta: {
      successToast: {
        title: "Route started",
        description: "The route is now marked as in progress.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, id) => {
      invalidateRouteCaches(queryClient, id);
    },
  });
}

export function useCompleteRoute() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CompleteRouteRequest }) =>
      routesService.complete(id, data),
    meta: {
      successToast: {
        title: "Route completed",
        description: "The route has been closed with its final mileage.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, { id }) => {
      invalidateRouteCaches(queryClient, id);
    },
  });
}

type RouteUpdatedEvent = {
  routeId?: string | null;
  action?: string | null;
  trackingNumber?: string | null;
  reason?: string | null;
  changedAt?: string | null;
};

export function useDriverRouteRealtimeUpdates(enabled = true) {
  const queryClient = useQueryClient();
  const { data: session, status } = useSession();
  const accessToken = session?.accessToken ?? "";

  useEffect(() => {
    if (!enabled || status !== "authenticated") {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl().replace(/\/$/, "")}/hubs/routes`, {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .build();

    const handleRouteUpdated = (event: RouteUpdatedEvent) => {
      void queryClient.invalidateQueries({ queryKey: routeKeys.myLists() });
      if (event.routeId) {
        void queryClient.invalidateQueries({ queryKey: routeKeys.myDetail(event.routeId) });
      }

      appToast.success("Route updated", {
        description: getRouteParcelAdjustmentDescription(event),
      });
    };

    connection.on("RouteUpdated", handleRouteUpdated);

    let isDisposed = false;

    void (async () => {
      try {
        await connection.start();
        if (isDisposed) {
          return;
        }

        await connection.invoke("SubscribeToMyRoutes");
      } catch (error) {
        console.warn("Failed to subscribe to route updates", error);
      }
    })();

    return () => {
      isDisposed = true;
      connection.off("RouteUpdated", handleRouteUpdated);

      void (async () => {
        try {
          if (connection.state === HubConnectionState.Connected) {
            await connection.invoke("UnsubscribeFromMyRoutes");
          }
        } catch (error) {
          console.warn("Failed to unsubscribe from route updates", error);
        } finally {
          try {
            await connection.stop();
          } catch (error) {
            console.warn("Failed to stop route updates connection", error);
          }
        }
      })();
    };
  }, [accessToken, enabled, queryClient, status]);
}
