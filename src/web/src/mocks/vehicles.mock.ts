import type { Vehicle, VehicleStatus } from "@/types/vehicles";

const TEST_DEPOT_ID = "00000000-0000-0000-0000-000000000001";
const TEST_DEPOT_NAME = "Test Depot";

export const mockVehicles: Vehicle[] = [
  {
    id: "10000000-0000-0000-0000-000000000001",
    registrationPlate: "ABC-1234",
    type: "VAN",
    parcelCapacity: 120,
    weightCapacity: 800,
    status: "AVAILABLE",
    depotId: TEST_DEPOT_ID,
    depotName: TEST_DEPOT_NAME,
    totalRoutes: 48,
    routesCompleted: 45,
    totalMileage: 12500,
    createdAt: "2024-01-15T10:00:00Z",
    updatedAt: null,
  },
  {
    id: "10000000-0000-0000-0000-000000000002",
    registrationPlate: "XYZ-5678",
    type: "CAR",
    parcelCapacity: 20,
    weightCapacity: 150,
    status: "IN_USE",
    depotId: TEST_DEPOT_ID,
    depotName: TEST_DEPOT_NAME,
    totalRoutes: 14,
    routesCompleted: 12,
    totalMileage: 3400,
    createdAt: "2024-02-20T09:30:00Z",
    updatedAt: "2024-03-01T14:22:00Z",
  },
  {
    id: "10000000-0000-0000-0000-000000000003",
    registrationPlate: "BIKE-001",
    type: "BIKE",
    parcelCapacity: 5,
    weightCapacity: 25,
    status: "AVAILABLE",
    depotId: TEST_DEPOT_ID,
    depotName: TEST_DEPOT_NAME,
    totalRoutes: 3,
    routesCompleted: 3,
    totalMileage: 180,
    createdAt: "2024-03-01T08:00:00Z",
    updatedAt: null,
  },
  {
    id: "10000000-0000-0000-0000-000000000004",
    registrationPlate: "VAN-999",
    type: "VAN",
    parcelCapacity: 100,
    weightCapacity: 700,
    status: "MAINTENANCE",
    depotId: TEST_DEPOT_ID,
    depotName: TEST_DEPOT_NAME,
    totalRoutes: 91,
    routesCompleted: 89,
    totalMileage: 22400,
    createdAt: "2023-11-10T12:00:00Z",
    updatedAt: "2024-02-28T16:45:00Z",
  },
  {
    id: "10000000-0000-0000-0000-000000000005",
    registrationPlate: "OLD-001",
    type: "VAN",
    parcelCapacity: 80,
    weightCapacity: 500,
    status: "RETIRED",
    depotId: TEST_DEPOT_ID,
    depotName: TEST_DEPOT_NAME,
    totalRoutes: 315,
    routesCompleted: 312,
    totalMileage: 78000,
    createdAt: "2020-05-01T00:00:00Z",
    updatedAt: "2024-01-10T09:00:00Z",
  },
];

export function getMockVehiclesPaginated(
  page = 1,
  pageSize = 20,
  status?: VehicleStatus
): { items: Vehicle[]; totalCount: number; page: number; pageSize: number; totalPages: number } {
  let items = [...mockVehicles];

  if (status !== undefined) {
    items = items.filter((v) => v.status === status);
  }

  const totalCount = items.length;
  const totalPages = Math.ceil(totalCount / pageSize) || 1;
  const start = (page - 1) * pageSize;
  const paginatedItems = items.slice(start, start + pageSize);

  return {
    items: paginatedItems,
    totalCount,
    page,
    pageSize,
    totalPages,
  };
}

export function getMockVehicleById(id: string): Vehicle | undefined {
  return mockVehicles.find((v) => v.id === id);
}
