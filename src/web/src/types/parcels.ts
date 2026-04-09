export type { RouteStagingScanOutcome } from "@/graphql/generated";

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
  zoneId: string;
  zoneName: string | null;
}

export interface ParcelConnectionPageInfo {
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  startCursor: string | null;
  endCursor: string | null;
}

export interface ParcelConnectionPage<TNode> {
  totalCount: number;
  pageInfo: ParcelConnectionPageInfo;
  nodes: TNode[];
}

export type LabelDownloadFormat = "zpl" | "pdf";

export interface ParcelFormData {
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
  dimensionUnit: GraphQLDimensionUnit;
  declaredValue: number;
  currency: string;
  estimatedDeliveryDate: string;
}

export type RegisterParcelFormData = ParcelFormData;

export interface UpdateParcelRequest {
  id: string;
  data: ParcelFormData;
}

export interface CancelParcelRequest {
  id: string;
  reason: string;
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

export interface ParcelRouteAssignment {
  routeId: string;
  routeStatus: string;
  startDate: string;
  endDate: string | null;
  driverId: string;
  driverName: string;
  vehicleId: string;
  vehiclePlate: string;
}

export interface ParcelProofOfDelivery {
  receivedBy: string | null;
  deliveryLocation: string | null;
  deliveredAt: string;
  hasSignatureImage: boolean;
  hasPhoto: boolean;
}

export interface ParcelDetail extends RegisteredParcelResult {
  shipperAddressId: string;
  cancellationReason: string | null;
  deliveryAttempts: number;
  lastModifiedAt: string | null;
  canEdit: boolean;
  canCancel: boolean;
  senderAddress: ParcelDetailAddress;
  recipientAddress: ParcelDetailAddress;
  statusTimeline: TrackingEvent[];
  changeHistory: ParcelChangeHistoryEntry[];
  routeAssignment: ParcelRouteAssignment | null;
  proofOfDelivery: ParcelProofOfDelivery | null;
  /** GraphQL ParcelStatus enum names, e.g. RECEIVED_AT_DEPOT */
  allowedNextStatuses?: GraphQLParcelStatus[];
}

export interface ParcelChangeHistoryEntry {
  action: string;
  fieldName: string;
  beforeValue: string | null;
  afterValue: string | null;
  changedAt: string;
  changedBy: string | null;
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

export interface TrackingEvent {
  id: string;
  timestamp: string;
  eventType: string;
  description: string;
  location: string | null;
  operator: string | null;
}

export interface TransitionParcelStatusRequest {
  parcelId: string;
  newStatus: GraphQLParcelStatus;
  location?: string;
  description?: string;
}

export interface InboundManifest {
  id: string;
  manifestNumber: string;
  truckIdentifier: string | null;
  depotId: string;
  depotName: string;
  status: string;
  expectedParcelCount: number;
  scannedExpectedCount: number;
  scannedUnexpectedCount: number;
  openSessionId: string | null;
  createdAt: string;
}

export interface InboundExpectedParcel {
  manifestLineId: string;
  parcelId: string;
  trackingNumber: string;
  barcode: string;
  status: string;
  isScanned: boolean;
}

export interface InboundScannedParcel {
  id: string;
  parcelId: string;
  trackingNumber: string;
  barcode: string;
  matchType: string;
  status: string;
  scannedAt: string;
  scannedBy: string | null;
}

export interface InboundReceivingException {
  id: string;
  parcelId: string | null;
  manifestLineId: string | null;
  exceptionType: string;
  trackingNumber: string;
  barcode: string;
  createdAt: string;
}

export interface InboundReceivingSession {
  id: string;
  manifestId: string;
  manifestNumber: string;
  truckIdentifier: string | null;
  depotId: string;
  depotName: string;
  status: string;
  startedAt: string;
  startedBy: string | null;
  confirmedAt: string | null;
  confirmedBy: string | null;
  expectedParcelCount: number;
  scannedExpectedCount: number;
  scannedUnexpectedCount: number;
  remainingExpectedCount: number;
  expectedParcels: InboundExpectedParcel[];
  scannedParcels: InboundScannedParcel[];
  exceptions: InboundReceivingException[];
}

export interface StartInboundReceivingSessionRequest {
  manifestId: string;
}

export interface ScanInboundParcelRequest {
  sessionId: string;
  barcode: string;
}

export interface ConfirmInboundReceivingSessionRequest {
  sessionId: string;
}

export interface InboundParcelScanResult {
  sessionId: string;
  isExpected: boolean;
  scannedParcel: InboundScannedParcel;
  session: InboundReceivingSession;
}

export interface StagingRouteSummary {
  id: string;
  vehicleId: string;
  vehiclePlate: string;
  driverId: string;
  driverName: string;
  status: import("@/graphql/generated").RouteStatus;
  stagingArea: import("@/graphql/generated").StagingArea;
  startDate: string;
  expectedParcelCount: number;
  stagedParcelCount: number;
  remainingParcelCount: number;
}

export interface RouteStagingExpectedParcel {
  parcelId: string;
  trackingNumber: string;
  barcode: string;
  status: string;
  isStaged: boolean;
}

export interface RouteStagingBoard extends StagingRouteSummary {
  expectedParcels: RouteStagingExpectedParcel[];
}

export interface StageParcelForRouteRequest {
  routeId: string;
  barcode: string;
}

export interface StageParcelForRouteResult {
  outcome: import("@/graphql/generated").RouteStagingScanOutcome;
  message: string;
  trackingNumber: string | null;
  parcelId: string | null;
  conflictingRouteId: string | null;
  conflictingStagingArea: import("@/graphql/generated").StagingArea | null;
  board: RouteStagingBoard;
}

export interface LoadOutRouteSummary {
  id: string;
  vehicleId: string;
  vehiclePlate: string;
  driverId: string;
  driverName: string;
  status: import("@/graphql/generated").RouteStatus;
  stagingArea: import("@/graphql/generated").StagingArea;
  startDate: string;
  expectedParcelCount: number;
  loadedParcelCount: number;
  remainingParcelCount: number;
}

export interface RouteLoadOutExpectedParcel {
  parcelId: string;
  trackingNumber: string;
  barcode: string;
  status: string;
  isLoaded: boolean;
}

export interface RouteLoadOutBoard extends LoadOutRouteSummary {
  expectedParcels: RouteLoadOutExpectedParcel[];
}

export interface LoadParcelForRouteRequest {
  routeId: string;
  barcode: string;
}

export interface LoadParcelForRouteResult {
  outcome: import("@/graphql/generated").RouteLoadOutScanOutcome;
  message: string;
  trackingNumber: string | null;
  parcelId: string | null;
  conflictingRouteId: string | null;
  conflictingStagingArea: import("@/graphql/generated").StagingArea | null;
  board: RouteLoadOutBoard;
}

export interface CompleteLoadOutRequest {
  routeId: string;
  force: boolean;
}

export interface CompleteLoadOutResult {
  success: boolean;
  message: string;
  loadedCount: number;
  skippedCount: number;
  totalCount: number;
  board: RouteLoadOutBoard;
}
