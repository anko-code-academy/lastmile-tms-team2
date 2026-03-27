import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useSession } from "next-auth/react";
import type { MutationToastMeta } from "@/lib/query/mutation-toast-meta";
import { vehiclesService } from "@/services/vehicles.service";
import {
  CreateVehicleRequest,
  UpdateVehicleRequest,
  VehicleStatus,
} from "@/types/vehicles";
import type { VehicleDtoFilterInput } from "@/graphql/generated";

export const vehicleKeys = {
  all: ["vehicles"] as const,
  lists: () => [...vehicleKeys.all, "list"] as const,
  list: (where?: VehicleDtoFilterInput) =>
    [...vehicleKeys.lists(), where] as const,
  details: () => [...vehicleKeys.all, "detail"] as const,
  detail: (id: string) => [...vehicleKeys.details(), id] as const,
};

export function useVehicles(params: {
  status?: VehicleStatus;
  depotId?: string;
}) {
  const { status } = useSession();

  const where: VehicleDtoFilterInput | undefined =
    params.status !== undefined || params.depotId !== undefined
      ? {
          ...(params.status !== undefined && {
            status: { eq: params.status },
          }),
          ...(params.depotId !== undefined && {
            depotId: { eq: params.depotId },
          }),
        }
      : undefined;

  return useQuery({
    queryKey: vehicleKeys.list(where),
    queryFn: () => vehiclesService.getAll(where),
    enabled: status === "authenticated",
  });
}

export function useVehicle(id: string) {
  const { status } = useSession();
  return useQuery({
    queryKey: vehicleKeys.detail(id),
    queryFn: () => vehiclesService.getById(id),
    enabled: status === "authenticated" && !!id,
  });
}

export function useCreateVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateVehicleRequest) => vehiclesService.create(data),
    meta: {
      successToast: {
        title: "Vehicle created",
        describe: (variables) => {
          const v = variables as CreateVehicleRequest;
          return `${v.registrationPlate} was added to the fleet.`;
        },
      },
    } satisfies MutationToastMeta,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
    },
  });
}

export function useUpdateVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: UpdateVehicleRequest;
    }) => vehiclesService.update(id, data),
    meta: {
      successToast: {
        title: "Vehicle updated",
        describe: (variables) => {
          const v = variables as { data: UpdateVehicleRequest };
          return `${v.data.registrationPlate} was saved successfully.`;
        },
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
      queryClient.invalidateQueries({ queryKey: vehicleKeys.detail(id) });
    },
  });
}

export function useDeleteVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (vars: { id: string; registrationPlate?: string }) =>
      vehiclesService.delete(vars.id),
    meta: {
      successToast: {
        title: "Vehicle deleted",
        describe: (variables) => {
          const v = variables as { registrationPlate?: string };
          return v.registrationPlate
            ? `${v.registrationPlate} was removed from the fleet.`
            : undefined;
        },
      },
    } satisfies MutationToastMeta,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vehicleKeys.all });
    },
  });
}
