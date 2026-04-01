"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Package, CheckCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { SelectDropdown } from "@/components/form/select-dropdown";
import { useDepots } from "@/queries/depots";
import { useRegisterParcel } from "@/queries/parcels";
import type { SelectOption } from "@/types/forms";
import {
  ParcelDimensionUnit,
  ParcelDimensionUnitOptions,
  ParcelServiceTypeOptions,
  ParcelWeightUnit,
  ParcelWeightUnitOptions,
} from "@/types/parcels";
import type {
  GraphQLDimensionUnit,
  GraphQLServiceType,
  RegisteredParcelResult,
} from "@/types/parcels";

type ParcelFormState = {
  // Shipper
  shipperAddressId: string;
  // Recipient
  recipientStreet1: string;
  recipientStreet2: string;
  recipientCity: string;
  recipientState: string;
  recipientPostalCode: string;
  recipientCountryCode: string;
  recipientIsResidential: boolean;
  recipientContactName: string;
  recipientCompanyName: string;
  recipientPhone: string;
  recipientEmail: string;
  // Parcel
  description: string;
  parcelType: string;
  serviceType: GraphQLServiceType;
  weight: number;
  weightUnit: number;
  length: number;
  width: number;
  height: number;
  dimensionUnit: string;
  declaredValue: number;
  currency: string;
  estimatedDeliveryDate: string;
};

interface ParcelRegistrationFormProps {
  onSuccess?: (parcel: RegisteredParcelResult) => void;
}

export function ParcelRegistrationForm({
  onSuccess,
}: ParcelRegistrationFormProps) {
  const router = useRouter();
  const { data: depots = [], isLoading: depotsLoading } = useDepots();
  const registerParcel = useRegisterParcel();

  const [form, setForm] = useState<ParcelFormState>({
    // Shipper
    shipperAddressId: "",
    // Recipient
    recipientStreet1: "",
    recipientStreet2: "",
    recipientCity: "",
    recipientState: "",
    recipientPostalCode: "",
    recipientCountryCode: "US",
    recipientIsResidential: true,
    recipientContactName: "",
    recipientCompanyName: "",
    recipientPhone: "",
    recipientEmail: "",
    // Parcel
    description: "",
    parcelType: "",
    serviceType: "STANDARD",
    weight: 1,
    weightUnit: ParcelWeightUnit.Kg,
    length: 10,
    width: 10,
    height: 10,
    dimensionUnit: ParcelDimensionUnit.Cm,
    declaredValue: 0,
    currency: "USD",
    estimatedDeliveryDate: "",
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [result, setResult] = useState<RegisteredParcelResult | null>(null);

  const depotOptions: SelectOption<string>[] = depots
    .filter((d) => d.isActive)
    .map((d) => ({
      value: d.id,
      label: d.name,
    }));

  const serviceTypeOptions: SelectOption<GraphQLServiceType>[] = ParcelServiceTypeOptions.map(
    (o) => ({ value: o.value, label: o.label }),
  );

  const weightUnitOptions: SelectOption<number>[] = ParcelWeightUnitOptions.map(
    (o) => ({ value: o.value, label: o.label }),
  );

  const dimensionUnitOptions: SelectOption<string>[] =
    ParcelDimensionUnitOptions.map((o) => ({ value: o.value, label: o.label }));

  function set<K extends keyof typeof form>(key: K, value: (typeof form)[K]) {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((e) => {
      const next = { ...e };
      delete next[key];
      return next;
    });
  }

  function validate(): boolean {
    const errs: Record<string, string> = {};
    if (!form.shipperAddressId) errs.shipperAddressId = "Select a depot.";
    if (!form.recipientStreet1.trim()) errs.recipientStreet1 = "Required.";
    if (!form.recipientCity.trim()) errs.recipientCity = "Required.";
    if (!form.recipientState.trim()) errs.recipientState = "Required.";
    if (!form.recipientPostalCode.trim())
      errs.recipientPostalCode = "Required.";
    if (!form.recipientCountryCode.trim())
      errs.recipientCountryCode = "Required.";
    if (form.weight <= 0) errs.weight = "Must be greater than 0.";
    if (form.length <= 0) errs.length = "Must be greater than 0.";
    if (form.width <= 0) errs.width = "Must be greater than 0.";
    if (form.height <= 0) errs.height = "Must be greater than 0.";
    if (form.declaredValue < 0) errs.declaredValue = "Cannot be negative.";
    if (!form.estimatedDeliveryDate)
      errs.estimatedDeliveryDate = "Required.";
    else {
      const d = new Date(form.estimatedDeliveryDate);
      if (d <= new Date()) errs.estimatedDeliveryDate = "Must be a future date.";
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!validate()) return;
    try {
      // Ensure date is sent as ISO 8601 DateTime (e.g. "2026-04-04T00:00:00+00:00")
      // so HotChocolate can deserialize it to DateTimeOffset regardless of locale.
      const isoDate = form.estimatedDeliveryDate
        ? `${form.estimatedDeliveryDate}T00:00:00+00:00`
        : form.estimatedDeliveryDate;

      const selectedDepot = depots.find(d => d.id === form.shipperAddressId);

      const parcel = await registerParcel.mutateAsync({
        ...form,
        shipperAddressId: selectedDepot?.addressId ?? form.shipperAddressId,
        estimatedDeliveryDate: isoDate,
      });
      setResult(parcel);
      onSuccess?.(parcel);
    } catch {
      // error is handled by mutation state
    }
  }

  if (result) {
    return (
      <Card className="max-w-2xl">
        <CardContent className="pt-6">
          <div className="flex flex-col items-center gap-4 py-8 text-center">
            <CheckCircle className="h-16 w-16 text-green-600" />
            <div>
              <h2 className="text-2xl font-bold">Parcel Registered</h2>
              <p className="mt-1 text-muted-foreground">
                The parcel has been successfully registered.
              </p>
            </div>
            <div className="w-full rounded-xl border bg-muted/40 p-4 text-left">
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <p className="text-muted-foreground">Tracking Number</p>
                  <p className="font-mono font-semibold">{result.trackingNumber}</p>
                </div>
                <div>
                  <p className="text-muted-foreground">Status</p>
                  <p className="font-medium">{result.status}</p>
                </div>
                <div>
                  <p className="text-muted-foreground">Service</p>
                  <p>{result.serviceType}</p>
                </div>
                <div>
                  <p className="text-muted-foreground">Zone</p>
                  <p>{result.zoneName ?? "Unassigned"}</p>
                </div>
                <div>
                  <p className="text-muted-foreground">Weight</p>
                  <p>
                    {result.weight} {result.weightUnit}
                  </p>
                </div>
                <div>
                  <p className="text-muted-foreground">Dimensions</p>
                  <p>
                    {result.length} × {result.width} × {result.height}{" "}
                    {result.dimensionUnit}
                  </p>
                </div>
              </div>
            </div>
            <div className="flex gap-3">
              <Button
                variant="outline"
                onClick={() => setResult(null)}
              >
                Register Another
              </Button>
              <Button onClick={() => router.push("/parcels")}>
                View Intake Queue
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="grid gap-6 md:grid-cols-2">
        {/* Shipper */}
        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Package className="h-4 w-4" />
              Sender (Depot)
            </CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="md:col-span-2">
              <Label htmlFor="shipperAddressId" className="mb-1.5 block">
                Depot
              </Label>
              {depotsLoading ? (
                <Input disabled placeholder="Loading depots..." />
              ) : (
                <SelectDropdown
                  id="shipperAddressId"
                  options={depotOptions}
                  value={form.shipperAddressId}
                  onChange={(v) => set("shipperAddressId", v)}
                  placeholder="Select depot..."
                  invalid={!!errors.shipperAddressId}
                />
              )}
              {errors.shipperAddressId && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.shipperAddressId}
                </p>
              )}
              {form.shipperAddressId && (() => {
                const selected = depots.find(d => d.id === form.shipperAddressId);
                if (!selected?.address) return null;
                const a = selected.address;
                return (
                  <p className="mt-2 rounded-xl border bg-muted/40 px-4 py-3 text-sm text-muted-foreground">
                    {a.street1}
                    {a.street2 ? `, ${a.street2}` : ""}<br />
                    {a.city}{a.state ? `, ${a.state}` : ""} {a.postalCode}<br />
                    {a.countryCode}
                  </p>
                );
              })()}
            </div>
          </CardContent>
        </Card>

        {/* Recipient Address */}
        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Recipient</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="md:col-span-2">
              <Label htmlFor="recipientStreet1" className="mb-1.5 block">
                Street Address <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientStreet1"
                value={form.recipientStreet1}
                onChange={(e) => set("recipientStreet1", e.target.value)}
                placeholder="123 Main St"
                aria-invalid={!!errors.recipientStreet1}
              />
              {errors.recipientStreet1 && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientStreet1}
                </p>
              )}
            </div>

            <div className="md:col-span-2">
              <Label htmlFor="recipientStreet2" className="mb-1.5 block">
                Street Address 2
              </Label>
              <Input
                id="recipientStreet2"
                value={form.recipientStreet2}
                onChange={(e) => set("recipientStreet2", e.target.value)}
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
                onChange={(e) => set("recipientCity", e.target.value)}
                aria-invalid={!!errors.recipientCity}
              />
              {errors.recipientCity && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientCity}
                </p>
              )}
            </div>

            <div>
              <Label htmlFor="recipientState" className="mb-1.5 block">
                State / Province <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientState"
                value={form.recipientState}
                onChange={(e) => set("recipientState", e.target.value)}
                aria-invalid={!!errors.recipientState}
              />
              {errors.recipientState && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientState}
                </p>
              )}
            </div>

            <div>
              <Label htmlFor="recipientPostalCode" className="mb-1.5 block">
                Postal Code <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientPostalCode"
                value={form.recipientPostalCode}
                onChange={(e) => set("recipientPostalCode", e.target.value)}
                aria-invalid={!!errors.recipientPostalCode}
              />
              {errors.recipientPostalCode && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientPostalCode}
                </p>
              )}
            </div>

            <div>
              <Label htmlFor="recipientCountryCode" className="mb-1.5 block">
                Country Code <span className="text-destructive">*</span>
              </Label>
              <Input
                id="recipientCountryCode"
                value={form.recipientCountryCode}
                onChange={(e) =>
                  set("recipientCountryCode", e.target.value.toUpperCase().slice(0, 3))
                }
                maxLength={3}
                placeholder="US"
                aria-invalid={!!errors.recipientCountryCode}
              />
              {errors.recipientCountryCode && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.recipientCountryCode}
                </p>
              )}
            </div>

            <div>
              <Label htmlFor="recipientContactName" className="mb-1.5 block">
                Recipient Name
              </Label>
              <Input
                id="recipientContactName"
                value={form.recipientContactName}
                onChange={(e) => set("recipientContactName", e.target.value)}
                placeholder="John Doe"
              />
            </div>

            <div>
              <Label htmlFor="recipientCompanyName" className="mb-1.5 block">
                Company Name
              </Label>
              <Input
                id="recipientCompanyName"
                value={form.recipientCompanyName}
                onChange={(e) => set("recipientCompanyName", e.target.value)}
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
                onChange={(e) => set("recipientPhone", e.target.value)}
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
                onChange={(e) => set("recipientEmail", e.target.value)}
                placeholder="recipient@example.com"
              />
            </div>

            <div className="md:col-span-2">
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.recipientIsResidential}
                  onChange={(e) =>
                    set("recipientIsResidential", e.target.checked)
                  }
                  className="h-4 w-4 rounded border-input"
                />
                Residential address
              </label>
            </div>
          </CardContent>
        </Card>

        {/* Parcel Details */}
        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Parcel Details</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="md:col-span-2">
              <Label htmlFor="description" className="mb-1.5 block">
                Notes / Description
              </Label>
              <Input
                id="description"
                value={form.description}
                onChange={(e) => set("description", e.target.value)}
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
                onChange={(v) => set("serviceType", v)}
              />
            </div>

            <div>
              <Label htmlFor="parcelType" className="mb-1.5 block">
                Parcel Type
              </Label>
              <Input
                id="parcelType"
                value={form.parcelType}
                onChange={(e) => set("parcelType", e.target.value)}
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
                    onChange={(e) => set("weight", parseFloat(e.target.value) || 0)}
                    className="flex-1 min-w-0"
                    aria-invalid={!!errors.weight}
                  />
                  <SelectDropdown
                    options={weightUnitOptions}
                    value={form.weightUnit}
                    onChange={(v) => set("weightUnit", v)}
                    className="shrink-0"
                  />
                </div>
                {errors.weight && (
                  <p className="mt-1 text-sm text-destructive">{errors.weight}</p>
                )}
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
                    onChange={(e) =>
                      set("declaredValue", parseFloat(e.target.value) || 0)
                    }
                    className="flex-1 min-w-0"
                    aria-invalid={!!errors.declaredValue}
                  />
                  <Input
                    value={form.currency}
                    onChange={(e) =>
                      set("currency", e.target.value.toUpperCase().slice(0, 3))
                    }
                    maxLength={3}
                    className="w-16 shrink-0"
                  />
                </div>
                {errors.declaredValue && (
                  <p className="mt-1 text-sm text-destructive">
                    {errors.declaredValue}
                  </p>
                )}
              </div>
            </div>

            <div className="md:col-span-2">
              <Label className="mb-1.5 block">
                Dimensions (L × W × H){" "}
                <span className="text-destructive">*</span>
              </Label>
              <div className="grid grid-cols-4 gap-2">
                <div>
                  <Input
                    id="length"
                    type="number"
                    min={0.1}
                    step={0.1}
                    value={form.length}
                    onChange={(e) =>
                      set("length", parseFloat(e.target.value) || 0)
                    }
                    aria-invalid={!!errors.length}
                    aria-label="Length"
                  />
                  {errors.length && (
                    <p className="mt-1 text-xs text-destructive">
                      {errors.length}
                    </p>
                  )}
                </div>
                <div>
                  <Input
                    id="width"
                    type="number"
                    min={0.1}
                    step={0.1}
                    value={form.width}
                    onChange={(e) => set("width", parseFloat(e.target.value) || 0)}
                    aria-invalid={!!errors.width}
                    aria-label="Width"
                  />
                  {errors.width && (
                    <p className="mt-1 text-xs text-destructive">
                      {errors.width}
                    </p>
                  )}
                </div>
                <div>
                  <Input
                    id="height"
                    type="number"
                    min={0.1}
                    step={0.1}
                    value={form.height}
                    onChange={(e) =>
                      set("height", parseFloat(e.target.value) || 0)
                    }
                    aria-invalid={!!errors.height}
                    aria-label="Height"
                  />
                  {errors.height && (
                    <p className="mt-1 text-xs text-destructive">
                      {errors.height}
                    </p>
                  )}
                </div>
                <div>
                  <SelectDropdown
                    options={dimensionUnitOptions}
                    value={form.dimensionUnit}
                    onChange={(v) => set("dimensionUnit", v)}
                  />
                </div>
              </div>
            </div>

            <div>
              <Label htmlFor="estimatedDeliveryDate" className="mb-1.5 block">
                Est. Delivery Date{" "}
                <span className="text-destructive">*</span>
              </Label>
              <Input
                id="estimatedDeliveryDate"
                type="date"
                value={form.estimatedDeliveryDate}
                onChange={(e) => set("estimatedDeliveryDate", e.target.value)}
                aria-invalid={!!errors.estimatedDeliveryDate}
              />
              {errors.estimatedDeliveryDate && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.estimatedDeliveryDate}
                </p>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Error */}
        {registerParcel.isError && (
          <Card className="md:col-span-2 border-destructive">
            <CardContent className="pt-4">
              {(() => {
                const msg =
                  registerParcel.error instanceof Error
                    ? registerParcel.error.message
                    : "Registration failed. Please try again.";
                const isUnexpected = /^Unexpected (Execution )?Error$/i.test(msg.trim());

                if (
                  msg.includes("geocoded") ||
                  msg.includes("zone covers") ||
                  msg.includes("No active zone")
                ) {
                  return (
                    <div className="space-y-1">
                      <p className="text-sm font-medium text-destructive">
                        Address could not be validated
                      </p>
                      <p className="text-sm text-destructive/80">
                        {msg.includes("geocoded")
                          ? "The recipient address could not be located on the map. Please check the address details and try again."
                          : "The recipient address falls outside all active delivery zones. Please check the address or contact support."}
                      </p>
                    </div>
                  );
                }

                if (msg.includes("was not found") || msg.includes("not exist")) {
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
                        Something went wrong on the server. Please try again or contact support if the problem persists.
                      </p>
                    </div>
                  );
                }

                return (
                  <p className="text-sm text-destructive">{msg}</p>
                );
              })()}
            </CardContent>
          </Card>
        )}

        {/* Submit */}
        <div className="md:col-span-2 flex justify-end gap-3">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.back()}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={registerParcel.isPending}
          >
            {registerParcel.isPending ? "Registering..." : "Register Parcel"}
          </Button>
        </div>
      </div>
    </form>
  );
}
