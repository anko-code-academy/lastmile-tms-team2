import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSession } from "next-auth/react";
import { binLocationsService } from "@/services/bin-locations.service";
import type {
  CreateBinLocationRequest,
  CreateStorageAisleRequest,
  CreateStorageZoneRequest,
  UpdateBinLocationRequest,
  UpdateStorageAisleRequest,
  UpdateStorageZoneRequest,
} from "@/types/bin-locations";

export const binLocationKeys = {
  all: ["bin-locations"] as const,
  layout: (depotId: string) => [...binLocationKeys.all, "layout", depotId] as const,
};

export function useDepotStorageLayout(depotId: string) {
  const { status } = useSession();

  return useQuery({
    queryKey: binLocationKeys.layout(depotId),
    queryFn: () => binLocationsService.getDepotStorageLayout(depotId),
    enabled: status === "authenticated" && !!depotId,
  });
}

export function useCreateStorageZone() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateStorageZoneRequest) =>
      binLocationsService.createStorageZone(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}

export function useUpdateStorageZone() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateStorageZoneRequest }) =>
      binLocationsService.updateStorageZone(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}

export function useDeleteStorageZone() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => binLocationsService.deleteStorageZone(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}

export function useCreateStorageAisle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateStorageAisleRequest) =>
      binLocationsService.createStorageAisle(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}

export function useUpdateStorageAisle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateStorageAisleRequest }) =>
      binLocationsService.updateStorageAisle(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}

export function useDeleteStorageAisle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => binLocationsService.deleteStorageAisle(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}

export function useCreateBinLocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBinLocationRequest) =>
      binLocationsService.createBinLocation(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}

export function useUpdateBinLocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBinLocationRequest }) =>
      binLocationsService.updateBinLocation(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}

export function useDeleteBinLocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => binLocationsService.deleteBinLocation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: binLocationKeys.all });
    },
  });
}
