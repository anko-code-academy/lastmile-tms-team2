import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UserManagementClient } from "@/components/common/UserManagement";

vi.mock("sonner", () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

vi.mock("@/services/user-management.service", () => ({
  getUserManagementLookups: vi.fn(),
  getUsers: vi.fn(),
  createUser: vi.fn(),
  updateUser: vi.fn(),
  deactivateUser: vi.fn(),
  sendPasswordResetEmail: vi.fn(),
}));

import {
  createUser,
  getUserManagementLookups,
  getUsers,
} from "@/services/user-management.service";

const mockedGetLookups = vi.mocked(getUserManagementLookups);
const mockedGetUsers = vi.mocked(getUsers);
const mockedCreateUser = vi.mocked(createUser);

describe("UserManagementClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockedGetLookups.mockResolvedValue({
      roles: [
        { value: "Admin", label: "Admin" },
        { value: "Dispatcher", label: "Dispatcher" },
      ],
      depots: [{ id: "depot-1", name: "North Depot" }],
      zones: [{ id: "zone-1", depotId: "depot-1", name: "Zone A" }],
    });

    mockedGetUsers.mockResolvedValue({
      totalCount: 1,
      items: [
        {
          id: "user-1",
          firstName: "Alex",
          lastName: "Admin",
          fullName: "Alex Admin",
          email: "alex@example.com",
          phone: "+10000000000",
          role: "Admin",
          isActive: true,
          isProtected: false,
          depotId: null,
          depotName: null,
          zoneId: null,
          zoneName: null,
          createdAt: "2026-03-24T00:00:00Z",
          lastModifiedAt: null,
        },
      ],
    });

    mockedCreateUser.mockResolvedValue({
      id: "user-2",
      firstName: "Casey",
      lastName: "Creator",
      fullName: "Casey Creator",
      email: "casey@example.com",
      phone: null,
      role: "Dispatcher",
      isActive: true,
      isProtected: false,
      depotId: "depot-1",
      depotName: "North Depot",
      zoneId: "zone-1",
      zoneName: "Zone A",
      createdAt: "2026-03-24T00:00:00Z",
      lastModifiedAt: null,
    });
  });

  it("renders users and submits the create user flow", async () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
        },
        mutations: {
          retry: false,
        },
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <UserManagementClient accessToken="token-123" />
      </QueryClientProvider>
    );

    expect(await screen.findByText("Alex Admin")).toBeInTheDocument();
    expect(screen.getByText("Total users")).toBeInTheDocument();
    expect(screen.getByText("Filters")).toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /new user/i }));
    const modal = await screen.findByTestId("user-form-modal");

    await user.type(screen.getByLabelText(/first name/i), "  Casey");
    await user.type(screen.getByLabelText(/last name/i), " Creator  ");
    await user.type(screen.getByLabelText(/^email$/i), "casey@example.com");
    await user.selectOptions(within(modal).getByLabelText(/^role$/i), "Dispatcher");
    await user.selectOptions(within(modal).getByLabelText(/^depot$/i), "depot-1");
    await user.selectOptions(within(modal).getByLabelText(/^zone$/i), "zone-1");

    await user.click(screen.getByRole("button", { name: /create user/i }));

    await waitFor(() => {
      expect(mockedCreateUser).toHaveBeenCalledWith("token-123", {
        firstName: "Casey",
        lastName: "Creator",
        email: "casey@example.com",
        phone: null,
        role: "Dispatcher",
        depotId: "depot-1",
        zoneId: "zone-1",
      });
    });
  });

  it("renders protected system admin as read-only", async () => {
    mockedGetUsers.mockResolvedValueOnce({
      totalCount: 1,
      items: [
        {
          id: "user-1",
          firstName: "System",
          lastName: "Admin",
          fullName: "System Admin",
          email: "admin@lastmile.com",
          phone: null,
          role: "Admin",
          isActive: true,
          isProtected: true,
          depotId: null,
          depotName: null,
          zoneId: null,
          zoneName: null,
          createdAt: "2026-03-24T00:00:00Z",
          lastModifiedAt: null,
        },
      ],
    });

    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <UserManagementClient accessToken="token-123" />
      </QueryClientProvider>
    );

    const rowText = await screen.findByText("System Admin");
    expect(await screen.findByText("System admin")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: /user management/i }),
    ).toBeInTheDocument();

    const row = rowText.closest("tr");
    expect(row).not.toBeNull();
    const scoped = within(row!);
    expect(scoped.getByRole("button", { name: /edit/i })).toBeDisabled();
    expect(scoped.getByRole("button", { name: /reset email/i })).toBeDisabled();
    expect(scoped.getByRole("button", { name: /deactivate/i })).toBeDisabled();
  });
});
