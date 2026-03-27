import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  useVehicles,
  useVehicle,
  useCreateVehicle,
  useUpdateVehicle,
  useDeleteVehicle,
  vehicleKeys,
} from "../vehicles";
import type { Vehicle } from "../../types/vehicles";
import type { VehicleStatus } from "../../types/vehicles";
import * as vehiclesService from "../../services/vehicles.service";

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: { user: { name: "Test" } },
  }),
}));

// Mock the service
vi.mock("../../services/vehicles.service", () => ({
  vehiclesService: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}));

const mockVehiclesService = vi.mocked(vehiclesService.vehiclesService);

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

  function QueryClientWrapper({ children }: { children: React.ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  }
  QueryClientWrapper.displayName = "QueryClientWrapper";
  return QueryClientWrapper;
};

describe("useVehicles", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should fetch vehicles with no filters", async () => {
    const mockVehicles = [
      { id: "1", registrationPlate: "ABC-001" } as unknown as Vehicle,
    ];

    mockVehiclesService.getAll.mockResolvedValueOnce(mockVehicles);

    const { result } = renderHook(() => useVehicles({}), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockVehiclesService.getAll).toHaveBeenCalledWith(undefined);
    expect(result.current.data).toEqual(mockVehicles);
  });

  it("should fetch vehicles with status filter", async () => {
    const mockVehicles: Vehicle[] = [];

    mockVehiclesService.getAll.mockResolvedValueOnce(mockVehicles);

    const { result } = renderHook(
      () => useVehicles({ status: "AVAILABLE" }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockVehiclesService.getAll).toHaveBeenCalledWith({
      status: { eq: "AVAILABLE" },
    });
  });

  it("should have correct query key", () => {
    const keys = vehicleKeys.list({ status: { eq: "AVAILABLE" } });
    expect(keys).toEqual(["vehicles", "list", { status: { eq: "AVAILABLE" } }]);
  });
});

describe("useVehicle", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should fetch vehicle by ID", async () => {
    const mockVehicle = {
      id: "123",
      registrationPlate: "ABC-001",
    };

    mockVehiclesService.getById.mockResolvedValueOnce(
      mockVehicle as unknown as Vehicle,
    );

    const { result } = renderHook(() => useVehicle("123"), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockVehiclesService.getById).toHaveBeenCalledWith("123");
    expect(result.current.data).toEqual(mockVehicle);
  });

  it("should not fetch when ID is empty", () => {
    renderHook(() => useVehicle(""), {
      wrapper: createWrapper(),
    });

    expect(mockVehiclesService.getById).not.toHaveBeenCalled();
  });
});

describe("useCreateVehicle", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should create a vehicle and invalidate queries", async () => {
    const newVehicle = {
      registrationPlate: "XYZ-999",
      type: "VAN",
      parcelCapacity: 10,
      weightCapacity: 100,
      status: "AVAILABLE",
      depotId: "dep-1",
    } as const;

    const createdVehicle = { id: "new-id", ...newVehicle };

    mockVehiclesService.create.mockResolvedValueOnce(
      createdVehicle as unknown as Vehicle,
    );

    const queryClient = new QueryClient();
    queryClient.setQueryData(vehicleKeys.lists(), []);

    const { result } = renderHook(() => useCreateVehicle(), {
      wrapper: ({ children }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      ),
    });

    result.current.mutate(newVehicle);

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockVehiclesService.create).toHaveBeenCalledWith(newVehicle);
  });
});

describe("useUpdateVehicle", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should update a vehicle and invalidate queries", async () => {
    const updateData = {
      registrationPlate: "ABC-001",
      type: "VAN",
      parcelCapacity: 15,
      weightCapacity: 150,
      status: "IN_USE",
      depotId: "dep-1",
    } as const;

    const updatedVehicle = { id: "123", ...updateData };

    mockVehiclesService.update.mockResolvedValueOnce(
      updatedVehicle as unknown as Vehicle,
    );

    const queryClient = new QueryClient();
    queryClient.setQueryData(vehicleKeys.detail("123"), { id: "123", registrationPlate: "OLD" });

    const { result } = renderHook(() => useUpdateVehicle(), {
      wrapper: ({ children }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      ),
    });

    result.current.mutate({ id: "123", data: updateData });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockVehiclesService.update).toHaveBeenCalledWith(
      "123",
      updateData
    );
  });
});

describe("useDeleteVehicle", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should delete a vehicle and invalidate queries", async () => {
    mockVehiclesService.delete.mockResolvedValueOnce(true);

    const queryClient = new QueryClient();
    queryClient.setQueryData(vehicleKeys.lists(), []);

    const { result } = renderHook(() => useDeleteVehicle(), {
      wrapper: ({ children }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      ),
    });

    result.current.mutate({
      id: "123",
      registrationPlate: "ABC-001",
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockVehiclesService.delete).toHaveBeenCalledWith("123");
  });
});
