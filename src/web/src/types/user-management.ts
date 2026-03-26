export const USER_ROLES = [
  "Admin",
  "OperationsManager",
  "Dispatcher",
  "WarehouseOperator",
  "Driver",
] as const;

export type UserRole = (typeof USER_ROLES)[number];

export interface UserManagementUser {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone: string | null;
  role: UserRole | null;
  isActive: boolean;
  isProtected: boolean;
  depotId: string | null;
  depotName: string | null;
  zoneId: string | null;
  zoneName: string | null;
  createdAt: string;
  lastModifiedAt: string | null;
}

export interface UserManagementUsersResult {
  totalCount: number;
  items: UserManagementUser[];
}

export interface UserRoleOption {
  value: UserRole;
  label: string;
}

export interface DepotOption {
  id: string;
  name: string;
}

export interface ZoneOption {
  id: string;
  depotId: string;
  name: string;
}

export interface UserManagementLookups {
  roles: UserRoleOption[];
  depots: DepotOption[];
  zones: ZoneOption[];
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
  role?: UserRole;
  isActive?: boolean;
  depotId?: string;
  zoneId?: string;
  skip?: number;
  take?: number;
}
