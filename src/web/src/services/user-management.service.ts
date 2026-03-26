import { graphqlRequest } from "@/lib/graphql";
import type {
  CompletePasswordResetInput,
  CreateUserInput,
  GetUsersInput,
  RequestPasswordResetInput,
  UpdateUserInput,
  UserActionResult,
  UserManagementLookups,
  UserManagementUser,
  UserManagementUsersResult,
} from "@/types/user-management";

const USER_FIELDS = `
  id
  firstName
  lastName
  fullName
  email
  phone
  role
  isActive
  isProtected
  depotId
  depotName
  zoneId
  zoneName
  createdAt
  lastModifiedAt
`;

export async function getUserManagementLookups(
  accessToken: string
): Promise<UserManagementLookups> {
  const data = await graphqlRequest<{
    userManagementLookups: UserManagementLookups;
  }>(
    `
      query UserManagementLookups {
        userManagementLookups {
          roles {
            value
            label
          }
          depots {
            id
            name
          }
          zones {
            id
            depotId
            name
          }
        }
      }
    `,
    undefined,
    accessToken
  );

  return data.userManagementLookups;
}

export async function getUsers(
  accessToken: string,
  filters: GetUsersInput
): Promise<UserManagementUsersResult> {
  const data = await graphqlRequest<{ users: UserManagementUsersResult }>(
    `
      query Users(
        $search: String
        $role: UserRole
        $isActive: Boolean
        $depotId: UUID
        $zoneId: UUID
        $skip: Int!
        $take: Int!
      ) {
        users(
          search: $search
          role: $role
          isActive: $isActive
          depotId: $depotId
          zoneId: $zoneId
          skip: $skip
          take: $take
        ) {
          totalCount
          items {
            ${USER_FIELDS}
          }
        }
      }
    `,
    {
      search: filters.search,
      role: filters.role,
      isActive: filters.isActive,
      depotId: filters.depotId,
      zoneId: filters.zoneId,
      skip: filters.skip ?? 0,
      take: filters.take ?? 20,
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
    `
      mutation CreateUser($input: CreateUserInput!) {
        createUser(input: $input) {
          ${USER_FIELDS}
        }
      }
    `,
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
    `
      mutation UpdateUser($input: UpdateUserInput!) {
        updateUser(input: $input) {
          ${USER_FIELDS}
        }
      }
    `,
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
    `
      mutation DeactivateUser($userId: UUID!) {
        deactivateUser(userId: $userId) {
          ${USER_FIELDS}
        }
      }
    `,
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
    `
      mutation SendPasswordResetEmail($userId: UUID!) {
        sendPasswordResetEmail(userId: $userId) {
          success
          message
        }
      }
    `,
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
    `
      mutation CompletePasswordReset($input: CompletePasswordResetInput!) {
        completePasswordReset(input: $input) {
          success
          message
        }
      }
    `,
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
    `
      mutation RequestPasswordReset($email: String!) {
        requestPasswordReset(email: $email) {
          success
          message
        }
      }
    `,
    {
      email: input.email,
    }
  );

  return data.requestPasswordReset;
}
