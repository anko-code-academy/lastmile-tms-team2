import { graphqlRequest } from "@/lib/graphql";
import { normalizeDepot, serializeDepotOperatingHours } from "@/lib/depot-operating-hours";
import type { CreateDepotRequest, Depot, UpdateDepotRequest } from "@/types/depots";

type GraphQLDepot = Parameters<typeof normalizeDepot>[0];

const DEPOT_ADDRESS_FIELDS = `
  street1
  street2
  city
  state
  postalCode
  countryCode
  isResidential
  contactName
  companyName
  phone
  email
`;

const DEPOT_HOURS_FIELDS = `
  dayOfWeek
  openTime
  closedTime
  isClosed
`;

const DEPOT_FIELDS = `
  id
  name
  address { ${DEPOT_ADDRESS_FIELDS} }
  operatingHours { ${DEPOT_HOURS_FIELDS} }
  isActive
  createdAt
  updatedAt
`;

export const depotsApi = {
  list: async (): Promise<Depot[]> => {
    const data = await graphqlRequest<{ depots: GraphQLDepot[] }>(`
      query GetDepots { depots { ${DEPOT_FIELDS} } }
    `);
    return data.depots.map(normalizeDepot);
  },

  get: async (id: string): Promise<Depot> => {
    const data = await graphqlRequest<{ depot: GraphQLDepot | null }>(`
      query GetDepot($id: UUID!) { depot(id: $id) { ${DEPOT_FIELDS} } }
    `, { id });
    if (!data.depot) throw new Error("Depot not found");
    return normalizeDepot(data.depot);
  },

  create: async (req: CreateDepotRequest): Promise<Depot> => {
    const data = await graphqlRequest<{ createDepot: GraphQLDepot }>(`
      mutation CreateDepot($input: CreateDepotInput!) {
        createDepot(input: $input) { ${DEPOT_FIELDS} }
      }
    `, {
      input: {
        name: req.name,
        address: {
          street1: req.address.street1,
          street2: req.address.street2 ?? null,
          city: req.address.city,
          state: req.address.state,
          postalCode: req.address.postalCode,
          countryCode: req.address.countryCode,
          isResidential: req.address.isResidential,
          contactName: req.address.contactName ?? null,
          companyName: req.address.companyName ?? null,
          phone: req.address.phone ?? null,
          email: req.address.email ?? null,
        },
        operatingHours: serializeDepotOperatingHours(req.operatingHours) ?? [],
        isActive: req.isActive,
      },
    });
    return normalizeDepot(data.createDepot);
  },

  update: async (id: string, req: UpdateDepotRequest): Promise<Depot> => {
    const input: Record<string, unknown> = {
      name: req.name,
      isActive: req.isActive,
    };
    if (req.address) {
      input.address = {
        street1: req.address.street1,
        street2: req.address.street2 ?? null,
        city: req.address.city,
        state: req.address.state,
        postalCode: req.address.postalCode,
        countryCode: req.address.countryCode,
        isResidential: req.address.isResidential,
        contactName: req.address.contactName ?? null,
        companyName: req.address.companyName ?? null,
        phone: req.address.phone ?? null,
        email: req.address.email ?? null,
      };
    }
    if (req.operatingHours) {
      input.operatingHours = serializeDepotOperatingHours(req.operatingHours);
    }
    const data = await graphqlRequest<{ updateDepot: GraphQLDepot | null }>(`
      mutation UpdateDepot($id: UUID!, $input: UpdateDepotInput!) {
        updateDepot(id: $id, input: $input) { ${DEPOT_FIELDS} }
      }
    `, { id, input });
    if (!data.updateDepot) throw new Error("Depot not found");
    return normalizeDepot(data.updateDepot);
  },

  delete: async (id: string): Promise<void> => {
    await graphqlRequest<{ deleteDepot: boolean }>(`
      mutation DeleteDepot($id: UUID!) { deleteDepot(id: $id) }
    `, { id });
  },
};
