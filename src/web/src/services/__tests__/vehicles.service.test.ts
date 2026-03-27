import { describe, it, expect, vi, beforeEach } from "vitest";
import { vehiclesService } from "../vehicles.service";
import type { VehicleStatus } from "../../types/vehicles";

vi.mock("@/lib/network/graphql-client", () => ({
  graphqlRequest: vi.fn(),
}));

import { graphqlRequest } from "@/lib/network/graphql-client";

const mockGraphql = graphqlRequest as ReturnType<typeof vi.fn>;

describe("vehiclesService", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getAll", () => {
    it("should fetch all vehicles with status filter", async () => {
      const mockVehicles = [
        {
          id: "v-1",
          registrationPlate: "ABC-123",
          type: "VAN",
          parcelCapacity: 10,
          weightCapacity: 100,
          status: "AVAILABLE",
          depotId: "dep-1",
          depotName: "Main Depot",
          totalRoutes: 5,
          routesCompleted: 3,
          totalMileage: 1500,
          createdAt: "2024-01-01",
          lastModifiedAt: null,
        },
      ];

      mockGraphql.mockResolvedValueOnce({ vehicles: mockVehicles });

      const result = await vehiclesService.getAll({
        status: { eq: "AVAILABLE" },
      });

      expect(mockGraphql).toHaveBeenCalledWith(
        expect.any(Object),
        expect.objectContaining({
          where: { status: { eq: "AVAILABLE" } },
        })
      );
      expect(result).toHaveLength(1);
      expect(result[0].registrationPlate).toBe("ABC-123");
    });

    it("should fetch vehicles with depot filter", async () => {
      const mockVehicles: unknown[] = [];

      mockGraphql.mockResolvedValueOnce({ vehicles: mockVehicles });

      await vehiclesService.getAll({
        depotId: { eq: "dep-123" },
      });

      expect(mockGraphql).toHaveBeenCalledWith(
        expect.any(Object),
        expect.objectContaining({
          where: { depotId: { eq: "dep-123" } },
        })
      );
    });
  });

  describe("getById", () => {
    it("should fetch a vehicle by id from the list", async () => {
      const mockVehicle = {
        id: "v-1",
        registrationPlate: "ABC-123",
        type: "VAN",
        parcelCapacity: 10,
        weightCapacity: 100,
        status: "AVAILABLE",
        depotId: "dep-1",
        depotName: "Main Depot",
        totalRoutes: 5,
        routesCompleted: 3,
        totalMileage: 1500,
        createdAt: "2024-01-01",
        lastModifiedAt: null,
      };

      mockGraphql.mockResolvedValueOnce({ vehicles: [mockVehicle] });

      const result = await vehiclesService.getById("v-1");

      expect(mockGraphql).toHaveBeenCalledWith(
        expect.any(Object),
        {}
      );
      expect(result.registrationPlate).toBe("ABC-123");
    });
  });

  describe("create", () => {
    it("should create a new vehicle", async () => {
      const newVehicle = {
        registrationPlate: "XYZ-999",
        type: "CAR",
        parcelCapacity: 5,
        weightCapacity: 50,
        status: "AVAILABLE",
        depotId: "dep-1",
      } as const;

      const createdVehicle = {
        id: "new-id",
        ...newVehicle,
        depotName: "Main Depot",
        totalRoutes: 0,
        routesCompleted: 0,
        totalMileage: 0,
        createdAt: "2024-01-01",
        lastModifiedAt: null,
      };

      mockGraphql.mockResolvedValueOnce({ createVehicle: createdVehicle });

      const result = await vehiclesService.create(newVehicle);

      expect(mockGraphql).toHaveBeenCalledWith(
        expect.any(Object),
        {
          input: {
            registrationPlate: "XYZ-999",
            type: "CAR",
            parcelCapacity: 5,
            weightCapacity: 50,
            status: "AVAILABLE",
            depotId: "dep-1",
          },
        }
      );
      expect(result.registrationPlate).toBe("XYZ-999");
    });
  });

  describe("update", () => {
    it("should update an existing vehicle", async () => {
      const updateData = {
        registrationPlate: "ABC-001",
        type: "VAN",
        parcelCapacity: 15,
        weightCapacity: 150,
        status: "IN_USE",
        depotId: "dep-1",
      } as const;

      const updatedVehicle = {
        id: "123",
        ...updateData,
        depotName: "Main Depot",
        totalRoutes: 5,
        routesCompleted: 3,
        totalMileage: 1500,
        createdAt: "2024-01-01",
        lastModifiedAt: "2024-01-02",
      };

      mockGraphql.mockResolvedValueOnce({ updateVehicle: updatedVehicle });

      const result = await vehiclesService.update("123", updateData);

      expect(mockGraphql).toHaveBeenCalledWith(
        expect.any(Object),
        expect.objectContaining({
          id: "123",
        })
      );
      expect(result.registrationPlate).toBe("ABC-001");
    });
  });

  describe("delete", () => {
    it("should delete a vehicle", async () => {
      mockGraphql.mockResolvedValueOnce({ deleteVehicle: true });

      const result = await vehiclesService.delete("123");

      expect(mockGraphql).toHaveBeenCalledWith(
        expect.any(Object),
        { id: "123" }
      );
      expect(result).toBe(true);
    });
  });
});
