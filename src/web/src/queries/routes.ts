import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSession } from "next-auth/react";
import type { RouteFilterInput } from "@/graphql/generated";
import type { MutationToastMeta } from "@/lib/query/mutation-toast-meta";
import { routesService } from "@/services/routes.service";
import type {
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
  dispatchMap: (dateYmd: string) => [...routeKeys.all, "dispatchMap", dateYmd] as const,
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
