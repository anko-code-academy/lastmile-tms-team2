export type DepotOption = {
  id: string;
  name: string;
};

export interface Depot {
  id: string;
  name: string;
  address: DepotAddress | null;
  operatingHours: DepotOperatingHours[] | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface DepotAddress {
  street1: string;
  street2: string | null;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
  isResidential: boolean;
  contactName: string | null;
  companyName: string | null;
  phone: string | null;
  email: string | null;
  geoLocation: DepotGeoLocation | null;
}

export interface DepotGeoLocation {
  latitude: number;
  longitude: number;
}

export interface DepotOperatingHours {
  dayOfWeek: number;
  openTime: string | null;
  closedTime: string | null;
  isClosed: boolean;
}

export interface CreateDepotRequest {
  name: string;
  address: {
    street1: string;
    street2?: string;
    city: string;
    state: string;
    postalCode: string;
    countryCode: string;
    isResidential: boolean;
    contactName?: string;
    companyName?: string;
    phone?: string;
    email?: string;
  };
  operatingHours?: {
    dayOfWeek: number;
    openTime: string;
    closedTime: string;
    isClosed: boolean;
  }[];
  isActive: boolean;
}

export interface UpdateDepotRequest {
  name: string;
  address?: {
    street1: string;
    street2?: string;
    city: string;
    state: string;
    postalCode: string;
    countryCode: string;
    isResidential: boolean;
    contactName?: string;
    companyName?: string;
    phone?: string;
    email?: string;
  };
  operatingHours?: {
    dayOfWeek: number;
    openTime: string;
    closedTime: string;
    isClosed: boolean;
  }[];
  isActive: boolean;
}
