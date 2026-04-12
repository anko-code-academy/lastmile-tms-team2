export const DRIVER_ROLE = "Driver" as const;

export const ROUTE_MANAGER_ROLES = [
  "Admin",
  "Dispatcher",
  "OperationsManager",
] as const;

export function hasAnyRole(
  userRoles: readonly string[] | undefined,
  requiredRoles: readonly string[],
): boolean {
  return requiredRoles.some((role) => userRoles?.includes(role));
}

export function isDriver(userRoles: readonly string[] | undefined): boolean {
  return userRoles?.includes(DRIVER_ROLE) ?? false;
}

export function canManageRoutes(
  userRoles: readonly string[] | undefined,
): boolean {
  return hasAnyRole(userRoles, ROUTE_MANAGER_ROLES);
}
