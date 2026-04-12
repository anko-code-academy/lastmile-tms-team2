import type { LucideIcon } from "lucide-react";
import {
  Boxes,
  Building2,
  LayoutDashboard,
  Map,
  Package,
  Route,
  Truck,
  UserCircle,
  Users,
} from "lucide-react";
import {
  DRIVER_ROLE,
  ROUTE_MANAGER_ROLES,
} from "@/lib/routes/access";

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
  { href: "/drivers", label: "Drivers", icon: UserCircle },
  {
    href: "/routes",
    label: "Routes",
    icon: Route,
    requiredRoles: [...ROUTE_MANAGER_ROLES],
  },
  {
    href: "/routes/my",
    label: "My Schedule",
    icon: Route,
    requiredRoles: [DRIVER_ROLE],
  },
  { href: "/zones", label: "Zones", icon: Map },
  { href: "/depots", label: "Depots", icon: Building2 },
  {
    href: "/bin-locations",
    label: "Bin Locations",
    icon: Boxes,
    requiredRoles: ["Admin", "OperationsManager"],
  },
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

  if (href === "/routes/my") {
    return pathname === href || /^\/routes\/[^/]+$/.test(pathname);
  }

  return pathname === href || pathname.startsWith(`${href}/`);
}
