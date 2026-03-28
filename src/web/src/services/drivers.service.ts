import { DRIVERS_LIST } from "@/graphql/drivers";
import { graphqlRequest } from "@/lib/network/graphql-client";
import type { DriverOption } from "@/types/drivers";
import { mockDrivers } from "@/mocks/drivers.mock";

const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK_DATA === "true";

export const driversService = {
  getAll: async (depotId?: string): Promise<DriverOption[]> => {
    if (USE_MOCK) {
      return mockDrivers.map((d) => ({
        id: d.id,
        displayName: d.name,
      }));
    }

    const variables: Record<string, unknown> = {};
    // Empty string is not a valid GraphQL UUID literal omit variable to mean "no filter".
    if (depotId !== undefined && depotId.trim() !== "") {
      variables.depotId = depotId;
    }

    const data = await graphqlRequest<{ drivers: DriverOption[] }>(
      DRIVERS_LIST,
      Object.keys(variables).length ? variables : undefined
    );
    return data.drivers;
  },
};
