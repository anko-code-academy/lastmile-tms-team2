import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSession } from "next-auth/react";
import { parcelsService } from "@/services/parcels.service";
import type {
  ParcelOption,
  RegisterParcelFormData,
  RegisteredParcelResult,
} from "@/types/parcels";

export const parcelKeys = {
  all: ["parcels"] as const,
  forRoute: () => [...parcelKeys.all, "forRoute"] as const,
  registered: () => [...parcelKeys.all, "registered"] as const,
};

export function useParcelsForRouteCreation() {
  const { status } = useSession();
  return useQuery({
    queryKey: parcelKeys.forRoute(),
    queryFn: () => parcelsService.getForRouteCreation(),
    enabled: status === "authenticated",
  });
}

export function useRegisteredParcels() {
  const { status } = useSession();
  return useQuery({
    queryKey: parcelKeys.registered(),
    queryFn: () => parcelsService.getRegisteredParcels(),
    enabled: status === "authenticated",
  });
}

export function useRegisterParcel() {
  const qc = useQueryClient();
  return useMutation<
    RegisteredParcelResult,
    Error,
    RegisterParcelFormData
  >({
    mutationFn: (form: RegisterParcelFormData) => parcelsService.register(form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: parcelKeys.all });
    },
  });
}
