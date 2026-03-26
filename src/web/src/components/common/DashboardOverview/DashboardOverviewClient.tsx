"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowRight,
  LayoutDashboard,
  Map,
  Plus,
  Route as RouteIcon,
  Truck,
  Users,
  Warehouse,
} from "lucide-react";

import { ListPageHeader, ListStatCard } from "@/components/list";
import { buttonVariants } from "@/components/ui/button";
import { useDepots } from "@/lib/hooks/useDepots";
import { useZones } from "@/lib/hooks/useZones";
import { cn } from "@/lib/utils";
import { useRoutes } from "@/queries/routes";
import { useVehicles } from "@/queries/vehicles";
import { getUsers } from "@/services/user-management.service";
import { RouteStatus } from "@/types/routes";

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

export function DashboardOverviewClient({
  accessToken,
  displayName,
  isAdmin,
}: {
  accessToken: string;
  displayName: string;
  isAdmin: boolean;
}) {
  const vehiclesQuery = useVehicles({ page: 1, pageSize: 1 });
  const activeRoutesQuery = useRoutes({
    page: 1,
    pageSize: 1,
    status: RouteStatus.InProgress,
  });
  const depotsQuery = useDepots();
  const zonesQuery = useZones();
  const usersQuery = useQuery({
    queryKey: ["dashboard", "users-total"],
    queryFn: () => getUsers(accessToken, { skip: 0, take: 1 }),
    enabled: isAdmin,
  });

  const stats = [
    {
      key: "vehicles",
      label: "Fleet",
      value: vehiclesQuery.data?.totalCount,
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
      value: activeRoutesQuery.data?.totalCount,
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
        ? renderMetricValue(usersQuery.data?.totalCount, usersQuery.isLoading)
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
      value: "Dispatch and coverage",
      hint: "Routes, vehicles, zones, and depots share the same new visual language",
    },
  ];

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
    </>
  );
}
