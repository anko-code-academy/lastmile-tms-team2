"use client";

import Link from "next/link";
import { startTransition, useState } from "react";
import {
  Clock3,
  ArrowRight,
  LayoutDashboard,
  Map,
  Package2,
  Plus,
  Route as RouteIcon,
  Truck,
  Users,
  Warehouse,
} from "lucide-react";

import { ListPageHeader, ListStatCard } from "@/components/list";
import { Button, buttonVariants } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatParcelStatus, parcelStatusBadgeClass } from "@/lib/labels/parcels";
import { cn } from "@/lib/utils";
import { useDepots } from "@/queries/depots";
import {
  useDepotParcelInventory,
  useDepotParcelInventoryParcels,
} from "@/queries/parcels";
import { useRoutes } from "@/queries/routes";
import { useUsers } from "@/queries/users";
import { useVehicles } from "@/queries/vehicles";
import { useZones } from "@/queries/zones";
import type { GraphQLParcelStatus } from "@/types/parcels";

const inventoryStatuses: GraphQLParcelStatus[] = [
  "RECEIVED_AT_DEPOT",
  "SORTED",
  "STAGED",
  "LOADED",
  "EXCEPTION",
];

const drillDownPageSize = 20;

type InventoryDrillDownState = {
  title: string;
  description: string;
  status: GraphQLParcelStatus | null;
  zoneId: string | null;
  agingOnly: boolean;
  after: string | null;
};

function renderMetricValue(value: number | undefined, isLoading: boolean): string {
  if (isLoading) {
    return "...";
  }

  return (value ?? 0).toLocaleString();
}

function renderMetricHint(
  isLoading: boolean,
  hasError: boolean,
  defaultHint: string,
): string {
  if (hasError) {
    return "Temporarily unavailable";
  }

  if (isLoading) {
    return "Refreshing data";
  }

  return defaultHint;
}

function clampAgingThresholdHours(value: number): number {
  if (!Number.isFinite(value)) {
    return 4;
  }

  return Math.min(72, Math.max(1, Math.round(value)));
}

function deriveAgingThresholdHours(value: string): number {
  if (value.trim() === "") {
    return 4;
  }

  return clampAgingThresholdHours(Number(value));
}

function formatTimestamp(value: string | null | undefined): string {
  if (!value) {
    return "Waiting for first refresh";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatShortTimestamp(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}

function formatAgeMinutes(ageMinutes: number): string {
  return `${ageMinutes.toLocaleString()} min`;
}

function parseCursor(value: string | null | undefined): number {
  const parsed = Number.parseInt(value ?? "", 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
}

function inventoryCardClasses(status: GraphQLParcelStatus): string {
  switch (status) {
    case "RECEIVED_AT_DEPOT":
      return "border-sky-300/60 bg-sky-500/[0.08] text-sky-950 hover:border-sky-400/70 hover:bg-sky-500/[0.14] dark:text-sky-100";
    case "SORTED":
      return "border-teal-300/60 bg-teal-500/[0.08] text-teal-950 hover:border-teal-400/70 hover:bg-teal-500/[0.14] dark:text-teal-100";
    case "STAGED":
      return "border-amber-300/60 bg-amber-500/[0.08] text-amber-950 hover:border-amber-400/70 hover:bg-amber-500/[0.14] dark:text-amber-100";
    case "LOADED":
      return "border-emerald-300/60 bg-emerald-500/[0.08] text-emerald-950 hover:border-emerald-400/70 hover:bg-emerald-500/[0.14] dark:text-emerald-100";
    case "EXCEPTION":
      return "border-rose-300/60 bg-rose-500/[0.08] text-rose-950 hover:border-rose-400/70 hover:bg-rose-500/[0.14] dark:text-rose-100";
    default:
      return "border-border/60 bg-background/80 text-foreground hover:border-border";
  }
}

export function DashboardOverviewClient({
  accessToken,
  displayName,
  isAdmin,
  canViewDepotInventory = true,
}: {
  accessToken: string;
  displayName: string;
  isAdmin: boolean;
  canViewDepotInventory?: boolean;
}) {
  const vehiclesQuery = useVehicles({});
  const activeRoutesQuery = useRoutes({
    status: "IN_PROGRESS",
  });
  const depotsQuery = useDepots();
  const zonesQuery = useZones();
  const usersQuery = useUsers(
    accessToken,
    {},
    { enabled: isAdmin },
  );

  const [agingThresholdInput, setAgingThresholdInput] = useState("4");
  const [drillDown, setDrillDown] = useState<InventoryDrillDownState | null>(null);
  const agingThresholdHours = deriveAgingThresholdHours(agingThresholdInput);
  const agingThresholdMinutes = agingThresholdHours * 60;

  const depotInventoryQuery = useDepotParcelInventory(agingThresholdMinutes);
  const depotInventoryParcelsQuery = useDepotParcelInventoryParcels({
    agingThresholdMinutes,
    status: drillDown?.status ?? null,
    zoneId: drillDown?.zoneId ?? null,
    agingOnly: drillDown?.agingOnly ?? false,
    first: drillDownPageSize,
    after: drillDown?.after ?? null,
    enabled: canViewDepotInventory && Boolean(drillDown),
  });

  const stats = [
    {
      key: "vehicles",
      label: "Fleet",
      value: vehiclesQuery.data?.length,
      isLoading: vehiclesQuery.isLoading,
      hasError: Boolean(vehiclesQuery.error),
      hint: "Registered vehicles across your network",
      accent: "teal" as const,
      icon: <Truck strokeWidth={1.75} />,
      href: "/vehicles",
    },
    {
      key: "active-routes",
      label: "Active routes",
      value: activeRoutesQuery.data?.length,
      isLoading: activeRoutesQuery.isLoading,
      hasError: Boolean(activeRoutesQuery.error),
      hint: "Dispatch runs currently in progress",
      accent: "violet" as const,
      icon: <RouteIcon strokeWidth={1.75} />,
      href: "/routes",
    },
    {
      key: "zones",
      label: "Coverage zones",
      value: zonesQuery.data?.length,
      isLoading: zonesQuery.isLoading,
      hasError: Boolean(zonesQuery.error),
      hint: "Delivery boundaries ready for assignment",
      accent: "sky" as const,
      icon: <Map strokeWidth={1.75} />,
      href: "/zones",
    },
    {
      key: "depots",
      label: "Depots",
      value: depotsQuery.data?.length,
      isLoading: depotsQuery.isLoading,
      hasError: Boolean(depotsQuery.error),
      hint: "Operational hubs connected to the platform",
      accent: "amber" as const,
      icon: <Warehouse strokeWidth={1.75} />,
      href: "/depots",
    },
  ];

  const quickActions = [
    {
      title: "Plan a new route",
      description: "Start dispatch planning with vehicle and driver assignment.",
      href: "/routes/new",
      icon: <RouteIcon strokeWidth={1.75} />,
      cta: "Create route",
    },
    {
      title: "Add a vehicle",
      description: "Expand the fleet registry with capacity and depot details.",
      href: "/vehicles/new",
      icon: <Truck strokeWidth={1.75} />,
      cta: "Add vehicle",
    },
    {
      title: "Review zones",
      description: "Keep coverage boundaries aligned with depots and service areas.",
      href: "/zones",
      icon: <Map strokeWidth={1.75} />,
      cta: "Open zones",
    },
    {
      title: "Manage depots",
      description: "Update hub locations, hours, and operational readiness.",
      href: "/depots",
      icon: <Warehouse strokeWidth={1.75} />,
      cta: "Open depots",
    },
  ];

  const workspaceItems = [
    {
      label: "Logged in as",
      value: displayName,
      hint: isAdmin ? "Administrator access enabled" : "Standard workspace access",
    },
    {
      label: "Users",
      value: isAdmin
        ? renderMetricValue(usersQuery.data?.length, usersQuery.isLoading)
        : "Restricted",
      hint: isAdmin
        ? renderMetricHint(
            usersQuery.isLoading,
            Boolean(usersQuery.error),
            "Accounts currently managed in the system",
          )
        : "Visible in navigation for administrators only",
    },
    {
      label: "Primary focus",
      value: canViewDepotInventory ? "Depot throughput" : "Dispatch and coverage",
      hint: canViewDepotInventory
        ? "Inventory visibility refreshes automatically so stuck parcels surface early"
        : "Routes, vehicles, zones, and depots share the same visual language",
    },
  ];

  const inventorySummary = depotInventoryQuery.data;
  const drillDownData = depotInventoryParcelsQuery.data;
  const previousCursor = drillDownData
    ? Math.max(0, parseCursor(drillDownData.pageInfo.startCursor) - drillDownPageSize)
    : 0;

  const openDrillDown = (request: Omit<InventoryDrillDownState, "after">) => {
    startTransition(() => {
      setDrillDown({
        ...request,
        after: null,
      });
    });
  };

  return (
    <>
      <ListPageHeader
        variant="vehicle"
        eyebrow="Operations"
        title="Dashboard"
        description={`Welcome back, ${displayName}. This overview keeps fleet, dispatch, coverage, and infrastructure within one consistent workspace.`}
        icon={<LayoutDashboard strokeWidth={1.75} aria-hidden />}
        action={
          <div className="flex flex-wrap gap-2">
            <Link
              href="/routes/new"
              className={cn(buttonVariants({ size: "default" }), "gap-2")}
            >
              <Plus className="size-4" aria-hidden />
              New route
            </Link>
            <Link
              href="/vehicles"
              className={cn(buttonVariants({ variant: "outline", size: "default" }), "gap-2")}
            >
              Open fleet
              <ArrowRight className="size-4" aria-hidden />
            </Link>
          </div>
        }
      />

      {canViewDepotInventory ? (
        <section className="mb-8 overflow-hidden rounded-[1.75rem] border border-border/60 bg-[linear-gradient(135deg,oklch(0.98_0.01_220),oklch(0.96_0.02_180)_48%,oklch(0.97_0.03_80))] p-6 shadow-[0_1px_0_0_oklch(0_0_0/0.04),0_20px_56px_-28px_oklch(0.4_0.02_250/0.2)] dark:bg-[linear-gradient(135deg,oklch(0.23_0.02_230),oklch(0.2_0.03_190)_48%,oklch(0.22_0.03_80))]">
          <div className="flex flex-col gap-5 xl:flex-row xl:items-start xl:justify-between">
            <div className="max-w-3xl">
              <div className="inline-flex items-center gap-2 rounded-full border border-border/60 bg-background/75 px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.2em] text-muted-foreground">
                <Package2 className="size-3.5" aria-hidden />
                Depot inventory
              </div>
              <div className="mt-4 flex flex-wrap items-end gap-3">
                <div>
                  <h2 className="text-2xl font-semibold tracking-tight text-foreground">
                    {inventorySummary?.depotName ?? "Depot inventory overview"}
                  </h2>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Live parcel visibility by status, zone, and aging threshold. Refreshes every 60 seconds.
                  </p>
                </div>
                <div className="rounded-full border border-border/60 bg-background/80 px-3 py-1.5 text-sm text-muted-foreground">
                  Last refresh: {formatTimestamp(inventorySummary?.generatedAt)}
                </div>
              </div>
            </div>

            <div className="w-full max-w-xs rounded-2xl border border-border/60 bg-background/80 p-4">
              <div className="space-y-2">
                <Label htmlFor="aging-threshold-hours">Aging threshold hours</Label>
                <Input
                  id="aging-threshold-hours"
                  type="number"
                  min={1}
                  max={72}
                  step={1}
                  value={agingThresholdInput}
                  onChange={(event) => setAgingThresholdInput(event.target.value)}
                  onBlur={() => setAgingThresholdInput(String(agingThresholdHours))}
                />
              </div>
              <p className="mt-2 text-sm text-muted-foreground">
                Parcels beyond this threshold appear in the aging alert and any open drill-down.
              </p>
            </div>
          </div>

          {depotInventoryQuery.error ? (
            <div className="mt-6 rounded-2xl border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
              Depot inventory is temporarily unavailable. Try refreshing the page.
            </div>
          ) : null}

          {!depotInventoryQuery.error && depotInventoryQuery.isLoading && !inventorySummary ? (
            <div className="mt-6 grid gap-4 xl:grid-cols-[minmax(0,1.5fr)_minmax(0,1fr)]">
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                {inventoryStatuses.map((status) => (
                  <div
                    key={status}
                    className="min-h-32 rounded-2xl border border-border/60 bg-background/75 p-4"
                  >
                    <p className="text-sm text-muted-foreground">
                      {formatParcelStatus(status)}
                    </p>
                    <p className="mt-4 text-3xl font-semibold text-foreground">...</p>
                  </div>
                ))}
              </div>
              <div className="grid gap-3">
                <div className="rounded-2xl border border-border/60 bg-background/75 p-4">
                  <p className="text-sm text-muted-foreground">Zone breakdown</p>
                  <p className="mt-4 text-foreground">Refreshing counts...</p>
                </div>
                <div className="rounded-2xl border border-border/60 bg-background/75 p-4">
                  <p className="text-sm text-muted-foreground">Aging alert</p>
                  <p className="mt-4 text-foreground">Refreshing counts...</p>
                </div>
              </div>
            </div>
          ) : null}

          {!depotInventoryQuery.error && !depotInventoryQuery.isLoading && !inventorySummary ? (
            <div className="mt-6 rounded-2xl border border-dashed border-border/70 bg-background/75 p-5">
              <p className="text-base font-semibold text-foreground">
                No depot inventory available
              </p>
              <p className="mt-2 text-sm text-muted-foreground">
                This view appears when your account has an assigned depot. Ask an administrator to confirm the assigned depot for your user profile.
              </p>
            </div>
          ) : null}

          {inventorySummary ? (
            <div className="mt-6 grid gap-4 xl:grid-cols-[minmax(0,1.5fr)_minmax(0,1fr)]">
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                {inventorySummary.statusCounts.map((statusCount) => {
                  const statusLabel = formatParcelStatus(statusCount.status);
                  return (
                    <button
                      key={statusCount.status}
                      type="button"
                      className={cn(
                        "group rounded-2xl border p-4 text-left transition-all duration-200",
                        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50",
                        inventoryCardClasses(statusCount.status as GraphQLParcelStatus),
                      )}
                      onClick={() =>
                        openDrillDown({
                          title: `${statusLabel} parcels`,
                          description: `Filtered to ${statusLabel.toLowerCase()} parcels at ${inventorySummary.depotName}.`,
                          status: statusCount.status as GraphQLParcelStatus,
                          zoneId: null,
                          agingOnly: false,
                        })}
                    >
                      <p className="text-sm font-medium text-current/80">{statusLabel}</p>
                      <p className="mt-4 text-4xl font-semibold tracking-tight">
                        {statusCount.count.toLocaleString()}
                      </p>
                      <p className="mt-3 text-sm text-current/80">Open parcel list</p>
                    </button>
                  );
                })}
              </div>

              <div className="grid gap-3">
                <div className="rounded-2xl border border-border/60 bg-background/80 p-5">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h3 className="text-base font-semibold text-foreground">
                        Zone breakdown
                      </h3>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Counts are sorted by parcel volume, then zone name.
                      </p>
                    </div>
                    <div className="rounded-xl bg-primary/10 p-2 text-primary">
                      <Map className="size-4" aria-hidden />
                    </div>
                  </div>
                  <div className="mt-4 space-y-2">
                    {inventorySummary.zoneCounts.length > 0 ? (
                      inventorySummary.zoneCounts.map((zoneCount) => (
                        <button
                          key={zoneCount.zoneId}
                          type="button"
                          className="flex w-full items-center justify-between rounded-xl border border-border/50 bg-background px-3 py-3 text-left transition hover:border-border hover:bg-background/95 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
                          onClick={() =>
                            openDrillDown({
                              title: `${zoneCount.zoneName} parcels`,
                              description: `Filtered to parcels currently assigned to ${zoneCount.zoneName}.`,
                              status: null,
                              zoneId: zoneCount.zoneId,
                              agingOnly: false,
                            })}
                        >
                          <span className="text-sm font-medium text-foreground">
                            {zoneCount.zoneName}
                          </span>
                          <span className="text-sm font-semibold text-foreground">
                            {zoneCount.count.toLocaleString()}
                          </span>
                        </button>
                      ))
                    ) : (
                      <p className="text-sm text-muted-foreground">
                        No inventory parcels are currently assigned to a depot zone.
                      </p>
                    )}
                  </div>
                </div>

                <button
                  type="button"
                  className="rounded-2xl border border-amber-400/40 bg-amber-500/[0.08] p-5 text-left transition hover:border-amber-500/50 hover:bg-amber-500/[0.14] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
                  onClick={() =>
                    openDrillDown({
                      title: "Aging alert parcels",
                      description: `Parcels at ${inventorySummary.depotName} older than ${agingThresholdHours} hours.`,
                      status: null,
                      zoneId: null,
                      agingOnly: true,
                    })}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-medium text-amber-950/80 dark:text-amber-100/85">
                        Aging alert
                      </p>
                      <p className="mt-3 text-4xl font-semibold tracking-tight text-amber-950 dark:text-amber-50">
                        {inventorySummary.agingAlert.count.toLocaleString()}
                      </p>
                      <p className="mt-2 text-sm text-amber-950/80 dark:text-amber-100/85">
                        Older than {agingThresholdHours} hours
                      </p>
                    </div>
                    <div className="rounded-xl bg-amber-500/15 p-2 text-amber-900 dark:text-amber-100">
                      <Clock3 className="size-4" aria-hidden />
                    </div>
                  </div>
                </button>
              </div>
            </div>
          ) : null}
        </section>
      ) : null}

      <div className="mb-8 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {stats.map((stat) => (
          <Link key={stat.key} href={stat.href} className="block">
            <ListStatCard
              label={stat.label}
              accent={stat.accent}
              icon={stat.icon}
              hint={renderMetricHint(stat.isLoading, stat.hasError, stat.hint)}
              className="h-full"
            >
              <p className="text-[1.65rem] font-semibold tracking-tight text-foreground">
                {renderMetricValue(stat.value, stat.isLoading)}
              </p>
            </ListStatCard>
          </Link>
        ))}
      </div>

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
        <section className="rounded-2xl border border-border/50 bg-card/85 p-6 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)]">
          <div className="mb-5 flex items-start justify-between gap-4">
            <div>
              <h2 className="text-lg font-semibold tracking-tight text-foreground">
                Quick actions
              </h2>
              <p className="mt-1 text-sm text-muted-foreground">
                Jump straight into the flows your team uses most during daily operations.
              </p>
            </div>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            {quickActions.map((action) => (
              <Link
                key={action.href}
                href={action.href}
                className="group rounded-2xl border border-border/50 bg-background/80 p-4 transition-all duration-200 hover:-translate-y-0.5 hover:border-border hover:shadow-md"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex size-11 items-center justify-center rounded-xl bg-primary/8 text-primary ring-1 ring-primary/10">
                    {action.icon}
                  </div>
                  <ArrowRight className="mt-1 size-4 text-muted-foreground transition-transform duration-200 group-hover:translate-x-0.5" />
                </div>
                <h3 className="mt-4 text-base font-semibold text-foreground">
                  {action.title}
                </h3>
                <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                  {action.description}
                </p>
                <p className="mt-4 text-sm font-medium text-primary">
                  {action.cta}
                </p>
              </Link>
            ))}
          </div>
        </section>

        <section className="rounded-2xl border border-border/50 bg-card/85 p-6 shadow-[0_1px_0_0_oklch(0_0_0/0.05),0_16px_48px_-20px_oklch(0.4_0.02_250/0.14)]">
          <div className="mb-5 flex items-start gap-3">
            <div className="flex size-11 items-center justify-center rounded-xl bg-violet-500/12 text-violet-800 ring-1 ring-violet-500/15 dark:bg-violet-400/10 dark:text-violet-200 dark:ring-violet-400/20">
              <Users className="size-5" strokeWidth={1.75} />
            </div>
            <div>
              <h2 className="text-lg font-semibold tracking-tight text-foreground">
                Workspace snapshot
              </h2>
              <p className="mt-1 text-sm text-muted-foreground">
                A quick read on access, team scope, and the main operational surfaces.
              </p>
            </div>
          </div>

          <div className="space-y-3">
            {workspaceItems.map((item) => (
              <div
                key={item.label}
                className="rounded-xl border border-border/45 bg-background/80 px-4 py-3"
              >
                <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-muted-foreground">
                  {item.label}
                </p>
                <p className="mt-2 text-base font-semibold text-foreground">
                  {item.value}
                </p>
                <p className="mt-1 text-sm text-muted-foreground">{item.hint}</p>
              </div>
            ))}
          </div>

          {isAdmin ? (
            <Link
              href="/users"
              className="mt-5 inline-flex items-center gap-2 text-sm font-medium text-primary transition-colors hover:text-primary/80"
            >
              Review user access
              <ArrowRight className="size-4" aria-hidden />
            </Link>
          ) : null}
        </section>
      </div>

      <Dialog
        open={Boolean(drillDown)}
        title={drillDown?.title ?? "Depot parcels"}
        description={
          drillDown ? (
            <div className="space-y-1">
              <p>{drillDown.description}</p>
              <p className="text-xs uppercase tracking-[0.18em] text-muted-foreground">
                Threshold {agingThresholdHours}h
              </p>
            </div>
          ) : undefined
        }
        onClose={() => setDrillDown(null)}
        panelClassName="max-w-5xl"
        footer={
          <>
            <Button
              variant="outline"
              disabled={!drillDownData?.pageInfo.hasPreviousPage}
              onClick={() =>
                setDrillDown((current) =>
                  current
                    ? {
                        ...current,
                        after: previousCursor > 0 ? String(previousCursor) : null,
                      }
                    : current,
                )}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              disabled={!drillDownData?.pageInfo.hasNextPage}
              onClick={() =>
                setDrillDown((current) =>
                  current
                    ? {
                        ...current,
                        after: drillDownData?.pageInfo.endCursor ?? null,
                      }
                    : current,
                )}
            >
              Next
            </Button>
            <Button variant="outline" onClick={() => setDrillDown(null)}>
              Close
            </Button>
          </>
        }
      >
        {drillDown ? (
          <div className="space-y-4">
            {depotInventoryParcelsQuery.isLoading ? (
              <p className="text-sm text-muted-foreground">Loading parcels...</p>
            ) : null}

            {depotInventoryParcelsQuery.error ? (
              <div className="rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
                Unable to load the parcel list right now.
              </div>
            ) : null}

            {drillDownData ? (
              <>
                <div className="flex flex-wrap items-center gap-3 rounded-2xl border border-border/60 bg-background/80 px-4 py-3 text-sm">
                  <span className="font-medium text-foreground">
                    {drillDownData.totalCount.toLocaleString()} parcels
                  </span>
                  {drillDownData.nodes.length > 0 ? (
                    <span className="text-muted-foreground">
                      Showing {parseCursor(drillDownData.pageInfo.startCursor) + 1}
                      {" "}-{" "}
                      {parseCursor(drillDownData.pageInfo.startCursor) + drillDownData.nodes.length}
                    </span>
                  ) : null}
                  {depotInventoryParcelsQuery.isFetching ? (
                    <span className="inline-flex items-center gap-1 text-muted-foreground">
                      <Clock3 className="size-3.5" aria-hidden />
                      Refreshing
                    </span>
                  ) : null}
                </div>

                {drillDownData.nodes.length === 0 ? (
                  <div className="rounded-2xl border border-dashed border-border/60 p-6 text-sm text-muted-foreground">
                    No parcels match this filter.
                  </div>
                ) : (
                  <div className="overflow-hidden rounded-2xl border border-border/60">
                    <div className="overflow-x-auto">
                      <table className="min-w-full divide-y divide-border/60 text-sm">
                        <thead className="bg-muted/35 text-left text-xs uppercase tracking-[0.16em] text-muted-foreground">
                          <tr>
                            <th className="px-4 py-3 font-medium">Tracking</th>
                            <th className="px-4 py-3 font-medium">Status</th>
                            <th className="px-4 py-3 font-medium">Zone</th>
                            <th className="px-4 py-3 font-medium">Age</th>
                            <th className="px-4 py-3 font-medium">Last updated</th>
                            <th className="px-4 py-3 font-medium text-right">Detail</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-border/50 bg-background/90">
                          {drillDownData.nodes.map((parcel) => (
                            <tr key={parcel.id}>
                              <td className="px-4 py-3 font-medium text-foreground">
                                {parcel.trackingNumber}
                              </td>
                              <td className="px-4 py-3">
                                <span className={parcelStatusBadgeClass(parcel.status)}>
                                  {formatParcelStatus(parcel.status)}
                                </span>
                              </td>
                              <td className="px-4 py-3 text-foreground">{parcel.zoneName}</td>
                              <td className="px-4 py-3 text-foreground">
                                {formatAgeMinutes(parcel.ageMinutes)}
                              </td>
                              <td className="px-4 py-3 text-muted-foreground">
                                {formatShortTimestamp(parcel.lastUpdatedAt)}
                              </td>
                              <td className="px-4 py-3 text-right">
                                <Link
                                  href={`/parcels/${parcel.id}`}
                                  className="inline-flex items-center gap-1 font-medium text-primary transition-colors hover:text-primary/80"
                                >
                                  Open
                                  <ArrowRight className="size-3.5" aria-hidden />
                                </Link>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}
              </>
            ) : null}
          </div>
        ) : null}
      </Dialog>
    </>
  );
}
