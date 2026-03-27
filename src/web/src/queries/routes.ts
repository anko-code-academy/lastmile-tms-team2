import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSession } from "next-auth/react";
import type { MutationToastMeta } from "@/lib/query/mutation-toast-meta";
import { routesService } from "@/services/routes.service";
import { CreateRouteRequest, RouteStatus } from "@/types/routes";
import { vehicleKeys } from "./vehicles";
import { parcelKeys } from "./parcels";
import type { RouteDtoFilterInput } from "@/graphql/generated";

export const routeKeys = {
  all: ["routes"] as const,
  lists: () => [...routeKeys.all, "list"] as const,
  list: (where?: RouteDtoFilterInput) =>
    [...routeKeys.lists(), where] as const,
  details: () => [...routeKeys.all, "detail"] as const,
  detail: (id: string) => [...routeKeys.details(), id] as const,
};

export function useRoutes(params: {
  vehicleId?: string;
  status?: RouteStatus;
}) {
  const { status } = useSession();

  const where: RouteDtoFilterInput | undefined =
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

export function useRoute(id: string) {
  const { status } = useSession();
  return useQuery({
    queryKey: routeKeys.detail(id),
    queryFn: () => routesService.getById(id),
    enabled: status === "authenticated" && !!id,
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
      queryClient.invalidateQueries({ queryKey: routeKeys.all });
      queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
      queryClient.invalidateQueries({ queryKey: parcelKeys.all });
    },
  });
}
