"use client";

import dynamic from "next/dynamic";
import { useState } from "react";
import type {
  SearchBoxFeatureSuggestion,
  SearchBoxRetrieveResponse,
} from "@mapbox/search-js-core";
import { Building2, MapPinHouse, Pencil, Plus, Trash2, X } from "lucide-react";
import { useSession } from "next-auth/react";

import {
  ListDataTable,
  ListPageHeader,
  ListPageLoading,
  ListPageStatsStrip,
  listDataTableBodyRowClass,
  listDataTableHeadRowClass,
  listDataTableTdClass,
  listDataTableThClass,
  listDataTableThRightClass,
} from "@/components/list";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { getMapboxAccessToken, getMapboxConfigurationError } from "@/lib/mapbox/config";
import { getErrorMessage } from "@/lib/network/error-message";
import {
  useCreateDepot,
  useDeleteDepot,
  useDepots,
  useUpdateDepot,
} from "@/queries/depots";
import { cn } from "@/lib/utils";
import type {
  CreateDepotRequest,
  Depot,
  UpdateDepotRequest,
} from "@/types/depots";

const MAPBOX_EXACT_ADDRESS_TYPES = "address";
const MAPBOX_ADDRESS_FIRST_TYPES = "address,street,block";
const MAPBOX_ALPHA3_TO_ALPHA2_COUNTRY_CODES: Record<string, string> = {
  AUS: "AU",
  CAN: "CA",
  GBR: "GB",
  NZL: "NZ",
  USA: "US",
};
const mapboxRegionDisplayNames =
  typeof Intl !== "undefined"
    ? new Intl.DisplayNames(["en"], { type: "region" })
    : null;

const depotSearchBoxTheme = {
  variables: {
    border: "1px solid rgba(148, 163, 184, 0.3)",
    borderRadius: "1rem",
    boxShadow: "0 20px 44px -28px rgba(15, 23, 42, 0.28)",
    colorBackground: "#ffffff",
    colorBackgroundHover: "#f8fafc",
    colorPrimary: "#0f172a",
    colorSecondary: "#64748b",
    colorText: "#0f172a",
    fontFamily: "inherit",
    lineHeight: "1.45",
    minWidth: "100%",
    padding: "0.75rem",
    spacing: "0.5rem",
    unit: "0.95rem",
  },
} as const;

const DepotAddressSearchBox = dynamic(
  () => import("@mapbox/search-js-react").then((module) => module.SearchBox),
  { ssr: false },
);

const DAYS = [
  "Sunday",
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
];

interface DepotFormData {
  name: string;
  street1: string;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
  isActive: boolean;
}

interface HoursData {
  dayOfWeek: number;
  openTime: string;
  closedTime: string;
  isClosed: boolean;
}

interface DepotAddressSearchSelection {
  accuracy: string | null;
  label: string;
  city: string;
  countryCode: string;
  postalCode: string;
  state: string;
  street1: string;
}

function formatRegion(feature: SearchBoxFeatureSuggestion): string {
  const regionCode = feature.properties.context.region?.region_code?.trim();
  if (regionCode) {
    const parts = regionCode.split("-");
    return parts[parts.length - 1] ?? regionCode;
  }

  return feature.properties.context.region?.name?.trim() ?? "";
}

function formatStreetLine(feature: SearchBoxFeatureSuggestion): string {
  const directAddress = feature.properties.address?.trim();
  if (directAddress) {
    return directAddress;
  }

  return [
    feature.properties.context.address_number?.name?.trim(),
    feature.properties.context.street?.name?.trim(),
  ]
    .filter(Boolean)
    .join(" ")
    .trim();
}

function queryIncludesHouseNumber(query: string): boolean {
  return /\d/.test(query);
}

function normalizeMapboxCountryCode(value?: string | null): string | undefined {
  if (!value) {
    return undefined;
  }

  const normalized = value.trim().toUpperCase();
  if (!normalized) {
    return undefined;
  }

  if (normalized.length === 3) {
    return MAPBOX_ALPHA3_TO_ALPHA2_COUNTRY_CODES[normalized];
  }

  if (normalized.length !== 2 || !mapboxRegionDisplayNames) {
    return undefined;
  }

  const displayName = mapboxRegionDisplayNames.of(normalized);
  if (!displayName || displayName.toUpperCase() === normalized) {
    return undefined;
  }

  return normalized;
}

function scoreDepotFeature(
  feature: SearchBoxFeatureSuggestion,
  query: string,
): number {
  let score = 0;

  switch (feature.properties.feature_type?.toLowerCase()) {
    case "address":
      score += 400;
      break;
    case "street":
      score += 180;
      break;
    case "block":
      score += 120;
      break;
    default:
      break;
  }

  switch (feature.properties.coordinates.accuracy?.toLowerCase()) {
    case "rooftop":
      score += 140;
      break;
    case "parcel":
      score += 120;
      break;
    case "interpolated":
      score += 95;
      break;
    case "street":
      score += 50;
      break;
    case "proximate":
      score += 20;
      break;
    default:
      break;
  }

  if (
    feature.properties.coordinates.routable_points?.some(
      (point) => point.name.toLowerCase() === "default",
    )
  ) {
    score += 70;
  }

  if (queryIncludesHouseNumber(query)) {
    const candidateNumber =
      feature.properties.context.address?.address_number?.trim()
      ?? feature.properties.context.address_number?.name?.trim()
      ?? "";

    score += candidateNumber ? 80 : -60;
  }

  return score;
}

function pickBestDepotFeature(
  response: SearchBoxRetrieveResponse,
  query: string,
): SearchBoxFeatureSuggestion | null {
  if (response.features.length === 0) {
    return null;
  }

  return response.features.reduce<SearchBoxFeatureSuggestion | null>(
    (bestFeature, currentFeature) => {
      if (!bestFeature) {
        return currentFeature;
      }

      return scoreDepotFeature(currentFeature, query)
        > scoreDepotFeature(bestFeature, query)
        ? currentFeature
        : bestFeature;
    },
    null,
  );
}

function mapRetrieveResponseToDepotSelection(
  response: SearchBoxRetrieveResponse,
  query: string,
): DepotAddressSearchSelection | null {
  const feature = pickBestDepotFeature(response, query);
  if (!feature) {
    return null;
  }

  const city =
    feature.properties.context.place?.name?.trim()
    ?? feature.properties.context.locality?.name?.trim()
    ?? feature.properties.context.district?.name?.trim()
    ?? "";
  const street1 = formatStreetLine(feature);
  const label =
    feature.properties.full_address?.trim()
    ?? feature.properties.name?.trim()
    ?? street1;

  return {
    accuracy: feature.properties.coordinates.accuracy ?? null,
    city,
    countryCode:
      feature.properties.context.country?.country_code?.trim().toUpperCase() ?? "",
    label,
    postalCode: feature.properties.context.postcode?.name?.trim() ?? "",
    state: formatRegion(feature),
    street1,
  };
}

function formatDepotSearchValue(form: DepotFormData): string {
  return [
    form.street1,
    form.city,
    form.state,
    form.postalCode,
    form.countryCode,
  ]
    .filter((value) => value.trim().length > 0)
    .join(", ");
}

function depotAddressToComparableValue(form: DepotFormData): string {
  return [
    form.street1,
    form.city,
    form.state,
    form.postalCode,
    form.countryCode,
  ]
    .map((value) => value.trim().toUpperCase())
    .join("|");
}

function getSelectionAccuracyMessage(accuracy: string | null): string | null {
  switch (accuracy) {
    case "rooftop":
      return "High-confidence rooftop match.";
    case "parcel":
      return "Parcel-level match.";
    case "interpolated":
      return "Interpolated match. Double-check the street number before saving.";
    case "street":
      return "Street-level match. Double-check the street number before saving.";
    case "proximate":
      return "Approximate match. Review the address before saving.";
    default:
      return null;
  }
}

function defaultHours(): HoursData[] {
  return DAYS.map((_, index) => ({
    dayOfWeek: index,
    openTime: "08:00",
    closedTime: "17:00",
    isClosed: index === 0 || index === 6,
  }));
}

function defaultForm(): { depot: DepotFormData; hours: HoursData[] } {
  return {
    depot: {
      name: "",
      street1: "",
      city: "",
      state: "",
      postalCode: "",
      countryCode: "",
      isActive: true,
    },
    hours: defaultHours(),
  };
}

function toHoursData(depot: Depot | undefined): HoursData[] {
  const fallback = defaultHours();

  if (!depot?.operatingHours?.length) {
    return fallback;
  }

  const hoursByDay = new Map(
    depot.operatingHours.map((item) => [Number(item.dayOfWeek), item]),
  );

  return fallback.map((item) => {
    const existing = hoursByDay.get(item.dayOfWeek);
    if (!existing) {
      return item;
    }

    return {
      dayOfWeek: item.dayOfWeek,
      openTime: existing.openTime?.slice(0, 5) ?? item.openTime,
      closedTime: existing.closedTime?.slice(0, 5) ?? item.closedTime,
      isClosed: existing.isClosed,
    };
  });
}

function DepotForm({
  initial,
  onSubmit,
  onCancel,
  error,
  isPending,
}: {
  initial?: { depot: Depot; hours: HoursData[] };
  onSubmit: (data: CreateDepotRequest | UpdateDepotRequest) => void;
  onCancel: () => void;
  error?: string;
  isPending: boolean;
}) {
  const [form, setForm] = useState<DepotFormData>(
    initial?.depot
      ? {
          name: initial.depot.name,
          street1: initial.depot.address?.street1 ?? "",
          city: initial.depot.address?.city ?? "",
          state: initial.depot.address?.state ?? "",
          postalCode: initial.depot.address?.postalCode ?? "",
          countryCode: initial.depot.address?.countryCode ?? "",
          isActive: initial.depot.isActive,
        }
      : defaultForm().depot,
  );
  const [hours, setHours] = useState<HoursData[]>(
    initial?.hours ?? defaultForm().hours,
  );
  const [addressSearchValue, setAddressSearchValue] = useState(
    formatDepotSearchValue(
      initial?.depot
        ? {
            name: initial.depot.name,
            street1: initial.depot.address?.street1 ?? "",
            city: initial.depot.address?.city ?? "",
            state: initial.depot.address?.state ?? "",
            postalCode: initial.depot.address?.postalCode ?? "",
            countryCode: initial.depot.address?.countryCode ?? "",
            isActive: initial.depot.isActive,
          }
        : defaultForm().depot,
    ),
  );
  const [addressSearchSelection, setAddressSearchSelection] =
    useState<DepotAddressSearchSelection | null>(null);
  const mapboxToken = getMapboxAccessToken();
  const mapboxConfigurationError = getMapboxConfigurationError();
  const addressSearchTypes = queryIncludesHouseNumber(addressSearchValue)
    ? MAPBOX_EXACT_ADDRESS_TYPES
    : MAPBOX_ADDRESS_FIRST_TYPES;
  const addressSearchCountry = normalizeMapboxCountryCode(form.countryCode);

  function setAddressFields(nextFields: Partial<DepotFormData>) {
    setAddressSearchSelection(null);
    setForm((current) => ({ ...current, ...nextFields }));
  }

  function applyAddressSearchSelection(response: SearchBoxRetrieveResponse) {
    const selection = mapRetrieveResponseToDepotSelection(
      response,
      addressSearchValue,
    );
    if (!selection) {
      return;
    }

    setForm((current) => ({
      ...current,
      city: selection.city || current.city,
      countryCode: selection.countryCode || current.countryCode,
      postalCode: selection.postalCode || current.postalCode,
      state: selection.state || current.state,
      street1: selection.street1 || current.street1,
    }));
    setAddressSearchValue(selection.label);
    setAddressSearchSelection(selection);
  }

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const address = {
      street1: form.street1,
      city: form.city,
      state: form.state,
      postalCode: form.postalCode,
      countryCode: form.countryCode,
      isResidential: false,
    };

    const operatingHours = hours
      .filter((item) => !item.isClosed && item.openTime && item.closedTime)
      .map((item) => ({
        dayOfWeek: item.dayOfWeek,
        openTime: item.openTime,
        closedTime: item.closedTime,
        isClosed: item.isClosed,
      }));

    const initialAddressComparableValue = initial?.depot.address
      ? depotAddressToComparableValue({
          name: initial.depot.name,
          street1: initial.depot.address.street1 ?? "",
          city: initial.depot.address.city ?? "",
          state: initial.depot.address.state ?? "",
          postalCode: initial.depot.address.postalCode ?? "",
          countryCode: initial.depot.address.countryCode ?? "",
          isActive: initial.depot.isActive,
        })
      : null;
    const currentAddressComparableValue = depotAddressToComparableValue(form);
    const shouldIncludeAddress =
      !initial || initialAddressComparableValue !== currentAddressComparableValue;

    onSubmit({
      name: form.name,
      ...(shouldIncludeAddress ? { address } : {}),
      operatingHours,
      isActive: form.isActive,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="sm:col-span-2">
          <Label htmlFor="depot-name">Depot name</Label>
          <Input
            id="depot-name"
            value={form.name}
            onChange={(event) => setForm({ ...form, name: event.target.value })}
            required
          />
        </div>
        <div className="sm:col-span-2">
          <Label className="mb-1.5 block">Find address</Label>
          {mapboxToken ? (
            <div className="overflow-visible rounded-[1.25rem] border border-border/60 bg-gradient-to-br from-background via-background to-muted/30 p-3 shadow-[0_18px_45px_-35px_rgba(15,23,42,0.4)]">
              <DepotAddressSearchBox
                accessToken={mapboxToken}
                value={addressSearchValue}
                onChange={setAddressSearchValue}
                onClear={() => {
                  setAddressSearchValue("");
                  setAddressSearchSelection(null);
                }}
                onRetrieve={applyAddressSearchSelection}
                placeholder="Paste or search the full depot address..."
                options={{
                  language: "en",
                  limit: 5,
                  types: addressSearchTypes,
                  ...(addressSearchCountry
                    ? { country: addressSearchCountry }
                    : {}),
                }}
                interceptSearch={(value) => {
                  const query = value.trim();
                  return query.length >= 3 ? query : "";
                }}
                popoverOptions={{
                  flip: false,
                  offset: 8,
                  placement: "bottom-start",
                }}
                theme={depotSearchBoxTheme}
              />
              <div className="mt-3 flex flex-wrap gap-2 text-xs">
                <span className="rounded-full border border-border/70 bg-background px-2.5 py-1 text-muted-foreground">
                  Paste a full address
                </span>
                <span className="rounded-full border border-border/70 bg-background px-2.5 py-1 text-muted-foreground">
                  3+ characters to search
                </span>
                <span className="rounded-full border border-border/70 bg-background px-2.5 py-1 text-muted-foreground">
                  Manual fields stay editable
                </span>
                {addressSearchCountry ? (
                  <span className="rounded-full border border-border/70 bg-background px-2.5 py-1 text-muted-foreground">
                    Country locked to {addressSearchCountry}
                  </span>
                ) : null}
              </div>
              {!queryIncludesHouseNumber(addressSearchValue) ? (
                <p className="mt-3 text-xs text-muted-foreground">
                  Tip: include the building number to get a more reliable match.
                </p>
              ) : null}
            </div>
          ) : (
            <p className="rounded-xl border border-dashed border-border/70 bg-muted/20 px-4 py-3 text-sm text-muted-foreground">
              Address search is unavailable until Mapbox is configured.
              {mapboxConfigurationError ? ` ${mapboxConfigurationError}` : ""}
            </p>
          )}
        </div>
        {addressSearchSelection ? (
          <div className="sm:col-span-2 rounded-[1.25rem] border border-emerald-200/80 bg-emerald-50/70 px-4 py-3 text-sm text-emerald-950">
            <div className="flex items-start gap-3">
              <div className="mt-0.5 rounded-full bg-emerald-100 p-2 text-emerald-700">
                <MapPinHouse className="h-4 w-4" aria-hidden />
              </div>
              <div className="space-y-1">
                <p className="font-medium">Address matched from search</p>
                <p>{addressSearchSelection.label}</p>
                {getSelectionAccuracyMessage(addressSearchSelection.accuracy) ? (
                  <p className="text-xs text-emerald-800/80">
                    {getSelectionAccuracyMessage(addressSearchSelection.accuracy)}
                  </p>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}
        <div className="sm:col-span-2">
          <Label htmlFor="depot-street">Street address</Label>
          <Input
            id="depot-street"
            value={form.street1}
            onChange={(event) =>
              setAddressFields({ street1: event.target.value })
            }
            required
          />
        </div>
        <div>
          <Label htmlFor="depot-city">City</Label>
          <Input
            id="depot-city"
            value={form.city}
            onChange={(event) => setAddressFields({ city: event.target.value })}
            required
          />
        </div>
        <div>
          <Label htmlFor="depot-state">State</Label>
          <Input
            id="depot-state"
            value={form.state}
            onChange={(event) => setAddressFields({ state: event.target.value })}
            required
          />
        </div>
        <div>
          <Label htmlFor="depot-postal">Postal code</Label>
          <Input
            id="depot-postal"
            value={form.postalCode}
            onChange={(event) =>
              setAddressFields({ postalCode: event.target.value })
            }
            required
          />
        </div>
        <div>
          <Label htmlFor="depot-country">Country code</Label>
          <Input
            id="depot-country"
            value={form.countryCode}
            maxLength={2}
            onChange={(event) =>
              setAddressFields({
                countryCode: event.target.value.toUpperCase().slice(0, 2),
              })
            }
            required
          />
        </div>
        <div>
          <Label htmlFor="depot-active">Status</Label>
          <select
            id="depot-active"
            value={form.isActive ? "true" : "false"}
            onChange={(event) =>
              setForm({ ...form, isActive: event.target.value === "true" })
            }
            className="flex h-10 w-full items-center rounded-xl border border-input/90 bg-background px-3 py-2 text-sm"
          >
            <option value="true">Active</option>
            <option value="false">Inactive</option>
          </select>
        </div>
      </div>

      <div>
        <p className="mb-2 text-sm font-medium">Operating hours</p>
        <div className="space-y-2">
          {hours.map((item, index) => (
            <div
              key={index}
              className="flex flex-wrap items-center gap-3 rounded-lg border border-border/60 bg-muted/20 px-3 py-2"
            >
              <span className="w-24 shrink-0 text-sm font-medium">
                {DAYS[index]}
              </span>
              <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <input
                  type="checkbox"
                  checked={item.isClosed}
                  onChange={(event) => {
                    const next = [...hours];
                    next[index] = { ...next[index], isClosed: event.target.checked };
                    setHours(next);
                  }}
                  className="accent-primary"
                />
                Closed
              </label>
              {!item.isClosed ? (
                <div className="flex items-center gap-1">
                  <input
                    type="time"
                    value={item.openTime}
                    onChange={(event) => {
                      const next = [...hours];
                      next[index] = { ...next[index], openTime: event.target.value };
                      setHours(next);
                    }}
                    className="rounded-lg border border-input bg-background px-2 py-1 text-sm"
                  />
                  <span className="text-muted-foreground">to</span>
                  <input
                    type="time"
                    value={item.closedTime}
                    onChange={(event) => {
                      const next = [...hours];
                      next[index] = {
                        ...next[index],
                        closedTime: event.target.value,
                      };
                      setHours(next);
                    }}
                    className="rounded-lg border border-input bg-background px-2 py-1 text-sm"
                  />
                </div>
              ) : null}
            </div>
          ))}
        </div>
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      <div className="flex justify-end gap-3 border-t border-border/50 pt-4">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending ? "Saving..." : initial ? "Save changes" : "Create depot"}
        </Button>
      </div>
    </form>
  );
}

export default function DepotsPage() {
  const { status } = useSession();
  const { data: depots, isLoading, error } = useDepots();
  const createDepot = useCreateDepot();
  const updateDepot = useUpdateDepot();
  const deleteDepot = useDeleteDepot();

  const [showCreate, setShowCreate] = useState(false);
  const [editTarget, setEditTarget] = useState<Depot | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Depot | null>(null);
  const [submitError, setSubmitError] = useState<string | undefined>();

  const totalCount = depots?.length ?? 0;

  if (status === "loading" || isLoading) {
    return <ListPageLoading />;
  }

  if (error) {
    return (
      <QueryErrorAlert
        title="Could not load depots"
        message={getErrorMessage(error)}
      />
    );
  }

  return (
    <>
      {deleteTarget ? (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4">
          <div className="mt-[15vh] w-full max-w-sm rounded-2xl border border-border bg-card p-6 shadow-xl">
            <h2 className="mb-2 text-lg font-semibold">Delete depot</h2>
            <p className="mb-6 text-sm text-muted-foreground">
              Are you sure you want to delete <strong>{deleteTarget.name}</strong>?
              This action cannot be undone.
            </p>
            <div className="flex justify-end gap-3">
              <Button
                variant="outline"
                onClick={() => {
                  setDeleteTarget(null);
                  setSubmitError(undefined);
                }}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                disabled={deleteDepot.isPending}
                onClick={async () => {
                  try {
                    await deleteDepot.mutateAsync(deleteTarget.id);
                    setDeleteTarget(null);
                    setSubmitError(undefined);
                  } catch (mutationError) {
                    setSubmitError(getErrorMessage(mutationError));
                  }
                }}
              >
                {deleteDepot.isPending ? "Deleting..." : "Delete"}
              </Button>
            </div>
            {submitError ? (
              <p className="mt-3 text-sm text-destructive">{submitError}</p>
            ) : null}
          </div>
        </div>
      ) : null}

      {editTarget ? (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4">
          <div className="w-full max-w-2xl rounded-2xl border border-border bg-card p-6 shadow-xl">
            <div className="mb-6 flex items-center justify-between">
              <h2 className="text-lg font-semibold">Edit depot</h2>
              <button
                type="button"
                onClick={() => {
                  setEditTarget(null);
                  setSubmitError(undefined);
                }}
                className="rounded-lg p-1 transition-colors hover:bg-muted"
              >
                <X className="size-5" />
              </button>
            </div>
            <DepotForm
              initial={
                editTarget
                  ? { depot: editTarget, hours: toHoursData(editTarget) }
                  : undefined
              }
              onSubmit={async (data) => {
                try {
                  await updateDepot.mutateAsync({
                    id: editTarget.id,
                    data: data as UpdateDepotRequest,
                  });
                  setEditTarget(null);
                  setSubmitError(undefined);
                } catch (mutationError) {
                  setSubmitError(getErrorMessage(mutationError));
                }
              }}
              onCancel={() => {
                setEditTarget(null);
                setSubmitError(undefined);
              }}
              error={submitError}
              isPending={updateDepot.isPending}
            />
          </div>
        </div>
      ) : null}

      <ListPageHeader
        variant="vehicle"
        eyebrow="Infrastructure"
        title="Depots"
        description="Manage hub locations, working hours, and active status for network operations."
        icon={<Building2 strokeWidth={1.75} aria-hidden />}
        action={
          <Button onClick={() => setShowCreate(true)} className="gap-2">
            <Plus className="size-4" aria-hidden />
            Add depot
          </Button>
        }
      />

      <ListPageStatsStrip
        totalLabel="Total depots"
        totalCount={totalCount}
        rangeEntityLabel="depots"
        from={totalCount === 0 ? 0 : 1}
        to={totalCount}
        page={1}
        totalPages={1}
        pageSize={Math.max(totalCount, 1)}
        filterCardLabel="View"
        filterCardHint="No additional filters on this page"
        activeFilterDisplay="All depots"
      />

      {showCreate ? (
        <div className="mb-8 rounded-2xl border border-border/50 bg-card/80 p-6 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)] dark:bg-card/60">
          <h2 className="mb-4 text-base font-semibold">New depot</h2>
          <DepotForm
            onSubmit={async (data) => {
              try {
                await createDepot.mutateAsync(data as CreateDepotRequest);
                setShowCreate(false);
                setSubmitError(undefined);
              } catch (mutationError) {
                setSubmitError(getErrorMessage(mutationError));
              }
            }}
            onCancel={() => {
              setShowCreate(false);
              setSubmitError(undefined);
            }}
            error={submitError}
            isPending={createDepot.isPending}
          />
        </div>
      ) : null}

      {submitError && !showCreate && !editTarget && !deleteTarget ? (
        <p className="mb-4 text-sm text-destructive">{submitError}</p>
      ) : null}

      {depots && depots.length > 0 ? (
        <ListDataTable minWidthClassName="min-w-[920px]">
          <thead>
            <tr className={listDataTableHeadRowClass}>
              <th className={listDataTableThClass}>Depot</th>
              <th className={listDataTableThClass}>Location</th>
              <th className={listDataTableThClass}>Hours</th>
              <th className={listDataTableThClass}>Status</th>
              <th className={listDataTableThRightClass}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {depots.map((depot) => {
              const city = depot.address?.city ?? "-";
              const country = depot.address?.countryCode ?? "";
              const activeHours =
                depot.operatingHours?.filter((item) => !item.isClosed) ?? [];

              return (
                <tr key={depot.id} className={listDataTableBodyRowClass}>
                  <td className={cn(listDataTableTdClass, "font-medium")}>{depot.name}</td>
                  <td className={cn(listDataTableTdClass, "text-muted-foreground")}>
                    {[city, country].filter(Boolean).join(", ") || "-"}
                  </td>
                  <td className={cn(listDataTableTdClass, "text-muted-foreground")}>
                    {activeHours.length > 0
                      ? `${activeHours.length} day${activeHours.length > 1 ? "s" : ""}`
                      : depot.operatingHours?.some((item) => item.isClosed)
                        ? "Closed"
                        : "-"}
                  </td>
                  <td className={listDataTableTdClass}>
                    <span
                      className={cn(
                        "inline-flex rounded-full px-2.5 py-1 text-xs font-medium",
                        depot.isActive
                          ? "bg-emerald-100 text-emerald-700"
                          : "bg-amber-100 text-amber-700",
                      )}
                    >
                      {depot.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className={cn(listDataTableTdClass, "text-right")}>
                    <div className="flex justify-end gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => setEditTarget(depot)}
                      >
                        <Pencil className="size-3.5" aria-hidden />
                        Edit
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="border-destructive/25 text-destructive hover:bg-destructive/10 hover:text-destructive"
                        onClick={() => setDeleteTarget(depot)}
                      >
                        <Trash2 className="size-3.5" aria-hidden />
                        Delete
                      </Button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </ListDataTable>
      ) : (
        <div className="rounded-2xl border border-dashed border-border p-12 text-center">
          <p className="font-medium">No depots yet</p>
          <p className="mt-1 text-sm text-muted-foreground">
            Use the Add depot action to create your first hub.
          </p>
        </div>
      )}
    </>
  );
}
