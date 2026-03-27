"use client";

import { useState } from "react";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { userFormSchema, type UserFormSchema } from "@/lib/validation/users";
import type {
  UserRole,
  UserManagementLookups,
  UserManagementUser,
} from "@/types/users";

interface UserFormModalProps {
  isOpen: boolean;
  mode: "create" | "edit";
  lookups: UserManagementLookups;
  user?: UserManagementUser | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (values: UserFormSchema) => Promise<void>;
}

type UserFormErrors = Partial<Record<keyof UserFormSchema, string>>;

function getDefaultValues(user?: UserManagementUser | null): UserFormSchema {
  return {
    firstName: user?.firstName ?? "",
    lastName: user?.lastName ?? "",
    email: user?.email ?? "",
    phone: user?.phone ?? "",
    role: (user?.role ?? "Dispatcher") as UserRole,
    depotId: user?.depotId ?? "",
    zoneId: user?.zoneId ?? "",
    isActive: user?.isActive ?? true,
  };
}

function getValidationErrors(values: UserFormSchema): UserFormErrors {
  const result = userFormSchema.safeParse(values);

  if (result.success) {
    return {};
  }

  const fieldErrors = result.error.flatten().fieldErrors;

  return {
    firstName: fieldErrors.firstName?.[0],
    lastName: fieldErrors.lastName?.[0],
    email: fieldErrors.email?.[0],
    phone: fieldErrors.phone?.[0],
    role: fieldErrors.role?.[0],
    depotId: fieldErrors.depotId?.[0],
    zoneId: fieldErrors.zoneId?.[0],
    isActive: fieldErrors.isActive?.[0],
  };
}

export function UserFormModal({
  isOpen,
  mode,
  lookups,
  user,
  isSubmitting,
  onClose,
  onSubmit,
}: UserFormModalProps) {
  const inputClassName =
    "h-9 w-full min-w-0 rounded-md border border-input bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none selection:bg-primary selection:text-primary-foreground placeholder:text-muted-foreground disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 aria-invalid:border-destructive aria-invalid:ring-destructive/20";

  if (!isOpen) {
    return null;
  }

  return (
    <div
      data-testid="user-form-modal"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
    >
      <div className="w-full max-w-2xl rounded-2xl border border-border bg-background shadow-2xl">
        <div className="flex items-center justify-between border-b border-border px-6 py-4">
          <div>
            <h2 className="text-lg font-semibold">
              {mode === "create" ? "Create User" : "Edit User"}
            </h2>
            <p className="text-sm text-muted-foreground">
              {mode === "create"
                ? "Add a new user account and send a setup email."
                : "Update user details, access, and assignments."}
            </p>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Close"
          >
            <X className="size-4" />
          </Button>
        </div>

        <UserFormModalContent
          key={`${mode}:${user?.id ?? "create"}`}
          mode={mode}
          lookups={lookups}
          user={user}
          isSubmitting={isSubmitting}
          onClose={onClose}
          onSubmit={onSubmit}
          inputClassName={inputClassName}
        />
      </div>
    </div>
  );
}

function UserFormModalContent({
  mode,
  lookups,
  user,
  isSubmitting,
  onClose,
  onSubmit,
  inputClassName,
}: Pick<
  UserFormModalProps,
  "mode" | "lookups" | "user" | "isSubmitting" | "onClose" | "onSubmit"
> & { inputClassName: string }) {
  const [values, setValues] = useState<UserFormSchema>(() => getDefaultValues(user));
  const [errors, setErrors] = useState<UserFormErrors>({});

  const filteredZones = lookups.zones.filter(
    (zone) => !values.depotId || zone.depotId === values.depotId
  );
  const zoneValue = filteredZones.some((zone) => zone.id === values.zoneId)
    ? values.zoneId
    : "";

  function setFieldValue<K extends keyof UserFormSchema>(
    field: K,
    value: UserFormSchema[K]
  ) {
    setValues((current) => ({
      ...current,
      [field]: value,
    }));

    setErrors((current) => {
      if (!current[field]) {
        return current;
      }

      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  async function handleFormSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nextValues = {
      ...values,
      zoneId: zoneValue,
    };
    const nextErrors = getValidationErrors(nextValues);
    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors);
      return;
    }

    setErrors({});
    await onSubmit(nextValues);
  }

  return (
    <form onSubmit={handleFormSubmit} className="space-y-6 px-6 py-5">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="firstName">First Name</Label>
              <input
                id="firstName"
                className={inputClassName}
                value={values.firstName}
                onChange={(event) =>
                  setFieldValue("firstName", event.target.value)
                }
                aria-invalid={!!errors.firstName}
              />
              {errors.firstName && (
                <p className="text-sm text-destructive">{errors.firstName}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="lastName">Last Name</Label>
              <input
                id="lastName"
                className={inputClassName}
                value={values.lastName}
                onChange={(event) => setFieldValue("lastName", event.target.value)}
                aria-invalid={!!errors.lastName}
              />
              {errors.lastName && (
                <p className="text-sm text-destructive">{errors.lastName}</p>
              )}
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <input
                id="email"
                type="email"
                className={inputClassName}
                value={values.email}
                onChange={(event) => setFieldValue("email", event.target.value)}
                aria-invalid={!!errors.email}
              />
              {errors.email && (
                <p className="text-sm text-destructive">{errors.email}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="phone">Phone</Label>
              <input
                id="phone"
                className={inputClassName}
                value={values.phone}
                onChange={(event) => setFieldValue("phone", event.target.value)}
                aria-invalid={!!errors.phone}
              />
              {errors.phone && (
                <p className="text-sm text-destructive">{errors.phone}</p>
              )}
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="role">Role</Label>
              <select
                id="role"
                value={values.role}
                onChange={(event) =>
                  setFieldValue("role", event.target.value as UserFormSchema["role"])
                }
                className="flex h-8 w-full rounded-lg border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/40"
                aria-invalid={!!errors.role}
              >
                {lookups.roles.map((role) => (
                  <option key={role.value} value={role.value}>
                    {role.label}
                  </option>
                ))}
              </select>
              {errors.role && (
                <p className="text-sm text-destructive">{errors.role}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="depotId">Depot</Label>
              <select
                id="depotId"
                value={values.depotId}
                onChange={(event) => {
                  const nextDepotId = event.target.value;
                  const nextZoneId = lookups.zones.some(
                    (zone) =>
                      zone.id === values.zoneId &&
                      zone.depotId === nextDepotId
                  )
                    ? values.zoneId
                    : "";

                  setValues((current) => ({
                    ...current,
                    depotId: nextDepotId,
                    zoneId: nextZoneId,
                  }));

                  setErrors((current) => {
                    if (!current.depotId && !current.zoneId) {
                      return current;
                    }

                    const next = { ...current };
                    delete next.depotId;
                    delete next.zoneId;
                    return next;
                  });
                }}
                className="flex h-8 w-full rounded-lg border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/40"
                aria-invalid={!!errors.depotId}
              >
                <option value="">Unassigned</option>
                {lookups.depots.map((depot) => (
                  <option key={depot.id} value={depot.id}>
                    {depot.name}
                  </option>
                ))}
              </select>
              {errors.depotId && (
                <p className="text-sm text-destructive">{errors.depotId}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="zoneId">Zone</Label>
              <select
                id="zoneId"
                value={zoneValue}
                onChange={(event) => setFieldValue("zoneId", event.target.value)}
                className="flex h-8 w-full rounded-lg border border-border bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/40"
                aria-invalid={!!errors.zoneId}
              >
                <option value="">Unassigned</option>
                {filteredZones.map((zone) => (
                  <option key={zone.id} value={zone.id}>
                    {zone.name}
                  </option>
                ))}
              </select>
              {errors.zoneId && (
                <p className="text-sm text-destructive">{errors.zoneId}</p>
              )}
            </div>
          </div>

          <label className="flex items-center gap-3 rounded-xl border border-border bg-muted/40 px-4 py-3 text-sm">
            <input
              type="checkbox"
              checked={values.isActive}
              onChange={(event) =>
                setFieldValue("isActive", event.target.checked)
              }
              className="size-4 rounded border-border"
            />
            User is active and can sign in
          </label>

          <div className="flex justify-end gap-3">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting
                ? "Saving..."
                : mode === "create"
                  ? "Create User"
                  : "Save Changes"}
            </Button>
          </div>
    </form>
  );
}
