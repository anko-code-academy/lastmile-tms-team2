"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, UserCircle, X } from "lucide-react";

import {
  DetailBreadcrumb,
  DetailFormField,
  DetailFormPageShell,
  DetailPanel,
  FormActionsBar,
  FORM_PAGE_FORM_COLUMN_CLASS,
} from "@/components/detail";
import { ListPageHeader } from "@/components/list";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { DatePicker } from "@/components/form/date-picker";
import { SelectDropdown } from "@/components/form/select-dropdown";
import { cn } from "@/lib/utils";
import { depotSelectOptions } from "@/lib/forms/depots";
import { zoneSelectOptions } from "@/lib/forms/zones";
import { userSelectOptions } from "@/lib/forms/users";
import { API_RESOURCE_LOAD_ERROR } from "@/lib/network/api-messages";
import {
  DRIVER_STATUS_SELECT_OPTIONS,
  DAY_OF_WEEK_SELECT_OPTIONS,
} from "@/lib/labels/drivers";
import {
  driverCreateFormSchema,
  type DriverCreateFormValues,
} from "@/lib/validation/drivers";
import { zodErrorToFieldMap } from "@/lib/validation/zod-field-errors";
import { useDepots } from "@/queries/depots";
import { useZones } from "@/queries/zones";
import { useUsersForSelect } from "@/queries/users";
import { useCreateDriver, useUpdateDriver } from "@/queries/drivers";
import { toDateInputValue } from "@/lib/datetime/graphql-datetime";
import { absoluteApiAssetUrl } from "@/lib/network/api";
import { uploadDriverPhoto } from "@/lib/network/driver-photo-upload";
import type { Driver } from "@/types/drivers";
import { DriverAvailabilityForm } from "@/components/drivers/driver-availability-form";
import type { DayOfWeek } from "@/graphql/generated";

interface DriverFormProps {
  mode: "create" | "edit";
  driver?: Driver;
}

interface AvailabilityEntry {
  id?: string | null;
  dayOfWeek: DayOfWeek;
  shiftStart: string;
  shiftEnd: string;
  isAvailable: boolean;
}

interface DriverFormData {
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  licenseNumber: string;
  licenseExpiryDate: string;
  zoneId: string;
  depotId: string;
  status: string;
  userId: string;
  availabilitySchedule: AvailabilityEntry[];
}

interface DriverFormPhotoAreaProps {
  photoPreview: string | null;
  serverPhotoUrl: string | undefined;
  onRemove: () => void;
}

/** Isolated state so `key` on the parent resets load error when URL/preview changes (no setState in effects). */
function DriverFormPhotoArea({
  photoPreview,
  serverPhotoUrl,
  onRemove,
}: DriverFormPhotoAreaProps) {
  const [photoLoadError, setPhotoLoadError] = useState(false);
  const serverSrc = absoluteApiAssetUrl(serverPhotoUrl) ?? "";

  return (
    <div className="flex shrink-0 flex-col gap-1.5 sm:max-w-44">
      <div className="relative size-24 shrink-0">
        {photoPreview ? (
          <Image
            src={photoPreview}
            alt=""
            width={96}
            height={96}
            sizes="96px"
            unoptimized
            className="rounded-full border object-cover"
          />
        ) : photoLoadError ? (
          <div
            className="flex size-24 items-center justify-center rounded-full border border-dashed border-muted-foreground/35 bg-muted/40 text-muted-foreground"
            role="img"
            aria-label="Photo file not found on server"
          >
            <UserCircle
              className="h-12 w-12 shrink-0 opacity-80"
              strokeWidth={1.25}
              aria-hidden
            />
          </div>
        ) : (
          <Image
            src={serverSrc}
            alt=""
            width={96}
            height={96}
            sizes="96px"
            unoptimized
            className="rounded-full border object-cover"
            onError={() => setPhotoLoadError(true)}
          />
        )}
        <Button
          type="button"
          variant="secondary"
          size="icon"
          className="absolute -end-1 -top-1 size-8 rounded-full border border-border shadow-sm"
          onClick={onRemove}
          aria-label="Remove photo"
        >
          <X className="size-4" aria-hidden />
        </Button>
      </div>
      {photoLoadError && !photoPreview && (
        <p className="text-xs leading-snug text-muted-foreground">
          File missing — upload a new photo
        </p>
      )}
    </div>
  );
}

export function DriverForm({ mode, driver }: DriverFormProps) {
  const router = useRouter();
  const createDriver = useCreateDriver();
  const updateDriver = useUpdateDriver();

  const { data: depots = [], isLoading: depotsLoading, error: depotsError } = useDepots();
  const { data: zones = [], isLoading: zonesLoading, error: zonesError } = useZones();
  const { data: users = [], isLoading: usersLoading, error: usersError } = useUsersForSelect();

  const depotOptions = useMemo(() => depotSelectOptions(depots), [depots]);
  const zoneOptions = useMemo(() => zoneSelectOptions(zones), [zones]);
  const userOptions = useMemo(() => userSelectOptions(users), [users]);

  const defaultAvailability = useMemo(() => {
    if (driver?.availabilitySchedule?.length) {
      return driver.availabilitySchedule.map((a) => ({
        id: a.id,
        dayOfWeek: a.dayOfWeek,
        shiftStart: a.shiftStart ?? "",
        shiftEnd: a.shiftEnd ?? "",
        isAvailable: a.isAvailable,
      }));
    }
    return DAY_OF_WEEK_SELECT_OPTIONS.map((d) => ({
      id: undefined,
      dayOfWeek: d.value as DayOfWeek,
      shiftStart: "08:00:00",
      shiftEnd: "17:00:00",
      isAvailable: true,
    }));
  }, [driver]);

  const [formData, setFormData] = useState<DriverFormData>({
    firstName: driver?.firstName ?? "",
    lastName: driver?.lastName ?? "",
    phone: driver?.phone ?? "",
    email: driver?.email ?? "",
    licenseNumber: driver?.licenseNumber ?? "",
    licenseExpiryDate: toDateInputValue(driver?.licenseExpiryDate),
    zoneId: driver?.zoneId ?? "",
    depotId: driver?.depotId ?? "",
    status: driver?.status ?? "ACTIVE",
    userId: driver?.userId ?? "",
    availabilitySchedule: defaultAvailability,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);
  /** User removed existing server photo or cleared a new selection — submit sends photoUrl: null when no new file. */
  const [photoRemoved, setPhotoRemoved] = useState(false);
  const previewObjectUrlRef = useRef<string | null>(null);
  const photoInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    return () => {
      if (previewObjectUrlRef.current) {
        URL.revokeObjectURL(previewObjectUrlRef.current);
      }
    };
  }, []);

  const clearError = (key: string) => {
    setErrors((prev) => {
      if (prev[key] === undefined) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  };

  const isPending = mode === "create" ? createDriver.isPending : updateDriver.isPending;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const parsed = driverCreateFormSchema.safeParse({
      ...formData,
      licenseExpiryDate: formData.licenseExpiryDate || null,
    });

    if (!parsed.success) {
      setErrors(zodErrorToFieldMap(parsed.error));
      return;
    }
    setErrors({});

    const payload: DriverCreateFormValues = {
      ...parsed.data,
      phone: parsed.data.phone?.trim() || null,
      email: parsed.data.email?.trim() || null,
    };

    let photoUrl: string | null = driver?.photoUrl ?? null;
    if (photoFile) {
      photoUrl = await uploadDriverPhoto(photoFile);
    } else if (photoRemoved) {
      photoUrl = null;
    }

    try {
      if (mode === "create") {
        await createDriver.mutateAsync({ ...payload, photoUrl });
        router.push("/drivers");
      } else if (driver) {
        await updateDriver.mutateAsync({
          id: driver.id,
          data: { ...payload, photoUrl },
        });
        router.push("/drivers");
      }
    } catch {
      /* error toast from global MutationCache */
    }
  };

  const pageTitle = mode === "create" ? "Add driver" : "Edit driver";
  const pageDescription =
    mode === "create"
      ? "Register a new driver with zone and depot assignments."
      : `Editing driver profile for ${driver?.displayName}.`;

  return (
    <DetailFormPageShell variant="driver">
      <DetailBreadcrumb
        className="form-page-breadcrumb-animate"
        variant="driver"
        items={[
          { label: "Drivers", href: "/drivers" },
          { label: mode === "create" ? "New driver" : driver?.displayName ?? "Edit" },
        ]}
      />

      <ListPageHeader
        eyebrow="Drivers"
        title={pageTitle}
        description={pageDescription}
        icon={<UserCircle strokeWidth={1.75} />}
        action={
          <Link
            href="/drivers"
            className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
          >
            <ArrowLeft className="mr-2 size-4" aria-hidden />
            All drivers
          </Link>
        }
      />

      <div className={cn(FORM_PAGE_FORM_COLUMN_CLASS, "form-page-body-animate")}>
        <form onSubmit={handleSubmit} className="space-y-6">
          <DetailPanel
            className="form-page-panel-animate"
            section="driver"
            title="Personal details"
            description="Name, contact information, and license credentials."
          >
            <div className="space-y-6">
              <div className="grid gap-6 sm:grid-cols-2">
                <DetailFormField
                  label="First name"
                  htmlFor="firstName"
                  error={errors.firstName}
                >
                  <Input
                    id="firstName"
                    type="text"
                    autoComplete="off"
                    value={formData.firstName}
                    aria-invalid={errors.firstName ? true : undefined}
                    onChange={(e) => {
                      clearError("firstName");
                      setFormData({ ...formData, firstName: e.target.value });
                    }}
                  />
                </DetailFormField>

                <DetailFormField
                  label="Last name"
                  htmlFor="lastName"
                  error={errors.lastName}
                >
                  <Input
                    id="lastName"
                    type="text"
                    autoComplete="off"
                    value={formData.lastName}
                    aria-invalid={errors.lastName ? true : undefined}
                    onChange={(e) => {
                      clearError("lastName");
                      setFormData({ ...formData, lastName: e.target.value });
                    }}
                  />
                </DetailFormField>
              </div>

              <div className="grid gap-6 sm:grid-cols-2">
                <DetailFormField
                  label="Phone"
                  htmlFor="phone"
                  error={errors.phone}
                >
                  <Input
                    id="phone"
                    type="tel"
                    autoComplete="tel"
                    value={formData.phone}
                    onChange={(e) => {
                      clearError("phone");
                      setFormData({ ...formData, phone: e.target.value });
                    }}
                  />
                </DetailFormField>

                <DetailFormField
                  label="Email"
                  htmlFor="email"
                  error={errors.email}
                >
                  <Input
                    id="email"
                    type="email"
                    autoComplete="email"
                    value={formData.email}
                    aria-invalid={errors.email ? true : undefined}
                    onChange={(e) => {
                      clearError("email");
                      setFormData({ ...formData, email: e.target.value });
                    }}
                  />
                </DetailFormField>
              </div>

              <div className="grid gap-6 sm:grid-cols-2">
                <DetailFormField
                  label="License number"
                  htmlFor="licenseNumber"
                  error={errors.licenseNumber}
                >
                  <Input
                    id="licenseNumber"
                    type="text"
                    autoComplete="off"
                    value={formData.licenseNumber}
                    aria-invalid={errors.licenseNumber ? true : undefined}
                    onChange={(e) => {
                      clearError("licenseNumber");
                      setFormData({ ...formData, licenseNumber: e.target.value });
                    }}
                  />
                </DetailFormField>

                <DetailFormField
                  label="License expiry"
                  htmlFor="licenseExpiryDate"
                  error={errors.licenseExpiryDate}
                >
                  <DatePicker
                    id="licenseExpiryDate"
                    value={formData.licenseExpiryDate}
                    onChange={(v) => {
                      clearError("licenseExpiryDate");
                      setFormData({ ...formData, licenseExpiryDate: v });
                    }}
                    invalid={!!errors.licenseExpiryDate}
                    aria-invalid={errors.licenseExpiryDate ? true : undefined}
                    emptyLabel="Select expiry date"
                  />
                </DetailFormField>
              </div>

              <DetailFormField
                label="Photo"
                htmlFor="driver-photo"
                description="JPG, PNG, WebP, or GIF — max 5 MB. Stored on the server."
                error={errors.photo}
              >
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
                  {(photoPreview ||
                    (!photoRemoved && driver?.photoUrl)) && (
                    <DriverFormPhotoArea
                      key={`${driver?.id ?? "new"}-${driver?.photoUrl ?? ""}-${photoPreview ?? ""}-${photoRemoved}`}
                      photoPreview={photoPreview}
                      serverPhotoUrl={driver?.photoUrl}
                      onRemove={() => {
                        clearError("photo");
                        if (previewObjectUrlRef.current) {
                          URL.revokeObjectURL(previewObjectUrlRef.current);
                          previewObjectUrlRef.current = null;
                        }
                        setPhotoFile(null);
                        setPhotoPreview(null);
                        setPhotoRemoved(true);
                        if (photoInputRef.current) {
                          photoInputRef.current.value = "";
                        }
                      }}
                    />
                  )}
                  <Input
                    ref={photoInputRef}
                    id="driver-photo"
                    type="file"
                    accept="image/jpeg,image/png,image/webp,image/gif"
                    className="cursor-pointer text-sm file:font-medium"
                    onChange={(e) => {
                      const f = e.target.files?.[0];
                      if (!f) {
                        setPhotoFile(null);
                        setPhotoPreview(null);
                        return;
                      }
                      if (!/^image\/(jpeg|png|gif|webp)$/i.test(f.type)) {
                        setErrors((prev) => ({
                          ...prev,
                          photo: "Use JPG, PNG, WebP, or GIF.",
                        }));
                        return;
                      }
                      if (f.size > 5 * 1024 * 1024) {
                        setErrors((prev) => ({
                          ...prev,
                          photo: "Maximum file size is 5 MB.",
                        }));
                        return;
                      }
                      clearError("photo");
                      setPhotoRemoved(false);
                      if (previewObjectUrlRef.current) {
                        URL.revokeObjectURL(previewObjectUrlRef.current);
                      }
                      previewObjectUrlRef.current = URL.createObjectURL(f);
                      setPhotoFile(f);
                      setPhotoPreview(previewObjectUrlRef.current);
                    }}
                  />
                </div>
              </DetailFormField>
            </div>
          </DetailPanel>

          <DetailPanel
            className="form-page-panel-animate"
            section="driver"
            title="Assignment"
            description="Zone, depot, and user account linking."
          >
            <div className="space-y-6">
              <DetailFormField
                label="Zone"
                htmlFor="zone"
                error={errors.zoneId}
              >
                <SelectDropdown
                  id="zone"
                  options={zoneOptions}
                  value={formData.zoneId}
                  invalid={!!errors.zoneId}
                  onChange={(v) => {
                    clearError("zoneId");
                    setFormData({ ...formData, zoneId: v });
                  }}
                  placeholder={zonesLoading ? "Loading zones" : "Select zone"}
                />
                {zonesError && (
                  <p className="text-xs text-destructive">
                    Could not load zones. {API_RESOURCE_LOAD_ERROR}
                  </p>
                )}
              </DetailFormField>

              <DetailFormField
                label="Depot"
                htmlFor="depot"
                error={errors.depotId}
              >
                <SelectDropdown
                  id="depot"
                  options={depotOptions}
                  value={formData.depotId}
                  invalid={!!errors.depotId}
                  onChange={(v) => {
                    clearError("depotId");
                    setFormData({ ...formData, depotId: v });
                  }}
                  placeholder={depotsLoading ? "Loading depots" : "Select depot"}
                />
                {depotsError && (
                  <p className="text-xs text-destructive">
                    Could not load depots. {API_RESOURCE_LOAD_ERROR}
                  </p>
                )}
              </DetailFormField>

              <DetailFormField
                label="User account"
                htmlFor="user"
                description="Link driver to a user account for mobile app access."
                error={errors.userId}
              >
                <SelectDropdown
                  id="user"
                  options={userOptions}
                  value={formData.userId}
                  invalid={!!errors.userId}
                  onChange={(v) => {
                    clearError("userId");
                    setFormData({ ...formData, userId: v });
                  }}
                  placeholder={usersLoading ? "Loading users" : "Select user"}
                />
                {usersError && (
                  <p className="text-xs text-destructive">
                    Could not load users. {API_RESOURCE_LOAD_ERROR}
                  </p>
                )}
              </DetailFormField>

              <DetailFormField
                label="Status"
                htmlFor="status"
                error={errors.status}
              >
                <SelectDropdown
                  id="status"
                  options={DRIVER_STATUS_SELECT_OPTIONS}
                  value={formData.status}
                  invalid={!!errors.status}
                  onChange={(v) => {
                    clearError("status");
                    setFormData({ ...formData, status: v as typeof formData.status });
                  }}
                />
              </DetailFormField>
            </div>
          </DetailPanel>

          <DetailPanel
            className="form-page-panel-animate"
            section="driver"
            title="Availability Schedule"
            description="Set weekly working hours and days off."
          >
            <DriverAvailabilityForm
              value={formData.availabilitySchedule}
              onChange={(schedule) => {
                clearError("availabilitySchedule");
                setFormData({ ...formData, availabilitySchedule: schedule });
              }}
              errors={errors}
            />
          </DetailPanel>

          <FormActionsBar>
            <Link
              href="/drivers"
              className={cn(
                buttonVariants({ variant: "outline", size: "default" }),
                "w-full justify-center sm:w-auto",
              )}
            >
              Cancel
            </Link>
            <Button
              type="submit"
              className="w-full sm:w-auto"
              disabled={isPending}
            >
              {isPending
                ? mode === "create"
                  ? "Creating"
                  : "Saving"
                : mode === "create"
                  ? "Create driver"
                  : "Save changes"}
            </Button>
          </FormActionsBar>
        </form>
      </div>
    </DetailFormPageShell>
  );
}
