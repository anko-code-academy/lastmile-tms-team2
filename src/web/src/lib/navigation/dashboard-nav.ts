import type { LucideIcon } from "lucide-react";
import {
  Building2,
  LayoutDashboard,
  Map,
  Package,
  Route,
  Truck,
  Users,
} from "lucide-react";

export type DashboardNavItem = {
  href: string;
  label: string;
  icon: LucideIcon;
  requiredRoles?: string[];
};

export const dashboardNavItems: readonly DashboardNavItem[] = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/parcels", label: "Parcels", icon: Package },
  { href: "/users", label: "Users", icon: Users, requiredRoles: ["Admin"] },
  { href: "/vehicles", label: "Vehicles", icon: Truck },
  { href: "/routes", label: "Routes", icon: Route },
  { href: "/zones", label: "Zones", icon: Map },
  { href: "/depots", label: "Depots", icon: Building2 },
] as const;

export function getDashboardNavItems(userRoles?: string[]) {
  return dashboardNavItems.filter((item) => {
    if (!item.requiredRoles || item.requiredRoles.length === 0) {
      return true;
    }

    return item.requiredRoles.some((role) => userRoles?.includes(role));
  });
}

export function isDashboardNavActive(pathname: string, href: string): boolean {
  if (href === "/dashboard") {
    return pathname === "/dashboard";
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}
