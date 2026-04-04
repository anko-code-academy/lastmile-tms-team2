import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useSession } from "next-auth/react";

import { parcelsService } from "@/services/parcels.service";
import type { MutationToastMeta } from "@/lib/query/mutation-toast-meta";
import type {
  CancelParcelRequest,
  ParcelDetail,
  ParcelFormData,
  ParcelImportDetail,
  ParcelImportTemplateFormat,
  RegisteredParcelResult,
  UpdateParcelRequest,
  UploadParcelImportRequest,
  UploadParcelImportResult,
} from "@/types/parcels";

const parcelImportPollingStatuses = new Set(["Queued", "Processing"]);

export const parcelKeys = {
  all: ["parcels"] as const,
  preLoad: (search?: string) => [...parcelKeys.all, "preLoad", search ?? ""] as const,
  forRoute: () => [...parcelKeys.all, "forRoute"] as const,
  registered: () => [...parcelKeys.all, "registered"] as const,
  details: () => [...parcelKeys.all, "detail"] as const,
  detail: (id: string) => [...parcelKeys.details(), id] as const,
  imports: () => [...parcelKeys.all, "imports"] as const,
  importDetail: (id: string) => [...parcelKeys.imports(), "detail", id] as const,
};

export function useParcelsForRouteCreation() {
  const { status } = useSession();
  return useQuery({
    queryKey: parcelKeys.forRoute(),
    queryFn: () => parcelsService.getForRouteCreation(),
    enabled: status === "authenticated",
  });
}

export function usePreLoadParcels(search?: string) {
  const { status } = useSession();
  return useQuery({
    queryKey: parcelKeys.preLoad(search),
    queryFn: () => parcelsService.getPreLoadParcels(search),
    placeholderData: keepPreviousData,
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
    ParcelFormData
  >({
    mutationFn: (form: ParcelFormData) => parcelsService.register(form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: parcelKeys.all });
    },
  });
}

export function useUpdateParcel() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (request: UpdateParcelRequest) => parcelsService.update(request),
    meta: {
      successToast: {
        title: "Parcel updated",
        describe: () => "Parcel changes were saved successfully.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: parcelKeys.all });
      qc.invalidateQueries({ queryKey: parcelKeys.detail(variables.id) });
    },
  });
}

export function useCancelParcel() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (request: CancelParcelRequest) => parcelsService.cancel(request),
    meta: {
      successToast: {
        title: "Parcel cancelled",
        describe: () => "The parcel was removed from the pre-load queue.",
      },
    } satisfies MutationToastMeta,
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: parcelKeys.all });
      qc.invalidateQueries({ queryKey: parcelKeys.detail(variables.id) });
    },
  });
}

export function useParcel(id: string) {
  const { status } = useSession();

  return useQuery<ParcelDetail>({
    queryKey: parcelKeys.detail(id),
    queryFn: () => parcelsService.getById(id),
    enabled: status === "authenticated" && Boolean(id),
  });
}

export function useParcelImports() {
  const { status } = useSession();
  return useQuery({
    queryKey: parcelKeys.imports(),
    queryFn: () => parcelsService.getParcelImports(),
    enabled: status === "authenticated",
  });
}

export function useParcelImport(importId: string | null | undefined) {
  const { status } = useSession();

  return useQuery<ParcelImportDetail | null>({
    queryKey: parcelKeys.importDetail(importId ?? "latest"),
    queryFn: () => parcelsService.getParcelImport(importId!),
    enabled: status === "authenticated" && !!importId,
    refetchInterval: (query) => {
      const data = query.state.data;
      return data && parcelImportPollingStatuses.has(data.status) ? 1000 : false;
    },
  });
}

export function useUploadParcelImport() {
  const qc = useQueryClient();

  return useMutation<UploadParcelImportResult, Error, UploadParcelImportRequest>({
    mutationFn: (request) => parcelsService.uploadParcelImport(request),
    onSuccess: async (result) => {
      await qc.invalidateQueries({ queryKey: parcelKeys.imports() });
      await qc.invalidateQueries({
        queryKey: parcelKeys.importDetail(result.importId),
      });
    },
  });
}

export function useDownloadParcelImportTemplate() {
  return useMutation<void, Error, ParcelImportTemplateFormat>({
    mutationFn: (format) => parcelsService.downloadParcelImportTemplate(format),
  });
}

export function useDownloadParcelImportErrors() {
  return useMutation<void, Error, string>({
    mutationFn: (importId) => parcelsService.downloadParcelImportErrors(importId),
  });
}
