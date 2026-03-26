"use client";

import { useState } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Mail, Pencil, Plus, Search, UserX, Users } from "lucide-react";
import { toast } from "sonner";

import {
  ListDataTable,
  ListPageHeader,
  ListPageLoading,
  ListPagePagination,
  ListPageStatsStrip,
  listDataTableBodyRowClass,
  listDataTableHeadRowClass,
  listDataTableTdClass,
  listDataTableThClass,
  listDataTableThRightClass,
} from "@/components/list";
import { UserFormModal } from "@/components/common/UserManagement/UserFormModal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { QueryErrorAlert } from "@/components/ui/query-error-alert";
import { useDebounce } from "@/hooks/use-debounce";
import { getErrorMessage } from "@/lib/error-message";
import { cn } from "@/lib/utils";
import { getUserManagementLookups, getUsers, createUser, updateUser, deactivateUser, sendPasswordResetEmail } from "@/services/user-management.service";
import type { UserFormSchema } from "@/lib/validations";
import type {
  CreateUserInput,
  UpdateUserInput,
  UserManagementUser,
  UserRole,
} from "@/types/user-management";

const PAGE_SIZE = 10;

function formatRole(role: UserRole | null) {
  switch (role) {
    case "Admin":
      return "Admin";
    case "OperationsManager":
      return "Operations Manager";
    case "Dispatcher":
      return "Dispatcher";
    case "WarehouseOperator":
      return "Warehouse Operator";
    case "Driver":
      return "Driver";
    default:
      return "Unassigned";
  }
}

type FilterStatus = "ALL" | "ACTIVE" | "INACTIVE";
type DialogState =
  | { mode: "create" }
  | { mode: "edit"; user: UserManagementUser }
  | null;

interface UserManagementClientProps {
  accessToken: string;
}

export function UserManagementClient({
  accessToken,
}: UserManagementClientProps) {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState<"ALL" | UserRole>("ALL");
  const [statusFilter, setStatusFilter] = useState<FilterStatus>("ALL");
  const [depotFilter, setDepotFilter] = useState("ALL");
  const [zoneFilter, setZoneFilter] = useState("ALL");
  const [page, setPage] = useState(1);
  const [dialogState, setDialogState] = useState<DialogState>(null);
  const debouncedSearch = useDebounce(search, 300);

  const lookupsQuery = useQuery({
    queryKey: ["user-management", "lookups"],
    queryFn: () => getUserManagementLookups(accessToken),
  });

  const usersQuery = useQuery({
    queryKey: [
      "user-management",
      "users",
      debouncedSearch,
      roleFilter,
      statusFilter,
      depotFilter,
      zoneFilter,
      page,
    ],
    placeholderData: keepPreviousData,
    queryFn: () =>
      getUsers(accessToken, {
        search: debouncedSearch || undefined,
        role: roleFilter === "ALL" ? undefined : roleFilter,
        isActive:
          statusFilter === "ALL" ? undefined : statusFilter === "ACTIVE",
        depotId: depotFilter === "ALL" ? undefined : depotFilter,
        zoneId: zoneFilter === "ALL" ? undefined : zoneFilter,
        skip: (page - 1) * PAGE_SIZE,
        take: PAGE_SIZE,
      }),
  });

  const createMutation = useMutation({
    mutationFn: (input: CreateUserInput) => createUser(accessToken, input),
    onSuccess: () => {
      toast.success("User created and setup email queued.");
      setDialogState(null);
      queryClient.invalidateQueries({ queryKey: ["user-management", "users"] });
    },
    onError: (error: Error) => {
      toast.error(error.message);
    },
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateUserInput) => updateUser(accessToken, input),
    onSuccess: () => {
      toast.success("User updated.");
      setDialogState(null);
      queryClient.invalidateQueries({ queryKey: ["user-management", "users"] });
    },
    onError: (error: Error) => {
      toast.error(error.message);
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (userId: string) => deactivateUser(accessToken, userId),
    onSuccess: () => {
      toast.success("User deactivated.");
      queryClient.invalidateQueries({ queryKey: ["user-management", "users"] });
    },
    onError: (error: Error) => {
      toast.error(error.message);
    },
  });

  const sendResetMutation = useMutation({
    mutationFn: (userId: string) => sendPasswordResetEmail(accessToken, userId),
    onSuccess: () => {
      toast.success("Password reset email queued.");
    },
    onError: (error: Error) => {
      toast.error(error.message);
    },
  });

  const queryError = lookupsQuery.error ?? usersQuery.error;
  const totalCount = usersQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const from = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const to = totalCount === 0 ? 0 : Math.min(page * PAGE_SIZE, totalCount);
  const zoneOptions = lookupsQuery.data?.zones.filter(
    (zone) => depotFilter === "ALL" || zone.depotId === depotFilter,
  );
  const activeFilters = [
    debouncedSearch ? "Search" : null,
    roleFilter !== "ALL" ? formatRole(roleFilter) : null,
    statusFilter !== "ALL"
      ? statusFilter === "ACTIVE"
        ? "Active"
        : "Inactive"
      : null,
    depotFilter !== "ALL" ? "Depot" : null,
    zoneFilter !== "ALL" ? "Zone" : null,
  ].filter(Boolean);

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleRoleFilterChange(value: string) {
    setRoleFilter(value as "ALL" | UserRole);
    setPage(1);
  }

  function handleStatusFilterChange(value: string) {
    setStatusFilter(value as FilterStatus);
    setPage(1);
  }

  function handleDepotFilterChange(value: string) {
    setDepotFilter(value);
    setPage(1);

    if (
      zoneFilter !== "ALL" &&
      value !== "ALL" &&
      !lookupsQuery.data?.zones.some(
        (zone) => zone.id === zoneFilter && zone.depotId === value,
      )
    ) {
      setZoneFilter("ALL");
    }
  }

  function handleZoneFilterChange(value: string) {
    setZoneFilter(value);
    setPage(1);
  }

  async function handleSubmit(values: UserFormSchema) {
    const normalized = {
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim(),
      email: values.email.trim(),
      phone: values.phone?.trim() || null,
      role: values.role,
      depotId: values.depotId || null,
      zoneId: values.zoneId || null,
    };

    if (dialogState?.mode === "edit") {
      await updateMutation.mutateAsync({
        id: dialogState.user.id,
        isActive: values.isActive,
        ...normalized,
      });
      return;
    }

    await createMutation.mutateAsync(normalized);
  }

  async function handleDeactivate(user: UserManagementUser) {
    if (user.isProtected) {
      return;
    }

    if (!window.confirm(`Deactivate ${user.fullName}?`)) {
      return;
    }

    await deactivateMutation.mutateAsync(user.id);
  }

  if (
    (lookupsQuery.isLoading && !lookupsQuery.data) ||
    (usersQuery.isLoading && !usersQuery.data)
  ) {
    return <ListPageLoading />;
  }

  if (queryError) {
    return (
      <QueryErrorAlert
        title="Could not load users"
        message={getErrorMessage(queryError)}
      />
    );
  }

  return (
    <>
      <ListPageHeader
        variant="route"
        eyebrow="Access"
        title="User Management"
        description="Create, edit, deactivate, and manage access for system users."
        icon={<Users strokeWidth={1.75} aria-hidden />}
        action={
          <Button onClick={() => setDialogState({ mode: "create" })}>
            <Plus className="size-4" aria-hidden />
            New User
          </Button>
        }
      />

      <ListPageStatsStrip
        totalLabel="Total users"
        totalCount={totalCount}
        rangeEntityLabel="users"
        from={from}
        to={to}
        page={page}
        totalPages={totalPages}
        pageSize={PAGE_SIZE}
        filterCardLabel="Filters"
        filterCardHint="Search, role, status, depot, and zone"
        activeFilterDisplay={
          activeFilters.length > 0 ? activeFilters.join(", ") : "All users"
        }
      />

      <div className="mb-6 grid gap-4 rounded-2xl border border-border/50 bg-card/80 p-5 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)] dark:bg-card/60 lg:grid-cols-[2fr_repeat(4,1fr)]">
        <div className="space-y-2">
          <LabelText>Search</LabelText>
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-2.5 size-4 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => handleSearchChange(event.target.value)}
              placeholder="Search by name, email, or phone"
              className="pl-9"
            />
          </div>
        </div>

        <FilterSelect
          label="Role"
          value={roleFilter}
          onChange={handleRoleFilterChange}
          options={[
            { value: "ALL", label: "All roles" },
            ...(lookupsQuery.data?.roles.map((role) => ({
              value: role.value,
              label: role.label,
            })) ?? []),
          ]}
        />

        <FilterSelect
          label="Status"
          value={statusFilter}
          onChange={handleStatusFilterChange}
          options={[
            { value: "ALL", label: "All statuses" },
            { value: "ACTIVE", label: "Active" },
            { value: "INACTIVE", label: "Inactive" },
          ]}
        />

        <FilterSelect
          label="Depot"
          value={depotFilter}
          onChange={handleDepotFilterChange}
          options={[
            { value: "ALL", label: "All depots" },
            ...(lookupsQuery.data?.depots.map((depot) => ({
              value: depot.id,
              label: depot.name,
            })) ?? []),
          ]}
        />

        <FilterSelect
          label="Zone"
          value={zoneFilter}
          onChange={handleZoneFilterChange}
          options={[
            { value: "ALL", label: "All zones" },
            ...(zoneOptions?.map((zone) => ({
              value: zone.id,
              label: zone.name,
            })) ?? []),
          ]}
        />
      </div>

      <ListDataTable minWidthClassName="min-w-[1080px]">
        <thead>
          <tr className={listDataTableHeadRowClass}>
            <th className={listDataTableThClass}>User</th>
            <th className={listDataTableThClass}>Role</th>
            <th className={listDataTableThClass}>Assignment</th>
            <th className={listDataTableThClass}>Status</th>
            <th className={listDataTableThRightClass}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {usersQuery.data?.items.length ? (
            usersQuery.data.items.map((user) => (
              <tr key={user.id} className={listDataTableBodyRowClass}>
                <td className={cn(listDataTableTdClass, "align-top")}>
                  <div className="flex items-center gap-2">
                    <div className="font-medium">{user.fullName}</div>
                    {user.isProtected ? (
                      <span className="inline-flex rounded-full bg-sky-100 px-2.5 py-1 text-xs font-medium text-sky-700">
                        System admin
                      </span>
                    ) : null}
                  </div>
                  <div className="text-muted-foreground">{user.email}</div>
                  {user.phone ? (
                    <div className="text-muted-foreground">{user.phone}</div>
                  ) : null}
                </td>
                <td className={cn(listDataTableTdClass, "align-top")}>
                  {formatRole(user.role)}
                </td>
                <td className={cn(listDataTableTdClass, "align-top")}>
                  <div>{user.depotName ?? "No depot"}</div>
                  <div className="text-muted-foreground">
                    {user.zoneName ?? "No zone"}
                  </div>
                </td>
                <td className={cn(listDataTableTdClass, "align-top")}>
                  <span
                    className={
                      user.isActive
                        ? "inline-flex rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-medium text-emerald-700"
                        : "inline-flex rounded-full bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-700"
                    }
                  >
                    {user.isActive ? "Active" : "Inactive"}
                  </span>
                </td>
                <td className={cn(listDataTableTdClass, "min-w-[270px] text-right align-top")}>
                  <div className="flex justify-end gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setDialogState({ mode: "edit", user })}
                      disabled={user.isProtected}
                    >
                      <Pencil className="size-4" aria-hidden />
                      Edit
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => sendResetMutation.mutate(user.id)}
                      disabled={user.isProtected || sendResetMutation.isPending}
                    >
                      <Mail className="size-4" aria-hidden />
                      Reset Email
                    </Button>
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleDeactivate(user)}
                      disabled={
                        user.isProtected ||
                        !user.isActive ||
                        deactivateMutation.isPending
                      }
                    >
                      <UserX className="size-4" aria-hidden />
                      Deactivate
                    </Button>
                  </div>
                </td>
              </tr>
            ))
          ) : (
            <tr className={listDataTableBodyRowClass}>
              <td className={listDataTableTdClass} colSpan={5}>
                <div className="py-8 text-center text-muted-foreground">
                  No users matched the current filters.
                </div>
              </td>
            </tr>
          )}
        </tbody>
      </ListDataTable>

      <ListPagePagination
        page={page}
        totalPages={totalPages}
        setPage={setPage}
      />

      <UserFormModal
        isOpen={dialogState !== null}
        mode={dialogState?.mode ?? "create"}
        lookups={
          lookupsQuery.data ?? {
            roles: [],
            depots: [],
            zones: [],
          }
        }
        user={dialogState?.mode === "edit" ? dialogState.user : null}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
        onClose={() => setDialogState(null)}
        onSubmit={handleSubmit}
      />
    </>
  );
}

function LabelText({ children }: { children: React.ReactNode }) {
  return <p className="text-sm font-medium text-foreground">{children}</p>;
}

function FilterSelect({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: Array<{ value: string; label: string }>;
}) {
  return (
    <div className="space-y-2">
      <LabelText>{label}</LabelText>
      <select
        aria-label={label}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="flex h-10 w-full rounded-xl border border-input/90 bg-background px-3 text-sm outline-none focus:border-ring focus:ring-2 focus:ring-ring/40"
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  );
}
