import { describe, expect, it } from "vitest";

import {
  getDashboardNavItems,
  isDashboardNavActive,
} from "@/lib/dashboard-nav";

describe("dashboard navigation", () => {
  it("shows admin-only links only for admins", () => {
    const adminItems = getDashboardNavItems(["Admin"]).map((item) => item.href);
    const dispatcherItems = getDashboardNavItems(["Dispatcher"]).map(
      (item) => item.href,
    );

    expect(adminItems).toContain("/users");
    expect(adminItems).toContain("/zones");
    expect(adminItems).toContain("/depots");
    expect(dispatcherItems).not.toContain("/users");
    expect(dispatcherItems).toContain("/zones");
    expect(dispatcherItems).toContain("/depots");
  });

  it("matches active dashboard routes including nested paths", () => {
    expect(isDashboardNavActive("/dashboard", "/dashboard")).toBe(true);
    expect(isDashboardNavActive("/zones", "/zones")).toBe(true);
    expect(isDashboardNavActive("/zones/123", "/zones")).toBe(true);
    expect(isDashboardNavActive("/depots", "/zones")).toBe(false);
  });
});
