import type {
  ParcelDetail,
  ParcelDetailAddress,
  RegisteredParcelResult,
  TrackingEvent,
} from "@/types/parcels";

type MockParcel = RegisteredParcelResult & {
  detail: ParcelDetail;
};

const MOCK_TRACKING_EVENTS: TrackingEvent[] = [
  {
    id: "evt-0001",
    timestamp: "2026-04-01T09:15:00Z",
    eventType: "LabelCreated",
    description: "Parcel registered",
    location: "Online Portal",
    operator: "System",
  },
  {
    id: "evt-0002",
    timestamp: "2026-04-01T11:30:00Z",
    eventType: "ArrivedAtFacility",
    description: "Received at depot intake",
    location: "North Depot",
    operator: "admin",
  },
];

const MOCK_SENDER_ADDRESS: ParcelDetailAddress = {
  street1: "10 Shipper Lane",
  street2: "Dock Office",
  city: "Chicago",
  state: "IL",
  postalCode: "60601",
  countryCode: "US",
  isResidential: false,
  contactName: "Sender Contact",
  companyName: "Warehouse Sender",
  phone: "+1 555 0199",
  email: "sender@example.com",
};

function makeMockParcel(
  id: string,
  trackingNumber: string,
  overrides?: Partial<ParcelDetail>,
): MockParcel {
  const base: ParcelDetail = {
    id,
    trackingNumber,
    barcode: trackingNumber,
    status: "REGISTERED",
    shipperAddressId: "address-1",
    serviceType: "STANDARD",
    weight: 2.5,
    weightUnit: "KG",
    length: 32,
    width: 24,
    height: 18,
    dimensionUnit: "CM",
    declaredValue: 120,
    currency: "USD",
    description: "Warehouse intake parcel",
    parcelType: "Box",
    estimatedDeliveryDate: "2026-04-06T00:00:00Z",
    createdAt: "2026-04-01T09:15:00Z",
    zoneId: "90000000-0000-0000-0000-000000000001",
    zoneName: "North Zone",
    depotId: "80000000-0000-0000-0000-000000000001",
    depotName: "North Depot",
    cancellationReason: null,
    deliveryAttempts: 0,
    canEdit: true,
    canCancel: true,
    lastModifiedAt: "2026-04-01T09:15:00Z",
    senderAddress: MOCK_SENDER_ADDRESS,
    recipientAddress: {
      street1: "123 Main St",
      street2: null,
      city: "Springfield",
      state: "IL",
      postalCode: "62701",
      countryCode: "US",
      isResidential: true,
      contactName: "Jamie Carter",
      companyName: null,
      phone: "+1 555 0100",
      email: "jamie@example.com",
    },
    statusTimeline: MOCK_TRACKING_EVENTS,
    changeHistory: [],
    routeAssignment: {
      routeId: "route-1",
      routeStatus: "InProgress",
      startDate: "2026-04-01T10:15:00Z",
      endDate: null,
      driverId: "driver-1",
      driverName: "Jamie Driver",
      vehicleId: "vehicle-1",
      vehiclePlate: "TST-100",
    },
    proofOfDelivery: {
      receivedBy: "Front Desk",
      deliveryLocation: "Building A",
      deliveredAt: "2026-04-01T12:30:00Z",
      hasSignatureImage: true,
      hasPhoto: true,
    },
    allowedNextStatuses: ["RECEIVED_AT_DEPOT", "CANCELLED"],
    ...overrides,
  };

  return {
    id: base.id,
    trackingNumber: base.trackingNumber,
    barcode: base.barcode,
    status: base.status,
    serviceType: base.serviceType,
    weight: base.weight,
    weightUnit: base.weightUnit,
    length: base.length,
    width: base.width,
    height: base.height,
    dimensionUnit: base.dimensionUnit,
    declaredValue: base.declaredValue,
    currency: base.currency,
    description: base.description,
    parcelType: base.parcelType,
    estimatedDeliveryDate: base.estimatedDeliveryDate,
    createdAt: base.createdAt,
    zoneId: base.zoneId,
    zoneName: base.zoneName,
    depotId: base.depotId,
    depotName: base.depotName,
    detail: base,
  };
}

/** Mock parcel rows for `NEXT_PUBLIC_USE_MOCK_DATA` (parcels service). */
export const mockParcels: MockParcel[] = [
  makeMockParcel("40000000-0000-0000-0000-000000000001", "LM202604010001"),
  makeMockParcel("40000000-0000-0000-0000-000000000002", "LM202604010002", {
    serviceType: "EXPRESS",
    parcelType: "Envelope",
    zoneName: "Central Zone",
    recipientAddress: {
      street1: "400 Market St",
      street2: "Suite 300",
      city: "San Francisco",
      state: "CA",
      postalCode: "94111",
      countryCode: "US",
      isResidential: false,
      contactName: "Taylor Singh",
      companyName: "Bay Retail",
      phone: "+1 555 0101",
      email: "taylor@bayretail.com",
    },
  }),
  makeMockParcel("40000000-0000-0000-0000-000000000003", "LM202604010003", {
    serviceType: "ECONOMY",
    weight: 5.8,
    parcelType: "Soft Pack",
    zoneName: "South Zone",
  }),
];
