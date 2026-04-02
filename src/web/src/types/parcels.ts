/** Matches backend `WeightUnit`: Lb = 0, Kg = 1 */
export const ParcelWeightUnit = {
  Lb: 0,
  Kg: 1,
} as const;

/** GraphQL WeightUnit enum: LB, KG */
export type GraphQLWeightUnit = "KG" | "LB";

/** GraphQL DimensionUnit enum: CM, IN */
export type GraphQLDimensionUnit = "CM" | "IN";

/** GraphQL ServiceType enum: ECONOMY, EXPRESS, OVERNIGHT, STANDARD */
export type GraphQLServiceType =
  | "ECONOMY"
  | "EXPRESS"
  | "OVERNIGHT"
  | "STANDARD";

/** GraphQL ParcelStatus enum */
export type GraphQLParcelStatus =
  | "REGISTERED"
  | "RECEIVED_AT_DEPOT"
  | "SORTED"
  | "STAGED"
  | "LOADED"
  | "OUT_FOR_DELIVERY"
  | "DELIVERED"
  | "FAILED_ATTEMPT"
  | "RETURNED_TO_DEPOT"
  | "CANCELLED"
  | "EXCEPTION";

/** Backend ServiceType enum values */
export const ParcelServiceType = {
  Economy: "ECONOMY",
  Express: "EXPRESS",
  Overnight: "OVERNIGHT",
  Standard: "STANDARD",
} as const;

/** Backend DimensionUnit enum values */
export const ParcelDimensionUnit = {
  Cm: "CM",
  In: "IN",
} as const;

export const ParcelWeightUnitOptions = [
  { value: ParcelWeightUnit.Kg, label: "kg" },
  { value: ParcelWeightUnit.Lb, label: "lb" },
] as const;

export const ParcelServiceTypeOptions = [
  { value: ParcelServiceType.Economy, label: "Economy" },
  { value: ParcelServiceType.Standard, label: "Standard" },
  { value: ParcelServiceType.Express, label: "Express" },
  { value: ParcelServiceType.Overnight, label: "Overnight" },
] as const;

export const ParcelDimensionUnitOptions = [
  { value: ParcelDimensionUnit.Cm, label: "cm" },
  { value: ParcelDimensionUnit.In, label: "in" },
] as const;

export interface ParcelOption {
  id: string;
  trackingNumber: string;
  weight: number;
  weightUnit: number;
}

export type LabelDownloadFormat = "zpl" | "pdf";

export interface RegisterParcelFormData {
  shipperAddressId: string;
  recipientStreet1: string;
  recipientStreet2: string;
  recipientCity: string;
  recipientState: string;
  recipientPostalCode: string;
  recipientCountryCode: string;
  recipientIsResidential: boolean;
  recipientContactName: string;
  recipientCompanyName: string;
  recipientPhone: string;
  recipientEmail: string;
  description: string;
  parcelType: string;
  serviceType: GraphQLServiceType;
  weight: number;
  weightUnit: number;
  length: number;
  width: number;
  height: number;
  dimensionUnit: string;
  declaredValue: number;
  currency: string;
  estimatedDeliveryDate: string;
}

export interface RegisteredParcelResult {
  id: string;
  trackingNumber: string;
  barcode: string;
  status: string;
  serviceType: string;
  weight: number;
  weightUnit: string;
  length: number;
  width: number;
  height: number;
  dimensionUnit: string;
  declaredValue: number;
  currency: string;
  description: string | null;
  parcelType: string | null;
  estimatedDeliveryDate: string;
  createdAt: string;
  zoneId: string;
  zoneName: string | null;
  depotId: string;
  depotName: string | null;
}

export interface ParcelDetailAddress {
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
}

export interface ParcelDetail extends RegisteredParcelResult {
  deliveryAttempts: number;
  lastModifiedAt: string | null;
  recipientAddress: ParcelDetailAddress;
}

export type ParcelImportFileFormat = "Csv" | "Xlsx";

export type ParcelImportStatus =
  | "Queued"
  | "Processing"
  | "Completed"
  | "CompletedWithErrors"
  | "Failed";

export interface ParcelImportHistoryEntry {
  id: string;
  fileName: string;
  fileFormat: ParcelImportFileFormat;
  status: ParcelImportStatus;
  totalRows: number;
  processedRows: number;
  importedRows: number;
  rejectedRows: number;
  depotName: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface ParcelImportRowFailurePreview {
  rowNumber: number;
  errorMessage: string;
  originalRowValues: string;
}

export interface ParcelImportDetail extends ParcelImportHistoryEntry {
  failureMessage: string | null;
  createdTrackingNumbers: string[];
  rowFailuresPreview: ParcelImportRowFailurePreview[];
}

export interface UploadParcelImportRequest {
  shipperAddressId: string;
  file: File;
}

export interface UploadParcelImportResult {
  importId: string;
}

export type ParcelImportTemplateFormat = "csv" | "xlsx";
