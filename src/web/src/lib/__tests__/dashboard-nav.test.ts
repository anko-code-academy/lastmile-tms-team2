import { describe, expect, it } from "vitest";

import {
  getDashboardNavItems,
  isDashboardNavActive,
} from "@/lib/navigation/dashboard-nav";

describe("dashboard navigation", () => {
  it("shows admin-only links only for admins", () => {
    const adminItems = getDashboardNavItems(["Admin"]).map((item) => item.href);
    const dispatcherItems = getDashboardNavItems(["Dispatcher"]).map(
      (item) => item.href,
    );
    const driverItems = getDashboardNavItems(["Driver"]).map((item) => item.href);

    expect(adminItems).toContain("/users");
    expect(adminItems).toContain("/zones");
    expect(adminItems).toContain("/depots");
    expect(adminItems).toContain("/bin-locations");
    expect(adminItems).toContain("/routes");
    expect(dispatcherItems).not.toContain("/users");
    expect(dispatcherItems).not.toContain("/bin-locations");
    expect(dispatcherItems).toContain("/routes");
    expect(dispatcherItems).toContain("/zones");
    expect(dispatcherItems).toContain("/depots");
    expect(driverItems).not.toContain("/routes");
    expect(driverItems).toContain("/routes/my");
  });

  it("shows bin locations to operations managers", () => {
    const operationsItems = getDashboardNavItems(["OperationsManager"]).map(
      (item) => item.href,
    );

    expect(operationsItems).toContain("/bin-locations");
  });

  it("matches active dashboard routes including nested paths", () => {
    expect(isDashboardNavActive("/dashboard", "/dashboard")).toBe(true);
    expect(isDashboardNavActive("/zones", "/zones")).toBe(true);
    expect(isDashboardNavActive("/zones/123", "/zones")).toBe(true);
    expect(isDashboardNavActive("/routes/123", "/routes/my")).toBe(true);
    expect(isDashboardNavActive("/depots", "/zones")).toBe(false);
  });
});
