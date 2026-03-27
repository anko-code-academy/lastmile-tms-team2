import {
  COMPLETE_PASSWORD_RESET,
  CREATE_USER,
  DEACTIVATE_USER,
  REQUEST_PASSWORD_RESET,
  SEND_PASSWORD_RESET_EMAIL,
  UPDATE_USER,
  USERS_LIST,
  USERS_LOOKUPS,
} from "@/graphql/users";
import { graphqlRequest } from "@/lib/network/graphql-client";
import type {
  CompletePasswordResetInput,
  CreateUserInput,
  GetUsersInput,
  RequestPasswordResetInput,
  UpdateUserInput,
  UserActionResult,
  UserManagementLookups,
  UserManagementUser,
} from "@/types/users";

export async function getUserManagementLookups(
  accessToken: string
): Promise<UserManagementLookups> {
  const data = await graphqlRequest<{
    userManagementLookups: UserManagementLookups;
  }>(USERS_LOOKUPS, undefined, accessToken);

  return data.userManagementLookups;
}

export async function getUsers(
  accessToken: string,
  filters: GetUsersInput
): Promise<UserManagementUser[]> {
  const data = await graphqlRequest<{ users: UserManagementUser[] }>(
    USERS_LIST,
    {
      search: filters.search,
      isActive: filters.isActive,
      depotId: filters.depotId,
      zoneId: filters.zoneId,
    },
    accessToken
  );

  return data.users;
}

export async function createUser(
  accessToken: string,
  input: CreateUserInput
): Promise<UserManagementUser> {
  const data = await graphqlRequest<{ createUser: UserManagementUser }>(
    CREATE_USER,
    {
      input,
    },
    accessToken
  );

  return data.createUser;
}

export async function updateUser(
  accessToken: string,
  input: UpdateUserInput
): Promise<UserManagementUser> {
  const data = await graphqlRequest<{ updateUser: UserManagementUser }>(
    UPDATE_USER,
    {
      input,
    },
    accessToken
  );

  return data.updateUser;
}

export async function deactivateUser(
  accessToken: string,
  userId: string
): Promise<UserManagementUser> {
  const data = await graphqlRequest<{ deactivateUser: UserManagementUser }>(
    DEACTIVATE_USER,
    {
      userId,
    },
    accessToken
  );

  return data.deactivateUser;
}

export async function sendPasswordResetEmail(
  accessToken: string,
  userId: string
): Promise<UserActionResult> {
  const data = await graphqlRequest<{ sendPasswordResetEmail: UserActionResult }>(
    SEND_PASSWORD_RESET_EMAIL,
    {
      userId,
    },
    accessToken
  );

  return data.sendPasswordResetEmail;
}

export async function completePasswordReset(
  input: CompletePasswordResetInput
): Promise<UserActionResult> {
  const data = await graphqlRequest<{ completePasswordReset: UserActionResult }>(
    COMPLETE_PASSWORD_RESET,
    {
      input,
    }
  );

  return data.completePasswordReset;
}

export async function requestPasswordReset(
  input: RequestPasswordResetInput
): Promise<UserActionResult> {
  const data = await graphqlRequest<{ requestPasswordReset: UserActionResult }>(
    REQUEST_PASSWORD_RESET,
    {
      email: input.email,
    }
  );

  return data.requestPasswordReset;
}
