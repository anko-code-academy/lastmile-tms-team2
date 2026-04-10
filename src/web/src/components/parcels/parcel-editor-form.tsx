"use client";

import { useState } from "react";
import dynamic from "next/dynamic";
import { useRouter } from "next/navigation";
import type {
  SearchBoxFeatureSuggestion,
  SearchBoxRetrieveResponse,
} from "@mapbox/search-js-core";
import { MapPinHouse, Package } from "lucide-react";

import { SelectDropdown } from "@/components/form/select-dropdown";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createEmptyParcelFormData } from "@/lib/parcels/forms";
import { getMapboxAccessToken, getMapboxConfigurationError } from "@/lib/mapbox/config";
import { zodErrorToFieldMap } from "@/lib/validation/zod-field-errors";
import { parcelFormSchema } from "@/lib/validation/parcels";
import { useDepots } from "@/queries/depots";
import type { SelectOption } from "@/types/forms";
import {
  ParcelDimensionUnitOptions,
  ParcelServiceTypeOptions,
  ParcelWeightUnitOptions,
  type GraphQLServiceType,
  type ParcelFormData,
} from "@/types/parcels";

type RecipientSearchSelection = {
  accuracy: string | null;
  label: string;
};

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

const recipientAutofillFieldKeys = [
  "recipientStreet1",
  "recipientCity",
  "recipientState",
  "recipientPostalCode",
  "recipientCountryCode",
] as const;

const searchBoxTheme = {
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

const AddressSearchBox = dynamic(
  () => import("@mapbox/search-js-react").then((module) => module.SearchBox),
  { ssr: false },
);

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

function scoreRecipientFeature(
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

function pickBestRecipientFeature(
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

      return scoreRecipientFeature(currentFeature, query)
        > scoreRecipientFeature(bestFeature, query)
        ? currentFeature
        : bestFeature;
    },
    null,
  );
}

function mapRetrieveResponseToRecipientSelection(
  response: SearchBoxRetrieveResponse,
  query: string,
): (RecipientSearchSelection & {
  city: string;
  countryCode: string;
  postalCode: string;
  state: string;
  street1: string;
}) | null {
  const feature = pickBestRecipientFeature(response, query);
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

function getSelectionAccuracyMessage(accuracy: string | null): string | null {
  switch (accuracy) {
    case "rooftop":
      return "High-confidence rooftop match.";
    case "parcel":
      return "Parcel-level match.";
    case "interpolated":
      return "Interpolated address match. Double-check the building number before submitting.";
    case "street":
      return "Street-level match. Confirm the building number before submitting.";
    case "proximate":
      return "Approximate match. Confirm the street and postal code before submitting.";
    default:
      return null;
  }
}

function renderParcelSubmitError(error: unknown) {
  const message =
    error instanceof Error ? error.message : "Registration failed. Please try again.";
  const isUnexpected = /^Unexpected (Execution )?Error$/i.test(message.trim());

  if (
    message.includes("geocoded")
    || message.includes("zone covers")
    || message.includes("No active zone")
  ) {
    return (
      <div className="space-y-1">
        <p className="text-sm font-medium text-destructive">
          Address could not be validated
        </p>
        <p className="text-sm text-destructive/80">
          {message.includes("geocoded")
            ? "The recipient address could not be located on the map. Please check the address details and try again."
            : "The recipient address falls outside all active delivery zones. Please check the address or contact support."}
        </p>
      </div>
    );
  }

  if (message.includes("was not found") || message.includes("not exist")) {
    return (
      <div className="space-y-1">
        <p className="text-sm font-medium text-destructive">
          Selected depot not found
        </p>
        <p className="text-sm text-destructive/80">
          The depot you selected may have been removed. Please refresh and try again.
        </p>
      </div>
    );
  }

  if (isUnexpected) {
    return (
      <div className="space-y-1">
        <p className="text-sm font-medium text-destructive">
          An unexpected error occurred
        </p>
        <p className="text-sm text-destructive/80">
          Something went wrong on the server. Please try again or contact support if
          the problem persists.
        </p>
      </div>
    );
  }

  return <p className="text-sm text-destructive">{message}</p>;
}

interface ParcelEditorFormProps {
  initialData?: ParcelFormData;
  onSubmit: (form: ParcelFormData) => Promise<void>;
  onCancel?: () => void;
  submitLabel: string;
  pendingLabel: string;
  error?: unknown;
  isPending?: boolean;
}

export function ParcelEditorForm({
  initialData,
  onSubmit,
  onCancel,
  submitLabel,
  pendingLabel,
  error,
  isPending = false,
}: ParcelEditorFormProps) {
  const router = useRouter();
  const { data: depots = [], isLoading: depotsLoading } = useDepots();
  const mapboxToken = getMapboxAccessToken();
  const mapboxConfigurationError = getMapboxConfigurationError();

  const [form, setForm] = useState<ParcelFormData>(
    initialData ?? createEmptyParcelFormData(),
  );
  const [addressSearchValue, setAddressSearchValue] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [recipientSearchSelection, setRecipientSearchSelection] =
    useState<RecipientSearchSelection | null>(null);

  const depotOptions: SelectOption<string>[] = depots
    .filter((depot) => depot.isActive)
    .map((depot) => ({
      value: depot.addressId,
      label: depot.name,
    }));

  const serviceTypeOptions: SelectOption<GraphQLServiceType>[] =
    ParcelServiceTypeOptions.map((option) => ({
      value: option.value,
      label: option.label,
    }));

  const weightUnitOptions: SelectOption<number>[] = ParcelWeightUnitOptions.map(
    (option) => ({
      value: option.value,
      label: option.label,
    }),
  );

  const dimensionUnitOptions: SelectOption<ParcelFormData["dimensionUnit"]>[] =
    ParcelDimensionUnitOptions.map((option) => ({
      value: option.value,
      label: option.label,
    }));

  const selectedDepot = depots.find(
    (depot) => depot.addressId === form.shipperAddressId,
  );
  const recipientSearchProximity = selectedDepot?.address?.geoLocation
    ? {
        lat: selectedDepot.address.geoLocation.latitude,
        lng: selectedDepot.address.geoLocation.longitude,
      }
    : undefined;
  const recipientSearchCountry = normalizeMapboxCountryCode(
    selectedDepot?.address?.countryCode,
  );
  const recipientSearchTypes = queryIncludesHouseNumber(addressSearchValue)
    ? MAPBOX_EXACT_ADDRESS_TYPES
    : MAPBOX_ADDRESS_FIRST_TYPES;

  function clearError(key: string) {
    setErrors((current) => {
      if (current[key] === undefined) {
        return current;
      }

      const next = { ...current };
      delete next[key];
      return next;
    });
  }

  function setField<K extends keyof ParcelFormData>(
    key: K,
    value: ParcelFormData[K],
  ) {
    clearError(key);
    setForm((current) => ({ ...current, [key]: value }));
  }

  function setRecipientAutofillField<
    K extends (typeof recipientAutofillFieldKeys)[number],
  >(
    key: K,
    value: ParcelFormData[K],
  ) {
    setRecipientSearchSelection(null);
    setField(key, value);
  }

  function applyRecipientSearchSelection(response: SearchBoxRetrieveResponse) {
    const selection = mapRetrieveResponseToRecipientSelection(
      response,
      addressSearchValue,
    );
    if (!selection) {
      return;
    }

    setForm((current) => ({
      ...current,
      recipientCity: selection.city || current.recipientCity,
      recipientCountryCode: selection.countryCode || current.recipientCountryCode,
      recipientPostalCode: selection.postalCode || current.recipientPostalCode,
      recipientState: selection.state || current.recipientState,
      recipientStreet1: selection.street1 || current.recipientStreet1,
    }));

    setErrors((current) => {
      const next = { ...current };
      for (const key of recipientAutofillFieldKeys) {
        delete next[key];
      }
      return next;
    });

    setAddressSearchValue(selection.label);
    setRecipientSearchSelection({
      accuracy: selection.accuracy,
      label: selection.label,
    });
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();

    const parsed = parcelFormSchema.safeParse({
      ...form,
      recipientCountryCode: form.recipientCountryCode.toUpperCase().slice(0, 3),
      currency: form.currency.toUpperCase().slice(0, 3),
    });

    if (!parsed.success) {
      setErrors(zodErrorToFieldMap(parsed.error));
      return;
    }

    setErrors({});

    try {
      await onSubmit({
        ...parsed.data,
        shipperAddressId: selectedDepot?.addressId ?? parsed.data.shipperAddressId,
      });
    } catch {
      // Error handling is surfaced via mutation state / global toasts.
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="grid gap-6 md:grid-cols-2">
        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Package className="h-4 w-4" />
              Sender (Depot)
            </CardTitle>
            <CardDescription>
              Choose the origin depot first so address search can bias results to the
              right service area.
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="md:col-span-2">
              <Label htmlFor="shipperAddressId" className="mb-1.5 block">
                Select Depot
              </Label>
              {depotsLoading ? (
                <Input disabled placeholder="Loading depots..." />
              ) : (
                <SelectDropdown
                  id="shipperAddressId"
                  options={depotOptions}
                  value={form.shipperAddressId}
                  onChange={(value) => setField("shipperAddressId", value)}
                  placeholder="Select depot..."
                  invalid={!!errors.shipperAddressId}
                />
              )}
              {errors.shipperAddressId ? (
                <p className="mt-1 text-sm text-destructive">
                  {errors.shipperAddressId}
                </p>
              ) : null}
              {selectedDepot?.address ? (
                <p className="mt-2 rounded-xl border bg-muted/40 px-4 py-3 text-sm text-muted-foreground">
                  {selectedDepot.address.street1}
                  {selectedDepot.address.street2
                    ? `, ${selectedDepot.address.street2}`
                    : ""}
                  <br />
                  {selectedDepot.address.city}
                  {selectedDepot.address.state
                    ? `, ${selectedDepot.address.state}`
                    : ""}{" "}
                  {selectedDepot.address.postalCode}
                  <br />
                  {selectedDepot.address.countryCode}
                </p>
              ) : null}
            </div>
          </CardContent>
        </Card>

        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Recipient</CardTitle>
            <CardDescription>
              Start with search to capture a delivery-ready street address, then
              fine-tune the details below if needed.
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="md:col-span-2">
              <Label className="mb-1.5 block">Find Address</Label>
              {mapboxToken ? (
                <div className="parcel-address-search overflow-visible rounded-[1.25rem] border border-border/60 bg-gradient-to-br from-background via-background to-muted/30 p-3 shadow-[0_18px_45px_-35px_rgba(15,23,42,0.4)]">
                  <AddressSearchBox
                    accessToken={mapboxToken}
                    value={addressSearchValue}
                    onChange={setAddressSearchValue}
                    onClear={() => setAddressSearchValue("")}
                    onRetrieve={applyRecipientSearchSelection}
                    placeholder="Search street address or building number..."
                    options={{
                      language: "en",
                      limit: 5,
                      types: recipientSearchTypes,
                      ...(recipientSearchCountry
                        ? { country: recipientSearchCountry }
                        : {}),
                      ...(recipientSearchProximity
                        ? { proximity: recipientSearchProximity }
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
                    theme={searchBoxTheme}
                  />
                  <div className="mt-3 flex flex-wrap gap-2 text-xs">
                    <span className="rounded-full border border-border/70 bg-background px-2.5 py-1 text-muted-foreground">
                      House numbers work best
                    </span>
                    <span className="rounded-full border border-border/70 bg-background px-2.5 py-1 text-muted-foreground">
                      3+ characters to search
                    </span>
                    <span className="rounded-full border border-border/70 bg-background px-2.5 py-1 text-muted-foreground">
                      Fields stay editable
                    </span>
                    {recipientSearchCountry ? (
                      <span className="rounded-full border border-border/70 bg-background px-2.5 py-1 text-muted-foreground">
                        Country locked to {recipientSearchCountry}
                      </span>
                    ) : null}
                  </div>
                  {!queryIncludesHouseNumber(addressSearchValue) ? (
                    <p className="mt-3 text-xs text-muted-foreground">
                      Tip: include the building number to get the most reliable
                      delivery-zone match.
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

            {recipientSearchSelection ? (
              <div className="md:col-span-2 rounded-[1.25rem] border border-emerald-200/80 bg-emerald-50/70 px-4 py-3 text-sm text-emerald-950">
                <div className="flex items-start gap-3">
                  <div className="mt-0.5 rounded-full bg-emerald-100 p-2 text-emerald-700">
                    <MapPinHouse className="h-4 w-4" aria-hidden />
                  </div>
                  <div className="space-y-1">
                    <p className="font-medium">Address matched from search</p>
                    <p>{recipientSearchSelection.label}</p>
                    {getSelectionAccuracyMessage(
                      recipientSearchSelection.accuracy,
                    ) ? (
                      <p className="text-xs text-emerald-800/80">
                        {getSelectionAccuracyMessage(
                          recipientSearchSelection.accuracy,
                        )}
                      </p>
                    ) : null}
                  </div>
                </div>
              </div>
            ) : null}

            <div className="md:col-span-2">
              <Label htmlFor="recipientStreet1" className="mb-1.5 block">
                Street Address <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientStreet1"
                value={form.recipientStreet1}
                onChange={(event) =>
                  setRecipientAutofillField("recipientStreet1", event.target.value)
                }
                placeholder="123 Main St"
                aria-invalid={!!errors.recipientStreet1}
              />
              {errors.recipientStreet1 ? (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientStreet1}
                </p>
              ) : null}
            </div>

            <div className="md:col-span-2">
              <Label htmlFor="recipientStreet2" className="mb-1.5 block">
                Street Address 2
              </Label>
              <Input
                id="recipientStreet2"
                value={form.recipientStreet2}
                onChange={(event) => setField("recipientStreet2", event.target.value)}
                placeholder="Apt, suite, unit..."
              />
            </div>

            <div>
              <Label htmlFor="recipientCity" className="mb-1.5 block">
                City <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientCity"
                value={form.recipientCity}
                onChange={(event) =>
                  setRecipientAutofillField("recipientCity", event.target.value)
                }
                aria-invalid={!!errors.recipientCity}
              />
              {errors.recipientCity ? (
                <p className="mt-1 text-sm text-destructive">{errors.recipientCity}</p>
              ) : null}
            </div>

            <div>
              <Label htmlFor="recipientState" className="mb-1.5 block">
                State / Province <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientState"
                value={form.recipientState}
                onChange={(event) =>
                  setRecipientAutofillField("recipientState", event.target.value)
                }
                aria-invalid={!!errors.recipientState}
              />
              {errors.recipientState ? (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientState}
                </p>
              ) : null}
            </div>

            <div>
              <Label htmlFor="recipientPostalCode" className="mb-1.5 block">
                Postal Code <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientPostalCode"
                value={form.recipientPostalCode}
                onChange={(event) =>
                  setRecipientAutofillField("recipientPostalCode", event.target.value)
                }
                aria-invalid={!!errors.recipientPostalCode}
              />
              {errors.recipientPostalCode ? (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientPostalCode}
                </p>
              ) : null}
            </div>

            <div>
              <Label htmlFor="recipientCountryCode" className="mb-1.5 block">
                Country Code <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientCountryCode"
                value={form.recipientCountryCode}
                onChange={(event) =>
                  setRecipientAutofillField(
                    "recipientCountryCode",
                    event.target.value.toUpperCase().slice(0, 3),
                  )
                }
                maxLength={3}
                placeholder="US"
                aria-invalid={!!errors.recipientCountryCode}
              />
              {errors.recipientCountryCode ? (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientCountryCode}
                </p>
              ) : null}
            </div>

            <div>
              <Label htmlFor="recipientContactName" className="mb-1.5 block">
                Recipient Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientContactName"
                value={form.recipientContactName}
                onChange={(event) =>
                  setField("recipientContactName", event.target.value)
                }
                placeholder="John Doe"
                aria-invalid={!!errors.recipientContactName}
              />
              {errors.recipientContactName ? (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientContactName}
                </p>
              ) : null}
            </div>

            <div>
              <Label htmlFor="recipientCompanyName" className="mb-1.5 block">
                Company Name
              </Label>
              <Input
                id="recipientCompanyName"
                value={form.recipientCompanyName}
                onChange={(event) =>
                  setField("recipientCompanyName", event.target.value)
                }
                placeholder="Company Inc."
              />
            </div>

            <div>
              <Label htmlFor="recipientPhone" className="mb-1.5 block">
                Phone
              </Label>
              <Input
                id="recipientPhone"
                type="tel"
                value={form.recipientPhone}
                onChange={(event) => setField("recipientPhone", event.target.value)}
                placeholder="+1 555 000 0000"
              />
            </div>

            <div>
              <Label htmlFor="recipientEmail" className="mb-1.5 block">
                Email
              </Label>
              <Input
                id="recipientEmail"
                type="email"
                value={form.recipientEmail}
                onChange={(event) => setField("recipientEmail", event.target.value)}
                placeholder="recipient@example.com"
              />
            </div>

            <div className="md:col-span-2">
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.recipientIsResidential}
                  onChange={(event) =>
                    setField("recipientIsResidential", event.target.checked)
                  }
                  className="h-4 w-4 rounded border-input"
                />
                Residential address
              </label>
            </div>
          </CardContent>
        </Card>

        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Parcel details</CardTitle>
            <CardDescription>
              Confirm service, package type, and measurements before saving the
              shipment.
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="md:col-span-2">
              <Label htmlFor="description" className="mb-1.5 block">
                Notes / Description
              </Label>
              <Input
                id="description"
                value={form.description}
                onChange={(event) => setField("description", event.target.value)}
                placeholder="Fragile, handle with care..."
              />
            </div>

            <div>
              <Label htmlFor="serviceType" className="mb-1.5 block">
                Service Type
              </Label>
              <SelectDropdown
                id="serviceType"
                options={serviceTypeOptions}
                value={form.serviceType}
                onChange={(value) => setField("serviceType", value)}
              />
            </div>

            <div>
              <Label htmlFor="parcelType" className="mb-1.5 block">
                Parcel Type
              </Label>
              <Input
                id="parcelType"
                value={form.parcelType}
                onChange={(event) => setField("parcelType", event.target.value)}
                placeholder="Box, envelope, pallet..."
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="weight" className="mb-1.5 block">
                  Weight <span className="text-destructive">*</span>
                </Label>
                <div className="flex min-w-0 gap-2">
                  <Input
                    id="weight"
                    type="number"
                    min={0.01}
                    step={0.1}
                    value={form.weight}
                    onChange={(event) =>
                      setField("weight", Number.parseFloat(event.target.value) || 0)
                    }
                    className="flex-1 min-w-0"
                    aria-invalid={!!errors.weight}
                  />
                  <SelectDropdown
                    options={weightUnitOptions}
                    value={form.weightUnit}
                    onChange={(value) => setField("weightUnit", value)}
                    className="shrink-0"
                  />
                </div>
                {errors.weight ? (
                  <p className="mt-1 text-sm text-destructive">{errors.weight}</p>
                ) : null}
              </div>

              <div>
                <Label htmlFor="declaredValue" className="mb-1.5 block">
                  Declared Value
                </Label>
                <div className="flex min-w-0 gap-2">
                  <Input
                    id="declaredValue"
                    type="number"
                    min={0}
                    step={0.01}
                    value={form.declaredValue}
                    onChange={(event) =>
                      setField(
                        "declaredValue",
                        Number.parseFloat(event.target.value) || 0,
                      )
                    }
                    className="flex-1 min-w-0"
                    aria-invalid={!!errors.declaredValue}
                  />
                  <Input
                    value={form.currency}
                    onChange={(event) =>
                      setField(
                        "currency",
                        event.target.value.toUpperCase().slice(0, 3),
                      )
                    }
                    maxLength={3}
                    className="w-16 shrink-0"
                  />
                </div>
                {errors.declaredValue ? (
                  <p className="mt-1 text-sm text-destructive">
                    {errors.declaredValue}
                  </p>
                ) : null}
              </div>
            </div>

            <div className="md:col-span-2">
              <Label className="mb-1.5 block">
                Dimensions (L x W x H) <span className="text-destructive">*</span>
              </Label>
              <div className="grid grid-cols-4 gap-2">
                <div>
                  <Input
                    id="length"
                    type="number"
                    min={0.1}
                    step={0.1}
                    value={form.length}
                    onChange={(event) =>
                      setField("length", Number.parseFloat(event.target.value) || 0)
                    }
                    aria-invalid={!!errors.length}
                    aria-label="Length"
                  />
                  {errors.length ? (
                    <p className="mt-1 text-xs text-destructive">{errors.length}</p>
                  ) : null}
                </div>
                <div>
                  <Input
                    id="width"
                    type="number"
                    min={0.1}
                    step={0.1}
                    value={form.width}
                    onChange={(event) =>
                      setField("width", Number.parseFloat(event.target.value) || 0)
                    }
                    aria-invalid={!!errors.width}
                    aria-label="Width"
                  />
                  {errors.width ? (
                    <p className="mt-1 text-xs text-destructive">{errors.width}</p>
                  ) : null}
                </div>
                <div>
                  <Input
                    id="height"
                    type="number"
                    min={0.1}
                    step={0.1}
                    value={form.height}
                    onChange={(event) =>
                      setField("height", Number.parseFloat(event.target.value) || 0)
                    }
                    aria-invalid={!!errors.height}
                    aria-label="Height"
                  />
                  {errors.height ? (
                    <p className="mt-1 text-xs text-destructive">{errors.height}</p>
                  ) : null}
                </div>
                <div>
                  <SelectDropdown
                    options={dimensionUnitOptions}
                    value={form.dimensionUnit}
                    onChange={(value) => setField("dimensionUnit", value)}
                  />
                </div>
              </div>
            </div>

            <div>
              <Label htmlFor="estimatedDeliveryDate" className="mb-1.5 block">
                Est. Delivery Date <span className="text-destructive">*</span>
              </Label>
              <Input
                id="estimatedDeliveryDate"
                type="date"
                value={form.estimatedDeliveryDate}
                onChange={(event) =>
                  setField("estimatedDeliveryDate", event.target.value)
                }
                aria-invalid={!!errors.estimatedDeliveryDate}
              />
              {errors.estimatedDeliveryDate ? (
                <p className="mt-1 text-sm text-destructive">
                  {errors.estimatedDeliveryDate}
                </p>
              ) : null}
            </div>
          </CardContent>
        </Card>

        {error ? (
          <Card className="md:col-span-2 border-destructive">
            <CardContent className="pt-4">
              {renderParcelSubmitError(error)}
            </CardContent>
          </Card>
        ) : null}

        <div className="md:col-span-2 flex justify-end gap-3">
          <Button
            type="button"
            variant="outline"
            onClick={() => {
              if (onCancel) {
                onCancel();
                return;
              }

              router.back();
            }}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={isPending}>
            {isPending ? pendingLabel : submitLabel}
          </Button>
        </div>
      </div>
    </form>
  );
}
