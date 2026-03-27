import type { DepotOption } from "@/types/depots";
import type { UserRole as GeneratedUserRole } from "@/graphql/generated";

export type UserRole = GeneratedUserRole;

export const USER_ROLES = [
  "Admin",
  "OperationsManager",
  "Dispatcher",
  "WarehouseOperator",
  "Driver",
] as const;

export interface UserManagementUser {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone: string | null;
  role: string | null;
  isActive: boolean;
  isProtected: boolean;
  depotId: string | null;
  depotName: string | null;
  zoneId: string | null;
  zoneName: string | null;
  createdAt: string;
  lastModifiedAt: string | null;
}

export interface UserRoleOption {
  value: UserRole;
  label: string;
}

export interface UserZoneOption {
  id: string;
  depotId: string;
  name: string;
}

export interface UserManagementLookups {
  roles: UserRoleOption[];
  depots: DepotOption[];
  zones: UserZoneOption[];
}

export interface CreateUserInput {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  role: UserRole;
  depotId?: string | null;
  zoneId?: string | null;
}

export interface UpdateUserInput extends CreateUserInput {
  id: string;
  isActive: boolean;
}

export interface CompletePasswordResetInput {
  email: string;
  token: string;
  newPassword: string;
}

export interface RequestPasswordResetInput {
  email: string;
}

export interface UserActionResult {
  success: boolean;
  message: string;
}

export interface GetUsersInput {
  search?: string;
  isActive?: boolean;
  depotId?: string;
  zoneId?: string;
}
