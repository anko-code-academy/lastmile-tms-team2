import {
  PARCELS_FOR_ROUTE,
  REGISTER_PARCEL,
  REGISTERED_PARCELS,
} from "@/graphql/parcels";
import { graphqlRequest } from "@/lib/network/graphql-client";
import type { GetRegisteredParcelsQuery } from "@/graphql/parcels";
import type {
  ParcelOption,
  RegisterParcelFormData,
  RegisteredParcelResult,
} from "@/types/parcels";
import { mockParcels } from "@/mocks/parcels.mock";

const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK_DATA === "true";

export const parcelsService = {
  getForRouteCreation: async (): Promise<ParcelOption[]> => {
    if (USE_MOCK) {
      return mockParcels.map((p) => ({
        id: p.id,
        trackingNumber: p.trackingNumber,
        weight: p.weight,
        weightUnit: (p.weightUnit as string) === "Lb" ? 0 : 1,
      }));
    }

    const data = await graphqlRequest<{
      parcelsForRouteCreation: ParcelOption[];
    }>(PARCELS_FOR_ROUTE);
    return data.parcelsForRouteCreation;
  },

  getRegisteredParcels: async (): Promise<GetRegisteredParcelsQuery["registeredParcels"]> => {
    if (USE_MOCK) {
      return [];
    }

    const data = await graphqlRequest<{
      registeredParcels: GetRegisteredParcelsQuery["registeredParcels"];
    }>(REGISTERED_PARCELS);
    return data.registeredParcels;
  },

  register: async (form: RegisterParcelFormData): Promise<RegisteredParcelResult> => {
    if (USE_MOCK) {
      return {
        id: "40000000-0000-0000-0000-000000000099",
        trackingNumber: "LM20260329MOCK001",
        barcode: "LM20260329MOCK001",
        status: "Registered",
        serviceType: form.serviceType,
        weight: form.weight,
        weightUnit: form.weightUnit === 1 ? "KG" : "LB",
        length: form.length,
        width: form.width,
        height: form.height,
        dimensionUnit: form.dimensionUnit,
        declaredValue: form.declaredValue,
        currency: form.currency,
        description: form.description || null,
        parcelType: form.parcelType || null,
        estimatedDeliveryDate: form.estimatedDeliveryDate,
        createdAt: new Date().toISOString(),
        zoneId: "00000000-0000-0000-0000-000000000099",
        zoneName: "Mock Zone",
        depotId: form.shipperAddressId,
        depotName: "Mock Depot",
      };
    }

    const data = await graphqlRequest<{
      registerParcel: RegisteredParcelResult;
    }>(REGISTER_PARCEL, {
      input: {
        shipperAddressId: form.shipperAddressId,
        recipientAddress: {
          street1: form.recipientStreet1,
          street2: form.recipientStreet2 || null,
          city: form.recipientCity,
          state: form.recipientState,
          postalCode: form.recipientPostalCode,
          countryCode: form.recipientCountryCode,
          isResidential: form.recipientIsResidential,
          contactName: form.recipientContactName || null,
          companyName: form.recipientCompanyName || null,
          phone: form.recipientPhone || null,
          email: form.recipientEmail || null,
        },
        description: form.description || null,
        parcelType: form.parcelType || null,
        serviceType: form.serviceType,
        weight: form.weight,
        weightUnit: form.weightUnit === 1 ? "KG" : "LB",
        length: form.length,
        width: form.width,
        height: form.height,
        dimensionUnit: form.dimensionUnit === "CM" ? "CM" : "IN",
        declaredValue: form.declaredValue,
        currency: form.currency,
        estimatedDeliveryDate: form.estimatedDeliveryDate,
      },
    });
    return data.registerParcel;
  },
};
