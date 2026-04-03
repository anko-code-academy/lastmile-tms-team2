import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: { input: string; output: string; }
  /** The `Decimal` scalar type represents a decimal floating-point number. */
  Decimal: { input: number; output: number; }
  /** The `TimeSpan` scalar represents an ISO-8601 compliant duration type. */
  TimeSpan: { input: string; output: string; }
  UUID: { input: string; output: string; }
};

export type Address = {
  __typename?: 'Address';
  city: Scalars['String']['output'];
  companyName?: Maybe<Scalars['String']['output']>;
  contactName?: Maybe<Scalars['String']['output']>;
  countryCode: Scalars['String']['output'];
  email?: Maybe<Scalars['String']['output']>;
  geoLocation?: Maybe<GeoLocation>;
  isResidential: Scalars['Boolean']['output'];
  phone?: Maybe<Scalars['String']['output']>;
  postalCode: Scalars['String']['output'];
  state: Scalars['String']['output'];
  street1: Scalars['String']['output'];
  street2?: Maybe<Scalars['String']['output']>;
};

export type AddressFilterInput = {
  and?: InputMaybe<Array<AddressFilterInput>>;
  city?: InputMaybe<StringOperationFilterInput>;
  companyName?: InputMaybe<StringOperationFilterInput>;
  contactName?: InputMaybe<StringOperationFilterInput>;
  countryCode?: InputMaybe<StringOperationFilterInput>;
  email?: InputMaybe<StringOperationFilterInput>;
  isResidential?: InputMaybe<BooleanOperationFilterInput>;
  or?: InputMaybe<Array<AddressFilterInput>>;
  phone?: InputMaybe<StringOperationFilterInput>;
  postalCode?: InputMaybe<StringOperationFilterInput>;
  state?: InputMaybe<StringOperationFilterInput>;
  street1?: InputMaybe<StringOperationFilterInput>;
  street2?: InputMaybe<StringOperationFilterInput>;
};

export type AddressInput = {
  city: Scalars['String']['input'];
  companyName?: InputMaybe<Scalars['String']['input']>;
  contactName?: InputMaybe<Scalars['String']['input']>;
  countryCode: Scalars['String']['input'];
  email?: InputMaybe<Scalars['String']['input']>;
  isResidential: Scalars['Boolean']['input'];
  phone?: InputMaybe<Scalars['String']['input']>;
  postalCode: Scalars['String']['input'];
  state: Scalars['String']['input'];
  street1: Scalars['String']['input'];
  street2?: InputMaybe<Scalars['String']['input']>;
};

export type AddressSortInput = {
  city?: InputMaybe<SortEnumType>;
  companyName?: InputMaybe<SortEnumType>;
  contactName?: InputMaybe<SortEnumType>;
  countryCode?: InputMaybe<SortEnumType>;
  email?: InputMaybe<SortEnumType>;
  isResidential?: InputMaybe<SortEnumType>;
  phone?: InputMaybe<SortEnumType>;
  postalCode?: InputMaybe<SortEnumType>;
  state?: InputMaybe<SortEnumType>;
  street1?: InputMaybe<SortEnumType>;
  street2?: InputMaybe<SortEnumType>;
};

/** Defines when a policy shall be executed. */
export type ApplyPolicy =
  /** After the resolver was executed. */
  | 'AFTER_RESOLVER'
  /** Before the resolver was executed. */
  | 'BEFORE_RESOLVER'
  /** The policy is applied in the validation step before the execution. */
  | 'VALIDATION';

export type BooleanOperationFilterInput = {
  eq?: InputMaybe<Scalars['Boolean']['input']>;
  neq?: InputMaybe<Scalars['Boolean']['input']>;
};

export type CancelParcelInput = {
  id: Scalars['UUID']['input'];
  reason: Scalars['String']['input'];
};

export type CompletePasswordResetInput = {
  email: Scalars['String']['input'];
  newPassword: Scalars['String']['input'];
  token: Scalars['String']['input'];
};

export type CreateDepotInput = {
  address: AddressInput;
  isActive: Scalars['Boolean']['input'];
  name: Scalars['String']['input'];
  operatingHours?: InputMaybe<Array<OperatingHoursInput>>;
};

export type CreateDriverAvailabilityInput = {
  dayOfWeek: DayOfWeek;
  isAvailable: Scalars['Boolean']['input'];
  shiftEnd?: InputMaybe<Scalars['TimeSpan']['input']>;
  shiftStart?: InputMaybe<Scalars['TimeSpan']['input']>;
};

export type CreateDriverInput = {
  availabilitySchedule: Array<CreateDriverAvailabilityInput>;
  depotId: Scalars['UUID']['input'];
  email?: InputMaybe<Scalars['String']['input']>;
  firstName: Scalars['String']['input'];
  lastName: Scalars['String']['input'];
  licenseExpiryDate?: InputMaybe<Scalars['DateTime']['input']>;
  licenseNumber: Scalars['String']['input'];
  phone?: InputMaybe<Scalars['String']['input']>;
  photoUrl?: InputMaybe<Scalars['String']['input']>;
  status: DriverStatus;
  userId: Scalars['UUID']['input'];
  zoneId: Scalars['UUID']['input'];
};

export type CreateRouteInput = {
  driverId: Scalars['UUID']['input'];
  parcelIds: Array<Scalars['UUID']['input']>;
  startDate: Scalars['DateTime']['input'];
  startMileage: Scalars['Int']['input'];
  vehicleId: Scalars['UUID']['input'];
};

export type CreateUserInput = {
  depotId?: InputMaybe<Scalars['UUID']['input']>;
  email: Scalars['String']['input'];
  firstName: Scalars['String']['input'];
  lastName: Scalars['String']['input'];
  phone?: InputMaybe<Scalars['String']['input']>;
  role: UserRole;
  zoneId?: InputMaybe<Scalars['UUID']['input']>;
};

export type CreateVehicleInput = {
  depotId: Scalars['UUID']['input'];
  parcelCapacity: Scalars['Int']['input'];
  registrationPlate: Scalars['String']['input'];
  status: VehicleStatus;
  type: VehicleType;
  weightCapacity: Scalars['Decimal']['input'];
};

export type CreateZoneInput = {
  boundaryWkt?: InputMaybe<Scalars['String']['input']>;
  coordinates?: InputMaybe<Array<Array<Scalars['Float']['input']>>>;
  depotId: Scalars['UUID']['input'];
  geoJson?: InputMaybe<Scalars['String']['input']>;
  isActive: Scalars['Boolean']['input'];
  name: Scalars['String']['input'];
};

export type DateTimeOperationFilterInput = {
  eq?: InputMaybe<Scalars['DateTime']['input']>;
  gt?: InputMaybe<Scalars['DateTime']['input']>;
  gte?: InputMaybe<Scalars['DateTime']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['DateTime']['input']>>>;
  lt?: InputMaybe<Scalars['DateTime']['input']>;
  lte?: InputMaybe<Scalars['DateTime']['input']>;
  neq?: InputMaybe<Scalars['DateTime']['input']>;
  ngt?: InputMaybe<Scalars['DateTime']['input']>;
  ngte?: InputMaybe<Scalars['DateTime']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['DateTime']['input']>>>;
  nlt?: InputMaybe<Scalars['DateTime']['input']>;
  nlte?: InputMaybe<Scalars['DateTime']['input']>;
};

export type DayOfWeek =
  | 'FRIDAY'
  | 'MONDAY'
  | 'SATURDAY'
  | 'SUNDAY'
  | 'THURSDAY'
  | 'TUESDAY'
  | 'WEDNESDAY';

export type DayOfWeekOperationFilterInput = {
  eq?: InputMaybe<DayOfWeek>;
  in?: InputMaybe<Array<DayOfWeek>>;
  neq?: InputMaybe<DayOfWeek>;
  nin?: InputMaybe<Array<DayOfWeek>>;
};

export type DecimalOperationFilterInput = {
  eq?: InputMaybe<Scalars['Decimal']['input']>;
  gt?: InputMaybe<Scalars['Decimal']['input']>;
  gte?: InputMaybe<Scalars['Decimal']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['Decimal']['input']>>>;
  lt?: InputMaybe<Scalars['Decimal']['input']>;
  lte?: InputMaybe<Scalars['Decimal']['input']>;
  neq?: InputMaybe<Scalars['Decimal']['input']>;
  ngt?: InputMaybe<Scalars['Decimal']['input']>;
  ngte?: InputMaybe<Scalars['Decimal']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['Decimal']['input']>>>;
  nlt?: InputMaybe<Scalars['Decimal']['input']>;
  nlte?: InputMaybe<Scalars['Decimal']['input']>;
};

export type Depot = {
  __typename?: 'Depot';
  address?: Maybe<Address>;
  addressId: Scalars['UUID']['output'];
  createdAt: Scalars['DateTime']['output'];
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  name: Scalars['String']['output'];
  operatingHours?: Maybe<Array<OperatingHours>>;
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
};

export type DepotFilterInput = {
  address?: InputMaybe<AddressFilterInput>;
  and?: InputMaybe<Array<DepotFilterInput>>;
  createdAt?: InputMaybe<DateTimeOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  isActive?: InputMaybe<BooleanOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  operatingHours?: InputMaybe<OperatingHoursListFilterInput>;
  or?: InputMaybe<Array<DepotFilterInput>>;
  updatedAt?: InputMaybe<DateTimeOperationFilterInput>;
};

export type DepotSortInput = {
  address?: InputMaybe<AddressSortInput>;
  createdAt?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  isActive?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
  updatedAt?: InputMaybe<SortEnumType>;
};

export type DimensionUnit =
  | 'CM'
  | 'IN';

export type DimensionUnitOperationFilterInput = {
  eq?: InputMaybe<DimensionUnit>;
  in?: InputMaybe<Array<DimensionUnit>>;
  neq?: InputMaybe<DimensionUnit>;
  nin?: InputMaybe<Array<DimensionUnit>>;
};

export type Driver = {
  __typename?: 'Driver';
  availabilitySchedule?: Maybe<Array<DriverAvailability>>;
  createdAt: Scalars['DateTime']['output'];
  depotId: Scalars['UUID']['output'];
  depotName?: Maybe<Scalars['String']['output']>;
  displayName: Scalars['String']['output'];
  email?: Maybe<Scalars['String']['output']>;
  firstName: Scalars['String']['output'];
  id: Scalars['UUID']['output'];
  lastName: Scalars['String']['output'];
  licenseExpiryDate?: Maybe<Scalars['DateTime']['output']>;
  licenseNumber: Scalars['String']['output'];
  phone?: Maybe<Scalars['String']['output']>;
  photoUrl?: Maybe<Scalars['String']['output']>;
  status: DriverStatus;
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
  userId: Scalars['UUID']['output'];
  userName?: Maybe<Scalars['String']['output']>;
  zoneId: Scalars['UUID']['output'];
  zoneName?: Maybe<Scalars['String']['output']>;
};

export type DriverAvailability = {
  __typename?: 'DriverAvailability';
  dayOfWeek: DayOfWeek;
  id: Scalars['UUID']['output'];
  isAvailable: Scalars['Boolean']['output'];
  shiftEnd?: Maybe<Scalars['TimeSpan']['output']>;
  shiftStart?: Maybe<Scalars['TimeSpan']['output']>;
};

export type DriverFilterInput = {
  and?: InputMaybe<Array<DriverFilterInput>>;
  createdAt?: InputMaybe<DateTimeOperationFilterInput>;
  depotId?: InputMaybe<UuidOperationFilterInput>;
  email?: InputMaybe<StringOperationFilterInput>;
  firstName?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  lastName?: InputMaybe<StringOperationFilterInput>;
  licenseExpiryDate?: InputMaybe<DateTimeOperationFilterInput>;
  licenseNumber?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<DriverFilterInput>>;
  phone?: InputMaybe<StringOperationFilterInput>;
  status?: InputMaybe<DriverStatusOperationFilterInput>;
  updatedAt?: InputMaybe<DateTimeOperationFilterInput>;
  userId?: InputMaybe<UuidOperationFilterInput>;
  zoneId?: InputMaybe<UuidOperationFilterInput>;
};

export type DriverSortInput = {
  createdAt?: InputMaybe<SortEnumType>;
  depotId?: InputMaybe<SortEnumType>;
  email?: InputMaybe<SortEnumType>;
  firstName?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  lastName?: InputMaybe<SortEnumType>;
  licenseExpiryDate?: InputMaybe<SortEnumType>;
  licenseNumber?: InputMaybe<SortEnumType>;
  phone?: InputMaybe<SortEnumType>;
  status?: InputMaybe<SortEnumType>;
  updatedAt?: InputMaybe<SortEnumType>;
  userId?: InputMaybe<SortEnumType>;
  zoneId?: InputMaybe<SortEnumType>;
};

export type DriverStatus =
  | 'ACTIVE'
  | 'INACTIVE'
  | 'ON_LEAVE'
  | 'SUSPENDED';

export type DriverStatusOperationFilterInput = {
  eq?: InputMaybe<DriverStatus>;
  in?: InputMaybe<Array<DriverStatus>>;
  neq?: InputMaybe<DriverStatus>;
  nin?: InputMaybe<Array<DriverStatus>>;
};

export type GeoLocation = {
  __typename?: 'GeoLocation';
  latitude: Scalars['Float']['output'];
  longitude: Scalars['Float']['output'];
};

export type IntOperationFilterInput = {
  eq?: InputMaybe<Scalars['Int']['input']>;
  gt?: InputMaybe<Scalars['Int']['input']>;
  gte?: InputMaybe<Scalars['Int']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['Int']['input']>>>;
  lt?: InputMaybe<Scalars['Int']['input']>;
  lte?: InputMaybe<Scalars['Int']['input']>;
  neq?: InputMaybe<Scalars['Int']['input']>;
  ngt?: InputMaybe<Scalars['Int']['input']>;
  ngte?: InputMaybe<Scalars['Int']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['Int']['input']>>>;
  nlt?: InputMaybe<Scalars['Int']['input']>;
  nlte?: InputMaybe<Scalars['Int']['input']>;
};

export type Mutation = {
  __typename?: 'Mutation';
  cancelParcel?: Maybe<ParcelDetail>;
  completePasswordReset: UserActionResultDto;
  createDepot: Depot;
  createDriver: Driver;
  createRoute: Route;
  createUser: UserManagementUser;
  createVehicle: Vehicle;
  createZone: Zone;
  deactivateUser: UserManagementUser;
  deleteDepot: Scalars['Boolean']['output'];
  deleteDriver: Scalars['Boolean']['output'];
  deleteVehicle: Scalars['Boolean']['output'];
  deleteZone: Scalars['Boolean']['output'];
  registerParcel: RegisteredParcel;
  requestPasswordReset: UserActionResultDto;
  sendPasswordResetEmail: UserActionResultDto;
  updateDepot?: Maybe<Depot>;
  updateDriver: Driver;
  updateParcel?: Maybe<ParcelDetail>;
  updateUser: UserManagementUser;
  updateVehicle?: Maybe<Vehicle>;
  updateZone?: Maybe<Zone>;
};


export type MutationCancelParcelArgs = {
  input: CancelParcelInput;
};


export type MutationCompletePasswordResetArgs = {
  input: CompletePasswordResetInput;
};


export type MutationCreateDepotArgs = {
  input: CreateDepotInput;
};


export type MutationCreateDriverArgs = {
  input: CreateDriverInput;
};


export type MutationCreateRouteArgs = {
  input: CreateRouteInput;
};


export type MutationCreateUserArgs = {
  input: CreateUserInput;
};


export type MutationCreateVehicleArgs = {
  input: CreateVehicleInput;
};


export type MutationCreateZoneArgs = {
  input: CreateZoneInput;
};


export type MutationDeactivateUserArgs = {
  userId: Scalars['UUID']['input'];
};


export type MutationDeleteDepotArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationDeleteDriverArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationDeleteVehicleArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationDeleteZoneArgs = {
  id: Scalars['UUID']['input'];
};


export type MutationRegisterParcelArgs = {
  input: RegisterParcelInput;
};


export type MutationRequestPasswordResetArgs = {
  email: Scalars['String']['input'];
};


export type MutationSendPasswordResetEmailArgs = {
  userId: Scalars['UUID']['input'];
};


export type MutationUpdateDepotArgs = {
  id: Scalars['UUID']['input'];
  input: UpdateDepotInput;
};


export type MutationUpdateDriverArgs = {
  id: Scalars['UUID']['input'];
  input: UpdateDriverInput;
};


export type MutationUpdateParcelArgs = {
  input: UpdateParcelInput;
};


export type MutationUpdateUserArgs = {
  input: UpdateUserInput;
};


export type MutationUpdateVehicleArgs = {
  id: Scalars['UUID']['input'];
  input: UpdateVehicleInput;
};


export type MutationUpdateZoneArgs = {
  id: Scalars['UUID']['input'];
  input: UpdateZoneInput;
};

export type OperatingHours = {
  __typename?: 'OperatingHours';
  closedTime?: Maybe<Scalars['TimeSpan']['output']>;
  dayOfWeek: DayOfWeek;
  isClosed: Scalars['Boolean']['output'];
  openTime?: Maybe<Scalars['TimeSpan']['output']>;
};

export type OperatingHoursFilterInput = {
  and?: InputMaybe<Array<OperatingHoursFilterInput>>;
  closedTime?: InputMaybe<TimeSpanOperationFilterInput>;
  dayOfWeek?: InputMaybe<DayOfWeekOperationFilterInput>;
  isClosed?: InputMaybe<BooleanOperationFilterInput>;
  openTime?: InputMaybe<TimeSpanOperationFilterInput>;
  or?: InputMaybe<Array<OperatingHoursFilterInput>>;
};

export type OperatingHoursInput = {
  closedTime?: InputMaybe<Scalars['String']['input']>;
  dayOfWeek: DayOfWeek;
  isClosed: Scalars['Boolean']['input'];
  openTime?: InputMaybe<Scalars['String']['input']>;
};

export type OperatingHoursListFilterInput = {
  all?: InputMaybe<OperatingHoursFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<OperatingHoursFilterInput>;
  some?: InputMaybe<OperatingHoursFilterInput>;
};

export type ParcelChangeHistory = {
  __typename?: 'ParcelChangeHistory';
  action: Scalars['String']['output'];
  afterValue?: Maybe<Scalars['String']['output']>;
  beforeValue?: Maybe<Scalars['String']['output']>;
  changedAt: Scalars['DateTime']['output'];
  changedBy?: Maybe<Scalars['String']['output']>;
  fieldName: Scalars['String']['output'];
};

export type ParcelDetail = {
  __typename?: 'ParcelDetail';
  actualDeliveryDate?: Maybe<Scalars['DateTime']['output']>;
  canCancel: Scalars['Boolean']['output'];
  canEdit: Scalars['Boolean']['output'];
  cancellationReason?: Maybe<Scalars['String']['output']>;
  changeHistory: Array<ParcelChangeHistory>;
  createdAt: Scalars['DateTime']['output'];
  currency: Scalars['String']['output'];
  declaredValue: Scalars['Decimal']['output'];
  deliveryAttempts: Scalars['Int']['output'];
  depotId: Scalars['UUID']['output'];
  depotName?: Maybe<Scalars['String']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  dimensionUnit: Scalars['String']['output'];
  estimatedDeliveryDate: Scalars['DateTime']['output'];
  height: Scalars['Decimal']['output'];
  id: Scalars['UUID']['output'];
  lastModifiedAt?: Maybe<Scalars['DateTime']['output']>;
  length: Scalars['Decimal']['output'];
  parcelType?: Maybe<Scalars['String']['output']>;
  recipientAddress: ParcelDetailAddress;
  serviceType: Scalars['String']['output'];
  shipperAddressId: Scalars['UUID']['output'];
  status: Scalars['String']['output'];
  trackingNumber: Scalars['String']['output'];
  weight: Scalars['Decimal']['output'];
  weightUnit: Scalars['String']['output'];
  width: Scalars['Decimal']['output'];
  zoneId: Scalars['UUID']['output'];
  zoneName?: Maybe<Scalars['String']['output']>;
};

export type ParcelDetailAddress = {
  __typename?: 'ParcelDetailAddress';
  city: Scalars['String']['output'];
  companyName?: Maybe<Scalars['String']['output']>;
  contactName?: Maybe<Scalars['String']['output']>;
  countryCode: Scalars['String']['output'];
  email?: Maybe<Scalars['String']['output']>;
  isResidential: Scalars['Boolean']['output'];
  phone?: Maybe<Scalars['String']['output']>;
  postalCode: Scalars['String']['output'];
  state: Scalars['String']['output'];
  street1: Scalars['String']['output'];
  street2?: Maybe<Scalars['String']['output']>;
};

export type ParcelFilterInput = {
  and?: InputMaybe<Array<ParcelFilterInput>>;
  createdAt?: InputMaybe<DateTimeOperationFilterInput>;
  currency?: InputMaybe<StringOperationFilterInput>;
  declaredValue?: InputMaybe<DecimalOperationFilterInput>;
  deliveryAttempts?: InputMaybe<IntOperationFilterInput>;
  description?: InputMaybe<StringOperationFilterInput>;
  dimensionUnit?: InputMaybe<DimensionUnitOperationFilterInput>;
  estimatedDeliveryDate?: InputMaybe<DateTimeOperationFilterInput>;
  height?: InputMaybe<DecimalOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  lastModifiedAt?: InputMaybe<DateTimeOperationFilterInput>;
  length?: InputMaybe<DecimalOperationFilterInput>;
  or?: InputMaybe<Array<ParcelFilterInput>>;
  parcelType?: InputMaybe<StringOperationFilterInput>;
  serviceType?: InputMaybe<ServiceTypeOperationFilterInput>;
  status?: InputMaybe<ParcelStatusOperationFilterInput>;
  trackingNumber?: InputMaybe<StringOperationFilterInput>;
  weight?: InputMaybe<DecimalOperationFilterInput>;
  weightUnit?: InputMaybe<WeightUnitOperationFilterInput>;
  width?: InputMaybe<DecimalOperationFilterInput>;
};

export type ParcelImport = {
  __typename?: 'ParcelImport';
  completedAt?: Maybe<Scalars['DateTime']['output']>;
  createdAt: Scalars['DateTime']['output'];
  createdTrackingNumbers: Array<Scalars['String']['output']>;
  depotName?: Maybe<Scalars['String']['output']>;
  failureMessage?: Maybe<Scalars['String']['output']>;
  fileFormat: Scalars['String']['output'];
  fileName: Scalars['String']['output'];
  id: Scalars['UUID']['output'];
  importedRows: Scalars['Int']['output'];
  processedRows: Scalars['Int']['output'];
  rejectedRows: Scalars['Int']['output'];
  rowFailuresPreview: Array<ParcelImportRowFailurePreview>;
  startedAt?: Maybe<Scalars['DateTime']['output']>;
  status: Scalars['String']['output'];
  totalRows: Scalars['Int']['output'];
};

export type ParcelImportHistory = {
  __typename?: 'ParcelImportHistory';
  completedAt?: Maybe<Scalars['DateTime']['output']>;
  createdAt: Scalars['DateTime']['output'];
  depotName?: Maybe<Scalars['String']['output']>;
  fileFormat: Scalars['String']['output'];
  fileName: Scalars['String']['output'];
  id: Scalars['UUID']['output'];
  importedRows: Scalars['Int']['output'];
  processedRows: Scalars['Int']['output'];
  rejectedRows: Scalars['Int']['output'];
  startedAt?: Maybe<Scalars['DateTime']['output']>;
  status: Scalars['String']['output'];
  totalRows: Scalars['Int']['output'];
};

export type ParcelImportRowFailurePreview = {
  __typename?: 'ParcelImportRowFailurePreview';
  errorMessage: Scalars['String']['output'];
  originalRowValues: Scalars['String']['output'];
  rowNumber: Scalars['Int']['output'];
};

export type ParcelRouteOption = {
  __typename?: 'ParcelRouteOption';
  id: Scalars['UUID']['output'];
  trackingNumber: Scalars['String']['output'];
  weight: Scalars['Decimal']['output'];
  weightUnit: WeightUnit;
};

export type ParcelSortInput = {
  createdAt?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  lastModifiedAt?: InputMaybe<SortEnumType>;
  serviceType?: InputMaybe<SortEnumType>;
  status?: InputMaybe<SortEnumType>;
  trackingNumber?: InputMaybe<SortEnumType>;
  weight?: InputMaybe<SortEnumType>;
};

export type ParcelStatus =
  | 'CANCELLED'
  | 'DELIVERED'
  | 'EXCEPTION'
  | 'FAILED_ATTEMPT'
  | 'LOADED'
  | 'OUT_FOR_DELIVERY'
  | 'RECEIVED_AT_DEPOT'
  | 'REGISTERED'
  | 'RETURNED_TO_DEPOT'
  | 'SORTED'
  | 'STAGED';

export type ParcelStatusOperationFilterInput = {
  eq?: InputMaybe<ParcelStatus>;
  in?: InputMaybe<Array<ParcelStatus>>;
  neq?: InputMaybe<ParcelStatus>;
  nin?: InputMaybe<Array<ParcelStatus>>;
};

export type Query = {
  __typename?: 'Query';
  depot?: Maybe<Depot>;
  depots: Array<Depot>;
  driver?: Maybe<Driver>;
  drivers: Array<Driver>;
  parcel?: Maybe<ParcelDetail>;
  parcelImport?: Maybe<ParcelImport>;
  parcelImports: Array<ParcelImportHistory>;
  parcelsForRouteCreation: Array<ParcelRouteOption>;
  preLoadParcels: Array<RegisteredParcel>;
  registeredParcels: Array<RegisteredParcel>;
  routes: Array<Route>;
  user?: Maybe<UserManagementUser>;
  userManagementLookups: UserManagementLookupsDto;
  users: Array<UserManagementUser>;
  vehicles: Array<Vehicle>;
  zone?: Maybe<Zone>;
  zones: Array<Zone>;
};


export type QueryDepotArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryDepotsArgs = {
  order?: InputMaybe<Array<DepotSortInput>>;
  where?: InputMaybe<DepotFilterInput>;
};


export type QueryDriverArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryDriversArgs = {
  order?: InputMaybe<Array<DriverSortInput>>;
  where?: InputMaybe<DriverFilterInput>;
};


export type QueryParcelArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryParcelImportArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryPreLoadParcelsArgs = {
  order?: InputMaybe<Array<ParcelSortInput>>;
  where?: InputMaybe<ParcelFilterInput>;
};


export type QueryRegisteredParcelsArgs = {
  order?: InputMaybe<Array<ParcelSortInput>>;
  where?: InputMaybe<ParcelFilterInput>;
};


export type QueryRoutesArgs = {
  order?: InputMaybe<Array<RouteSortInput>>;
  where?: InputMaybe<RouteFilterInput>;
};


export type QueryUserArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryUsersArgs = {
  depotId?: InputMaybe<Scalars['UUID']['input']>;
  isActive?: InputMaybe<Scalars['Boolean']['input']>;
  order?: InputMaybe<Array<UserManagementUserSortInput>>;
  search?: InputMaybe<Scalars['String']['input']>;
  where?: InputMaybe<UserManagementUserFilterInput>;
  zoneId?: InputMaybe<Scalars['UUID']['input']>;
};


export type QueryVehiclesArgs = {
  order?: InputMaybe<Array<VehicleSortInput>>;
  where?: InputMaybe<VehicleFilterInput>;
};


export type QueryZoneArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryZonesArgs = {
  order?: InputMaybe<Array<ZoneSortInput>>;
  where?: InputMaybe<ZoneFilterInput>;
};

export type RegisterParcelInput = {
  currency: Scalars['String']['input'];
  declaredValue: Scalars['Decimal']['input'];
  description?: InputMaybe<Scalars['String']['input']>;
  dimensionUnit: DimensionUnit;
  estimatedDeliveryDate: Scalars['DateTime']['input'];
  height: Scalars['Decimal']['input'];
  length: Scalars['Decimal']['input'];
  parcelType?: InputMaybe<Scalars['String']['input']>;
  recipientAddress: RegisterParcelRecipientAddressInput;
  serviceType: ServiceType;
  shipperAddressId: Scalars['UUID']['input'];
  weight: Scalars['Decimal']['input'];
  weightUnit: WeightUnit;
  width: Scalars['Decimal']['input'];
};

export type RegisterParcelRecipientAddressInput = {
  city: Scalars['String']['input'];
  companyName?: InputMaybe<Scalars['String']['input']>;
  contactName?: InputMaybe<Scalars['String']['input']>;
  countryCode: Scalars['String']['input'];
  email?: InputMaybe<Scalars['String']['input']>;
  isResidential: Scalars['Boolean']['input'];
  phone?: InputMaybe<Scalars['String']['input']>;
  postalCode: Scalars['String']['input'];
  state: Scalars['String']['input'];
  street1: Scalars['String']['input'];
  street2?: InputMaybe<Scalars['String']['input']>;
};

export type RegisteredParcel = {
  __typename?: 'RegisteredParcel';
  actualDeliveryDate?: Maybe<Scalars['DateTime']['output']>;
  barcode: Scalars['String']['output'];
  createdAt: Scalars['DateTime']['output'];
  currency: Scalars['String']['output'];
  declaredValue: Scalars['Decimal']['output'];
  deliveryAttempts: Scalars['Int']['output'];
  depotId: Scalars['UUID']['output'];
  depotName?: Maybe<Scalars['String']['output']>;
  description?: Maybe<Scalars['String']['output']>;
  dimensionUnit: Scalars['String']['output'];
  estimatedDeliveryDate: Scalars['DateTime']['output'];
  height: Scalars['Decimal']['output'];
  id: Scalars['UUID']['output'];
  lastModifiedAt?: Maybe<Scalars['DateTime']['output']>;
  length: Scalars['Decimal']['output'];
  parcelType?: Maybe<Scalars['String']['output']>;
  serviceType: Scalars['String']['output'];
  status: Scalars['String']['output'];
  trackingNumber: Scalars['String']['output'];
  weight: Scalars['Decimal']['output'];
  weightUnit: Scalars['String']['output'];
  width: Scalars['Decimal']['output'];
  zoneId: Scalars['UUID']['output'];
  zoneName?: Maybe<Scalars['String']['output']>;
};

export type Route = {
  __typename?: 'Route';
  createdAt: Scalars['DateTime']['output'];
  driverId: Scalars['UUID']['output'];
  driverName?: Maybe<Scalars['String']['output']>;
  endDate?: Maybe<Scalars['DateTime']['output']>;
  endMileage: Scalars['Int']['output'];
  id: Scalars['UUID']['output'];
  parcelCount: Scalars['Int']['output'];
  parcelsDelivered: Scalars['Int']['output'];
  startDate: Scalars['DateTime']['output'];
  startMileage: Scalars['Int']['output'];
  status: RouteStatus;
  totalMileage: Scalars['Int']['output'];
  vehicleId: Scalars['UUID']['output'];
  vehiclePlate?: Maybe<Scalars['String']['output']>;
};

export type RouteFilterInput = {
  and?: InputMaybe<Array<RouteFilterInput>>;
  createdAt?: InputMaybe<DateTimeOperationFilterInput>;
  driverId?: InputMaybe<UuidOperationFilterInput>;
  endDate?: InputMaybe<DateTimeOperationFilterInput>;
  endMileage?: InputMaybe<IntOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  or?: InputMaybe<Array<RouteFilterInput>>;
  startDate?: InputMaybe<DateTimeOperationFilterInput>;
  startMileage?: InputMaybe<IntOperationFilterInput>;
  status?: InputMaybe<RouteStatusOperationFilterInput>;
  vehicleId?: InputMaybe<UuidOperationFilterInput>;
};

export type RouteSortInput = {
  createdAt?: InputMaybe<SortEnumType>;
  driverId?: InputMaybe<SortEnumType>;
  endDate?: InputMaybe<SortEnumType>;
  endMileage?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  startDate?: InputMaybe<SortEnumType>;
  startMileage?: InputMaybe<SortEnumType>;
  status?: InputMaybe<SortEnumType>;
  vehicleId?: InputMaybe<SortEnumType>;
};

export type RouteStatus =
  | 'CANCELLED'
  | 'COMPLETED'
  | 'IN_PROGRESS'
  | 'PLANNED';

export type RouteStatusOperationFilterInput = {
  eq?: InputMaybe<RouteStatus>;
  in?: InputMaybe<Array<RouteStatus>>;
  neq?: InputMaybe<RouteStatus>;
  nin?: InputMaybe<Array<RouteStatus>>;
};

export type ServiceType =
  | 'ECONOMY'
  | 'EXPRESS'
  | 'OVERNIGHT'
  | 'STANDARD';

export type ServiceTypeOperationFilterInput = {
  eq?: InputMaybe<ServiceType>;
  in?: InputMaybe<Array<ServiceType>>;
  neq?: InputMaybe<ServiceType>;
  nin?: InputMaybe<Array<ServiceType>>;
};

export type SortEnumType =
  | 'ASC'
  | 'DESC';

export type StringOperationFilterInput = {
  and?: InputMaybe<Array<StringOperationFilterInput>>;
  contains?: InputMaybe<Scalars['String']['input']>;
  endsWith?: InputMaybe<Scalars['String']['input']>;
  eq?: InputMaybe<Scalars['String']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  ncontains?: InputMaybe<Scalars['String']['input']>;
  nendsWith?: InputMaybe<Scalars['String']['input']>;
  neq?: InputMaybe<Scalars['String']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  nstartsWith?: InputMaybe<Scalars['String']['input']>;
  or?: InputMaybe<Array<StringOperationFilterInput>>;
  startsWith?: InputMaybe<Scalars['String']['input']>;
};

export type TimeSpanOperationFilterInput = {
  eq?: InputMaybe<Scalars['TimeSpan']['input']>;
  gt?: InputMaybe<Scalars['TimeSpan']['input']>;
  gte?: InputMaybe<Scalars['TimeSpan']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['TimeSpan']['input']>>>;
  lt?: InputMaybe<Scalars['TimeSpan']['input']>;
  lte?: InputMaybe<Scalars['TimeSpan']['input']>;
  neq?: InputMaybe<Scalars['TimeSpan']['input']>;
  ngt?: InputMaybe<Scalars['TimeSpan']['input']>;
  ngte?: InputMaybe<Scalars['TimeSpan']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['TimeSpan']['input']>>>;
  nlt?: InputMaybe<Scalars['TimeSpan']['input']>;
  nlte?: InputMaybe<Scalars['TimeSpan']['input']>;
};

export type UpdateDepotInput = {
  address?: InputMaybe<AddressInput>;
  isActive: Scalars['Boolean']['input'];
  name: Scalars['String']['input'];
  operatingHours?: InputMaybe<Array<OperatingHoursInput>>;
};

export type UpdateDriverAvailabilityInput = {
  dayOfWeek: DayOfWeek;
  id?: InputMaybe<Scalars['UUID']['input']>;
  isAvailable: Scalars['Boolean']['input'];
  shiftEnd?: InputMaybe<Scalars['TimeSpan']['input']>;
  shiftStart?: InputMaybe<Scalars['TimeSpan']['input']>;
};

export type UpdateDriverInput = {
  availabilitySchedule: Array<UpdateDriverAvailabilityInput>;
  depotId: Scalars['UUID']['input'];
  email?: InputMaybe<Scalars['String']['input']>;
  firstName: Scalars['String']['input'];
  lastName: Scalars['String']['input'];
  licenseExpiryDate?: InputMaybe<Scalars['DateTime']['input']>;
  licenseNumber: Scalars['String']['input'];
  phone?: InputMaybe<Scalars['String']['input']>;
  photoUrl?: InputMaybe<Scalars['String']['input']>;
  status: DriverStatus;
  userId: Scalars['UUID']['input'];
  zoneId: Scalars['UUID']['input'];
};

export type UpdateParcelInput = {
  currency: Scalars['String']['input'];
  declaredValue: Scalars['Decimal']['input'];
  description?: InputMaybe<Scalars['String']['input']>;
  dimensionUnit: DimensionUnit;
  estimatedDeliveryDate: Scalars['DateTime']['input'];
  height: Scalars['Decimal']['input'];
  id: Scalars['UUID']['input'];
  length: Scalars['Decimal']['input'];
  parcelType?: InputMaybe<Scalars['String']['input']>;
  recipientAddress: RegisterParcelRecipientAddressInput;
  serviceType: ServiceType;
  shipperAddressId: Scalars['UUID']['input'];
  weight: Scalars['Decimal']['input'];
  weightUnit: WeightUnit;
  width: Scalars['Decimal']['input'];
};

export type UpdateUserInput = {
  depotId?: InputMaybe<Scalars['UUID']['input']>;
  email: Scalars['String']['input'];
  firstName: Scalars['String']['input'];
  id: Scalars['UUID']['input'];
  isActive: Scalars['Boolean']['input'];
  lastName: Scalars['String']['input'];
  phone?: InputMaybe<Scalars['String']['input']>;
  role: UserRole;
  zoneId?: InputMaybe<Scalars['UUID']['input']>;
};

export type UpdateVehicleInput = {
  depotId: Scalars['UUID']['input'];
  parcelCapacity: Scalars['Int']['input'];
  registrationPlate: Scalars['String']['input'];
  status: VehicleStatus;
  type: VehicleType;
  weightCapacity: Scalars['Decimal']['input'];
};

export type UpdateZoneInput = {
  boundaryWkt?: InputMaybe<Scalars['String']['input']>;
  coordinates?: InputMaybe<Array<Array<Scalars['Float']['input']>>>;
  depotId: Scalars['UUID']['input'];
  geoJson?: InputMaybe<Scalars['String']['input']>;
  isActive: Scalars['Boolean']['input'];
  name: Scalars['String']['input'];
};

export type UserActionResultDto = {
  __typename?: 'UserActionResultDto';
  message: Scalars['String']['output'];
  success: Scalars['Boolean']['output'];
};

export type UserManagementDepotOptionDto = {
  __typename?: 'UserManagementDepotOptionDto';
  id: Scalars['UUID']['output'];
  name: Scalars['String']['output'];
};

export type UserManagementLookupsDto = {
  __typename?: 'UserManagementLookupsDto';
  depots: Array<UserManagementDepotOptionDto>;
  roles: Array<UserManagementRoleOptionDto>;
  zones: Array<UserManagementZoneOptionDto>;
};

export type UserManagementRoleOptionDto = {
  __typename?: 'UserManagementRoleOptionDto';
  label: Scalars['String']['output'];
  value: UserRole;
};

export type UserManagementUser = {
  __typename?: 'UserManagementUser';
  createdAt: Scalars['DateTime']['output'];
  depotId?: Maybe<Scalars['UUID']['output']>;
  depotName?: Maybe<Scalars['String']['output']>;
  email?: Maybe<Scalars['String']['output']>;
  firstName: Scalars['String']['output'];
  fullName: Scalars['String']['output'];
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  isProtected: Scalars['Boolean']['output'];
  lastName: Scalars['String']['output'];
  phone?: Maybe<Scalars['String']['output']>;
  role?: Maybe<Scalars['String']['output']>;
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
  zoneId?: Maybe<Scalars['UUID']['output']>;
  zoneName?: Maybe<Scalars['String']['output']>;
};

export type UserManagementUserFilterInput = {
  and?: InputMaybe<Array<UserManagementUserFilterInput>>;
  createdAt?: InputMaybe<DateTimeOperationFilterInput>;
  depotId?: InputMaybe<UuidOperationFilterInput>;
  email?: InputMaybe<StringOperationFilterInput>;
  firstName?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  isActive?: InputMaybe<BooleanOperationFilterInput>;
  lastName?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<UserManagementUserFilterInput>>;
  phone?: InputMaybe<StringOperationFilterInput>;
  updatedAt?: InputMaybe<DateTimeOperationFilterInput>;
  zoneId?: InputMaybe<UuidOperationFilterInput>;
};

export type UserManagementUserSortInput = {
  createdAt?: InputMaybe<SortEnumType>;
  depotId?: InputMaybe<SortEnumType>;
  email?: InputMaybe<SortEnumType>;
  firstName?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  isActive?: InputMaybe<SortEnumType>;
  lastName?: InputMaybe<SortEnumType>;
  phone?: InputMaybe<SortEnumType>;
  updatedAt?: InputMaybe<SortEnumType>;
  zoneId?: InputMaybe<SortEnumType>;
};

export type UserManagementZoneOptionDto = {
  __typename?: 'UserManagementZoneOptionDto';
  depotId: Scalars['UUID']['output'];
  id: Scalars['UUID']['output'];
  name: Scalars['String']['output'];
};

export type UserRole =
  | 'Admin'
  | 'Dispatcher'
  | 'Driver'
  | 'OperationsManager'
  | 'WarehouseOperator';

export type UuidOperationFilterInput = {
  eq?: InputMaybe<Scalars['UUID']['input']>;
  gt?: InputMaybe<Scalars['UUID']['input']>;
  gte?: InputMaybe<Scalars['UUID']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['UUID']['input']>>>;
  lt?: InputMaybe<Scalars['UUID']['input']>;
  lte?: InputMaybe<Scalars['UUID']['input']>;
  neq?: InputMaybe<Scalars['UUID']['input']>;
  ngt?: InputMaybe<Scalars['UUID']['input']>;
  ngte?: InputMaybe<Scalars['UUID']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['UUID']['input']>>>;
  nlt?: InputMaybe<Scalars['UUID']['input']>;
  nlte?: InputMaybe<Scalars['UUID']['input']>;
};

export type Vehicle = {
  __typename?: 'Vehicle';
  createdAt: Scalars['DateTime']['output'];
  depotId: Scalars['UUID']['output'];
  depotName?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  parcelCapacity: Scalars['Int']['output'];
  registrationPlate: Scalars['String']['output'];
  routesCompleted: Scalars['Int']['output'];
  status: VehicleStatus;
  totalMileage: Scalars['Int']['output'];
  totalRoutes: Scalars['Int']['output'];
  type: VehicleType;
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
  weightCapacity: Scalars['Decimal']['output'];
};

export type VehicleFilterInput = {
  and?: InputMaybe<Array<VehicleFilterInput>>;
  createdAt?: InputMaybe<DateTimeOperationFilterInput>;
  depotId?: InputMaybe<UuidOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  or?: InputMaybe<Array<VehicleFilterInput>>;
  parcelCapacity?: InputMaybe<IntOperationFilterInput>;
  registrationPlate?: InputMaybe<StringOperationFilterInput>;
  status?: InputMaybe<VehicleStatusOperationFilterInput>;
  type?: InputMaybe<VehicleTypeOperationFilterInput>;
  updatedAt?: InputMaybe<DateTimeOperationFilterInput>;
  weightCapacity?: InputMaybe<DecimalOperationFilterInput>;
};

export type VehicleSortInput = {
  createdAt?: InputMaybe<SortEnumType>;
  depotId?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  parcelCapacity?: InputMaybe<SortEnumType>;
  registrationPlate?: InputMaybe<SortEnumType>;
  status?: InputMaybe<SortEnumType>;
  type?: InputMaybe<SortEnumType>;
  updatedAt?: InputMaybe<SortEnumType>;
  weightCapacity?: InputMaybe<SortEnumType>;
};

export type VehicleStatus =
  | 'AVAILABLE'
  | 'IN_USE'
  | 'MAINTENANCE'
  | 'RETIRED';

export type VehicleStatusOperationFilterInput = {
  eq?: InputMaybe<VehicleStatus>;
  in?: InputMaybe<Array<VehicleStatus>>;
  neq?: InputMaybe<VehicleStatus>;
  nin?: InputMaybe<Array<VehicleStatus>>;
};

export type VehicleType =
  | 'BIKE'
  | 'CAR'
  | 'VAN';

export type VehicleTypeOperationFilterInput = {
  eq?: InputMaybe<VehicleType>;
  in?: InputMaybe<Array<VehicleType>>;
  neq?: InputMaybe<VehicleType>;
  nin?: InputMaybe<Array<VehicleType>>;
};

export type WeightUnit =
  | 'KG'
  | 'LB';

export type WeightUnitOperationFilterInput = {
  eq?: InputMaybe<WeightUnit>;
  in?: InputMaybe<Array<WeightUnit>>;
  neq?: InputMaybe<WeightUnit>;
  nin?: InputMaybe<Array<WeightUnit>>;
};

export type Zone = {
  __typename?: 'Zone';
  boundary?: Maybe<Scalars['String']['output']>;
  boundaryGeoJson?: Maybe<Scalars['String']['output']>;
  createdAt: Scalars['DateTime']['output'];
  depotId: Scalars['UUID']['output'];
  depotName?: Maybe<Scalars['String']['output']>;
  id: Scalars['UUID']['output'];
  isActive: Scalars['Boolean']['output'];
  name: Scalars['String']['output'];
  updatedAt?: Maybe<Scalars['DateTime']['output']>;
};

export type ZoneFilterInput = {
  and?: InputMaybe<Array<ZoneFilterInput>>;
  createdAt?: InputMaybe<DateTimeOperationFilterInput>;
  depotId?: InputMaybe<UuidOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  isActive?: InputMaybe<BooleanOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<ZoneFilterInput>>;
  updatedAt?: InputMaybe<DateTimeOperationFilterInput>;
};

export type ZoneSortInput = {
  createdAt?: InputMaybe<SortEnumType>;
  depotId?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  isActive?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
  updatedAt?: InputMaybe<SortEnumType>;
};

export type GetDepotsQueryVariables = Exact<{ [key: string]: never; }>;


export type GetDepotsQuery = { __typename?: 'Query', depots: Array<{ __typename?: 'Depot', id: string, name: string, addressId: string, isActive: boolean, createdAt: string, updatedAt?: string | null, address?: { __typename?: 'Address', street1: string, street2?: string | null, city: string, state: string, postalCode: string, countryCode: string, isResidential: boolean, contactName?: string | null, companyName?: string | null, phone?: string | null, email?: string | null, geoLocation?: { __typename?: 'GeoLocation', latitude: number, longitude: number } | null } | null, operatingHours?: Array<{ __typename?: 'OperatingHours', dayOfWeek: DayOfWeek, openTime?: string | null, closedTime?: string | null, isClosed: boolean }> | null }> };

export type GetDepotQueryVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type GetDepotQuery = { __typename?: 'Query', depot?: { __typename?: 'Depot', id: string, name: string, addressId: string, isActive: boolean, createdAt: string, updatedAt?: string | null, address?: { __typename?: 'Address', street1: string, street2?: string | null, city: string, state: string, postalCode: string, countryCode: string, isResidential: boolean, contactName?: string | null, companyName?: string | null, phone?: string | null, email?: string | null, geoLocation?: { __typename?: 'GeoLocation', latitude: number, longitude: number } | null } | null, operatingHours?: Array<{ __typename?: 'OperatingHours', dayOfWeek: DayOfWeek, openTime?: string | null, closedTime?: string | null, isClosed: boolean }> | null } | null };

export type CreateDepotMutationVariables = Exact<{
  input: CreateDepotInput;
}>;


export type CreateDepotMutation = { __typename?: 'Mutation', createDepot: { __typename?: 'Depot', id: string, name: string, addressId: string, isActive: boolean, createdAt: string, updatedAt?: string | null, address?: { __typename?: 'Address', street1: string, street2?: string | null, city: string, state: string, postalCode: string, countryCode: string, isResidential: boolean, contactName?: string | null, companyName?: string | null, phone?: string | null, email?: string | null, geoLocation?: { __typename?: 'GeoLocation', latitude: number, longitude: number } | null } | null, operatingHours?: Array<{ __typename?: 'OperatingHours', dayOfWeek: DayOfWeek, openTime?: string | null, closedTime?: string | null, isClosed: boolean }> | null } };

export type UpdateDepotMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
  input: UpdateDepotInput;
}>;


export type UpdateDepotMutation = { __typename?: 'Mutation', updateDepot?: { __typename?: 'Depot', id: string, name: string, addressId: string, isActive: boolean, createdAt: string, updatedAt?: string | null, address?: { __typename?: 'Address', street1: string, street2?: string | null, city: string, state: string, postalCode: string, countryCode: string, isResidential: boolean, contactName?: string | null, companyName?: string | null, phone?: string | null, email?: string | null, geoLocation?: { __typename?: 'GeoLocation', latitude: number, longitude: number } | null } | null, operatingHours?: Array<{ __typename?: 'OperatingHours', dayOfWeek: DayOfWeek, openTime?: string | null, closedTime?: string | null, isClosed: boolean }> | null } | null };

export type DeleteDepotMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type DeleteDepotMutation = { __typename?: 'Mutation', deleteDepot: boolean };

export type GetDriversQueryVariables = Exact<{
  where?: InputMaybe<DriverFilterInput>;
  order?: InputMaybe<Array<DriverSortInput> | DriverSortInput>;
}>;


export type GetDriversQuery = { __typename?: 'Query', drivers: Array<{ __typename?: 'Driver', id: string, displayName: string, firstName: string, lastName: string, phone?: string | null, email?: string | null, licenseNumber: string, licenseExpiryDate?: string | null, photoUrl?: string | null, zoneId: string, depotId: string, status: DriverStatus, userId: string, zoneName?: string | null, depotName?: string | null, userName?: string | null, createdAt: string, updatedAt?: string | null }> };

export type GetDriverQueryVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type GetDriverQuery = { __typename?: 'Query', driver?: { __typename?: 'Driver', id: string, displayName: string, firstName: string, lastName: string, phone?: string | null, email?: string | null, licenseNumber: string, licenseExpiryDate?: string | null, photoUrl?: string | null, zoneId: string, depotId: string, status: DriverStatus, userId: string, zoneName?: string | null, depotName?: string | null, userName?: string | null, createdAt: string, updatedAt?: string | null, availabilitySchedule?: Array<{ __typename?: 'DriverAvailability', id: string, dayOfWeek: DayOfWeek, shiftStart?: string | null, shiftEnd?: string | null, isAvailable: boolean }> | null } | null };

export type CreateDriverMutationVariables = Exact<{
  input: CreateDriverInput;
}>;


export type CreateDriverMutation = { __typename?: 'Mutation', createDriver: { __typename?: 'Driver', id: string, displayName: string, firstName: string, lastName: string, phone?: string | null, email?: string | null, licenseNumber: string, licenseExpiryDate?: string | null, photoUrl?: string | null, zoneId: string, depotId: string, status: DriverStatus, userId: string, zoneName?: string | null, depotName?: string | null, userName?: string | null, createdAt: string, updatedAt?: string | null, availabilitySchedule?: Array<{ __typename?: 'DriverAvailability', id: string, dayOfWeek: DayOfWeek, shiftStart?: string | null, shiftEnd?: string | null, isAvailable: boolean }> | null } };

export type UpdateDriverMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
  input: UpdateDriverInput;
}>;


export type UpdateDriverMutation = { __typename?: 'Mutation', updateDriver: { __typename?: 'Driver', id: string, displayName: string, firstName: string, lastName: string, phone?: string | null, email?: string | null, licenseNumber: string, licenseExpiryDate?: string | null, photoUrl?: string | null, zoneId: string, depotId: string, status: DriverStatus, userId: string, zoneName?: string | null, depotName?: string | null, userName?: string | null, createdAt: string, updatedAt?: string | null, availabilitySchedule?: Array<{ __typename?: 'DriverAvailability', id: string, dayOfWeek: DayOfWeek, shiftStart?: string | null, shiftEnd?: string | null, isAvailable: boolean }> | null } };

export type DeleteDriverMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type DeleteDriverMutation = { __typename?: 'Mutation', deleteDriver: boolean };

export type GetRegisteredParcelsQueryVariables = Exact<{ [key: string]: never; }>;


export type GetRegisteredParcelsQuery = { __typename?: 'Query', registeredParcels: Array<{ __typename?: 'RegisteredParcel', id: string, trackingNumber: string, status: string, serviceType: string, weight: number, weightUnit: string, parcelType?: string | null, createdAt: string, zoneName?: string | null }> };

export type GetPreLoadParcelsQueryVariables = Exact<{ [key: string]: never; }>;


export type GetPreLoadParcelsQuery = { __typename?: 'Query', preLoadParcels: Array<{ __typename?: 'RegisteredParcel', id: string, trackingNumber: string, status: string, serviceType: string, weight: number, weightUnit: string, parcelType?: string | null, createdAt: string, zoneName?: string | null, estimatedDeliveryDate: string }> };

export type GetParcelImportsQueryVariables = Exact<{ [key: string]: never; }>;


export type GetParcelImportsQuery = { __typename?: 'Query', parcelImports: Array<{ __typename?: 'ParcelImportHistory', id: string, fileName: string, fileFormat: string, status: string, totalRows: number, processedRows: number, importedRows: number, rejectedRows: number, depotName?: string | null, createdAt: string, startedAt?: string | null, completedAt?: string | null }> };

export type GetParcelImportQueryVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type GetParcelImportQuery = { __typename?: 'Query', parcelImport?: { __typename?: 'ParcelImport', id: string, fileName: string, fileFormat: string, status: string, totalRows: number, processedRows: number, importedRows: number, rejectedRows: number, depotName?: string | null, failureMessage?: string | null, createdAt: string, startedAt?: string | null, completedAt?: string | null, createdTrackingNumbers: Array<string>, rowFailuresPreview: Array<{ __typename?: 'ParcelImportRowFailurePreview', rowNumber: number, errorMessage: string, originalRowValues: string }> } | null };

export type GetParcelsForRouteCreationQueryVariables = Exact<{ [key: string]: never; }>;


export type GetParcelsForRouteCreationQuery = { __typename?: 'Query', parcelsForRouteCreation: Array<{ __typename?: 'ParcelRouteOption', id: string, trackingNumber: string, weight: number, weightUnit: WeightUnit }> };

export type GetParcelQueryVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type GetParcelQuery = { __typename?: 'Query', parcel?: { __typename?: 'ParcelDetail', id: string, trackingNumber: string, status: string, shipperAddressId: string, serviceType: string, weight: number, weightUnit: string, length: number, width: number, height: number, dimensionUnit: string, declaredValue: number, currency: string, description?: string | null, parcelType?: string | null, cancellationReason?: string | null, estimatedDeliveryDate: string, deliveryAttempts: number, zoneId: string, zoneName?: string | null, depotId: string, depotName?: string | null, createdAt: string, lastModifiedAt?: string | null, canEdit: boolean, canCancel: boolean, recipientAddress: { __typename?: 'ParcelDetailAddress', street1: string, street2?: string | null, city: string, state: string, postalCode: string, countryCode: string, isResidential: boolean, contactName?: string | null, companyName?: string | null, phone?: string | null, email?: string | null }, changeHistory: Array<{ __typename?: 'ParcelChangeHistory', action: string, fieldName: string, beforeValue?: string | null, afterValue?: string | null, changedAt: string, changedBy?: string | null }> } | null };

export type RegisterParcelMutationVariables = Exact<{
  input: RegisterParcelInput;
}>;


export type RegisterParcelMutation = { __typename?: 'Mutation', registerParcel: { __typename?: 'RegisteredParcel', id: string, trackingNumber: string, status: string, serviceType: string, weight: number, weightUnit: string, length: number, width: number, height: number, dimensionUnit: string, declaredValue: number, currency: string, description?: string | null, parcelType?: string | null, estimatedDeliveryDate: string, createdAt: string, zoneId: string, zoneName?: string | null, depotId: string, depotName?: string | null, barcode: string } };

export type UpdateParcelMutationVariables = Exact<{
  input: UpdateParcelInput;
}>;


export type UpdateParcelMutation = { __typename?: 'Mutation', updateParcel?: { __typename?: 'ParcelDetail', id: string, trackingNumber: string, status: string, shipperAddressId: string, serviceType: string, weight: number, weightUnit: string, length: number, width: number, height: number, dimensionUnit: string, declaredValue: number, currency: string, description?: string | null, parcelType?: string | null, cancellationReason?: string | null, estimatedDeliveryDate: string, deliveryAttempts: number, zoneId: string, zoneName?: string | null, depotId: string, depotName?: string | null, createdAt: string, lastModifiedAt?: string | null, canEdit: boolean, canCancel: boolean, recipientAddress: { __typename?: 'ParcelDetailAddress', street1: string, street2?: string | null, city: string, state: string, postalCode: string, countryCode: string, isResidential: boolean, contactName?: string | null, companyName?: string | null, phone?: string | null, email?: string | null }, changeHistory: Array<{ __typename?: 'ParcelChangeHistory', action: string, fieldName: string, beforeValue?: string | null, afterValue?: string | null, changedAt: string, changedBy?: string | null }> } | null };

export type CancelParcelMutationVariables = Exact<{
  input: CancelParcelInput;
}>;


export type CancelParcelMutation = { __typename?: 'Mutation', cancelParcel?: { __typename?: 'ParcelDetail', id: string, trackingNumber: string, status: string, shipperAddressId: string, serviceType: string, weight: number, weightUnit: string, length: number, width: number, height: number, dimensionUnit: string, declaredValue: number, currency: string, description?: string | null, parcelType?: string | null, cancellationReason?: string | null, estimatedDeliveryDate: string, deliveryAttempts: number, zoneId: string, zoneName?: string | null, depotId: string, depotName?: string | null, createdAt: string, canEdit: boolean, canCancel: boolean, lastModifiedAt?: string | null, recipientAddress: { __typename?: 'ParcelDetailAddress', street1: string, street2?: string | null, city: string, state: string, postalCode: string, countryCode: string, isResidential: boolean, contactName?: string | null, companyName?: string | null, phone?: string | null, email?: string | null }, changeHistory: Array<{ __typename?: 'ParcelChangeHistory', action: string, fieldName: string, beforeValue?: string | null, afterValue?: string | null, changedAt: string, changedBy?: string | null }> } | null };

export type GetRoutesQueryVariables = Exact<{
  where?: InputMaybe<RouteFilterInput>;
  order?: InputMaybe<Array<RouteSortInput> | RouteSortInput>;
}>;


export type GetRoutesQuery = { __typename?: 'Query', routes: Array<{ __typename?: 'Route', id: string, vehicleId: string, vehiclePlate?: string | null, driverId: string, driverName?: string | null, startDate: string, endDate?: string | null, startMileage: number, endMileage: number, totalMileage: number, status: RouteStatus, parcelCount: number, parcelsDelivered: number, createdAt: string }> };

export type CreateRouteMutationVariables = Exact<{
  input: CreateRouteInput;
}>;


export type CreateRouteMutation = { __typename?: 'Mutation', createRoute: { __typename?: 'Route', id: string, vehicleId: string, vehiclePlate?: string | null, driverId: string, driverName?: string | null, startDate: string, endDate?: string | null, startMileage: number, endMileage: number, totalMileage: number, status: RouteStatus, parcelCount: number, parcelsDelivered: number, createdAt: string } };

export type UserManagementLookupsQueryVariables = Exact<{ [key: string]: never; }>;


export type UserManagementLookupsQuery = { __typename?: 'Query', userManagementLookups: { __typename?: 'UserManagementLookupsDto', roles: Array<{ __typename?: 'UserManagementRoleOptionDto', value: UserRole, label: string }>, depots: Array<{ __typename?: 'UserManagementDepotOptionDto', id: string, name: string }>, zones: Array<{ __typename?: 'UserManagementZoneOptionDto', id: string, depotId: string, name: string }> } };

export type UsersQueryVariables = Exact<{
  search?: InputMaybe<Scalars['String']['input']>;
  isActive?: InputMaybe<Scalars['Boolean']['input']>;
  depotId?: InputMaybe<Scalars['UUID']['input']>;
  zoneId?: InputMaybe<Scalars['UUID']['input']>;
}>;


export type UsersQuery = { __typename?: 'Query', users: Array<{ __typename?: 'UserManagementUser', id: string, firstName: string, lastName: string, fullName: string, email?: string | null, phone?: string | null, role?: string | null, isActive: boolean, isProtected: boolean, depotId?: string | null, depotName?: string | null, zoneId?: string | null, zoneName?: string | null, createdAt: string, updatedAt?: string | null }> };

export type CreateUserMutationVariables = Exact<{
  input: CreateUserInput;
}>;


export type CreateUserMutation = { __typename?: 'Mutation', createUser: { __typename?: 'UserManagementUser', id: string, firstName: string, lastName: string, fullName: string, email?: string | null, phone?: string | null, role?: string | null, isActive: boolean, isProtected: boolean, depotId?: string | null, depotName?: string | null, zoneId?: string | null, zoneName?: string | null, createdAt: string, updatedAt?: string | null } };

export type UpdateUserMutationVariables = Exact<{
  input: UpdateUserInput;
}>;


export type UpdateUserMutation = { __typename?: 'Mutation', updateUser: { __typename?: 'UserManagementUser', id: string, firstName: string, lastName: string, fullName: string, email?: string | null, phone?: string | null, role?: string | null, isActive: boolean, isProtected: boolean, depotId?: string | null, depotName?: string | null, zoneId?: string | null, zoneName?: string | null, createdAt: string, updatedAt?: string | null } };

export type DeactivateUserMutationVariables = Exact<{
  userId: Scalars['UUID']['input'];
}>;


export type DeactivateUserMutation = { __typename?: 'Mutation', deactivateUser: { __typename?: 'UserManagementUser', id: string, firstName: string, lastName: string, fullName: string, email?: string | null, phone?: string | null, role?: string | null, isActive: boolean, isProtected: boolean, depotId?: string | null, depotName?: string | null, zoneId?: string | null, zoneName?: string | null, createdAt: string, updatedAt?: string | null } };

export type SendPasswordResetEmailMutationVariables = Exact<{
  userId: Scalars['UUID']['input'];
}>;


export type SendPasswordResetEmailMutation = { __typename?: 'Mutation', sendPasswordResetEmail: { __typename?: 'UserActionResultDto', success: boolean, message: string } };

export type CompletePasswordResetMutationVariables = Exact<{
  input: CompletePasswordResetInput;
}>;


export type CompletePasswordResetMutation = { __typename?: 'Mutation', completePasswordReset: { __typename?: 'UserActionResultDto', success: boolean, message: string } };

export type RequestPasswordResetMutationVariables = Exact<{
  email: Scalars['String']['input'];
}>;


export type RequestPasswordResetMutation = { __typename?: 'Mutation', requestPasswordReset: { __typename?: 'UserActionResultDto', success: boolean, message: string } };

export type GetVehiclesQueryVariables = Exact<{
  where?: InputMaybe<VehicleFilterInput>;
  order?: InputMaybe<Array<VehicleSortInput> | VehicleSortInput>;
}>;


export type GetVehiclesQuery = { __typename?: 'Query', vehicles: Array<{ __typename?: 'Vehicle', id: string, registrationPlate: string, type: VehicleType, parcelCapacity: number, weightCapacity: number, status: VehicleStatus, depotId: string, depotName?: string | null, totalRoutes: number, routesCompleted: number, totalMileage: number, createdAt: string, updatedAt?: string | null }> };

export type CreateVehicleMutationVariables = Exact<{
  input: CreateVehicleInput;
}>;


export type CreateVehicleMutation = { __typename?: 'Mutation', createVehicle: { __typename?: 'Vehicle', id: string, registrationPlate: string, type: VehicleType, parcelCapacity: number, weightCapacity: number, status: VehicleStatus, depotId: string, depotName?: string | null, totalRoutes: number, routesCompleted: number, totalMileage: number, createdAt: string, updatedAt?: string | null } };

export type UpdateVehicleMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
  input: UpdateVehicleInput;
}>;


export type UpdateVehicleMutation = { __typename?: 'Mutation', updateVehicle?: { __typename?: 'Vehicle', id: string, registrationPlate: string, type: VehicleType, parcelCapacity: number, weightCapacity: number, status: VehicleStatus, depotId: string, depotName?: string | null, totalRoutes: number, routesCompleted: number, totalMileage: number, createdAt: string, updatedAt?: string | null } | null };

export type DeleteVehicleMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type DeleteVehicleMutation = { __typename?: 'Mutation', deleteVehicle: boolean };

export type GetZonesQueryVariables = Exact<{ [key: string]: never; }>;


export type GetZonesQuery = { __typename?: 'Query', zones: Array<{ __typename?: 'Zone', id: string, name: string, boundary?: string | null, boundaryGeoJson?: string | null, isActive: boolean, depotId: string, depotName?: string | null, createdAt: string, updatedAt?: string | null }> };

export type GetZoneQueryVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type GetZoneQuery = { __typename?: 'Query', zone?: { __typename?: 'Zone', id: string, name: string, boundary?: string | null, boundaryGeoJson?: string | null, isActive: boolean, depotId: string, depotName?: string | null, createdAt: string, updatedAt?: string | null } | null };

export type CreateZoneMutationVariables = Exact<{
  input: CreateZoneInput;
}>;


export type CreateZoneMutation = { __typename?: 'Mutation', createZone: { __typename?: 'Zone', id: string, name: string, boundary?: string | null, boundaryGeoJson?: string | null, isActive: boolean, depotId: string, depotName?: string | null, createdAt: string, updatedAt?: string | null } };

export type UpdateZoneMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
  input: UpdateZoneInput;
}>;


export type UpdateZoneMutation = { __typename?: 'Mutation', updateZone?: { __typename?: 'Zone', id: string, name: string, boundary?: string | null, boundaryGeoJson?: string | null, isActive: boolean, depotId: string, depotName?: string | null, createdAt: string, updatedAt?: string | null } | null };

export type DeleteZoneMutationVariables = Exact<{
  id: Scalars['UUID']['input'];
}>;


export type DeleteZoneMutation = { __typename?: 'Mutation', deleteZone: boolean };


export const GetDepotsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetDepots"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"depots"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"addressId"}},{"kind":"Field","name":{"kind":"Name","value":"address"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"street1"}},{"kind":"Field","name":{"kind":"Name","value":"street2"}},{"kind":"Field","name":{"kind":"Name","value":"city"}},{"kind":"Field","name":{"kind":"Name","value":"state"}},{"kind":"Field","name":{"kind":"Name","value":"postalCode"}},{"kind":"Field","name":{"kind":"Name","value":"countryCode"}},{"kind":"Field","name":{"kind":"Name","value":"isResidential"}},{"kind":"Field","name":{"kind":"Name","value":"contactName"}},{"kind":"Field","name":{"kind":"Name","value":"companyName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"geoLocation"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"latitude"}},{"kind":"Field","name":{"kind":"Name","value":"longitude"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"operatingHours"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"dayOfWeek"}},{"kind":"Field","name":{"kind":"Name","value":"openTime"}},{"kind":"Field","name":{"kind":"Name","value":"closedTime"}},{"kind":"Field","name":{"kind":"Name","value":"isClosed"}}]}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetDepotsQuery, GetDepotsQueryVariables>;
export const GetDepotDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetDepot"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"depot"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"addressId"}},{"kind":"Field","name":{"kind":"Name","value":"address"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"street1"}},{"kind":"Field","name":{"kind":"Name","value":"street2"}},{"kind":"Field","name":{"kind":"Name","value":"city"}},{"kind":"Field","name":{"kind":"Name","value":"state"}},{"kind":"Field","name":{"kind":"Name","value":"postalCode"}},{"kind":"Field","name":{"kind":"Name","value":"countryCode"}},{"kind":"Field","name":{"kind":"Name","value":"isResidential"}},{"kind":"Field","name":{"kind":"Name","value":"contactName"}},{"kind":"Field","name":{"kind":"Name","value":"companyName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"geoLocation"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"latitude"}},{"kind":"Field","name":{"kind":"Name","value":"longitude"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"operatingHours"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"dayOfWeek"}},{"kind":"Field","name":{"kind":"Name","value":"openTime"}},{"kind":"Field","name":{"kind":"Name","value":"closedTime"}},{"kind":"Field","name":{"kind":"Name","value":"isClosed"}}]}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetDepotQuery, GetDepotQueryVariables>;
export const CreateDepotDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateDepot"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateDepotInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createDepot"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"addressId"}},{"kind":"Field","name":{"kind":"Name","value":"address"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"street1"}},{"kind":"Field","name":{"kind":"Name","value":"street2"}},{"kind":"Field","name":{"kind":"Name","value":"city"}},{"kind":"Field","name":{"kind":"Name","value":"state"}},{"kind":"Field","name":{"kind":"Name","value":"postalCode"}},{"kind":"Field","name":{"kind":"Name","value":"countryCode"}},{"kind":"Field","name":{"kind":"Name","value":"isResidential"}},{"kind":"Field","name":{"kind":"Name","value":"contactName"}},{"kind":"Field","name":{"kind":"Name","value":"companyName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"geoLocation"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"latitude"}},{"kind":"Field","name":{"kind":"Name","value":"longitude"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"operatingHours"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"dayOfWeek"}},{"kind":"Field","name":{"kind":"Name","value":"openTime"}},{"kind":"Field","name":{"kind":"Name","value":"closedTime"}},{"kind":"Field","name":{"kind":"Name","value":"isClosed"}}]}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<CreateDepotMutation, CreateDepotMutationVariables>;
export const UpdateDepotDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateDepot"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateDepotInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateDepot"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}},{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"addressId"}},{"kind":"Field","name":{"kind":"Name","value":"address"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"street1"}},{"kind":"Field","name":{"kind":"Name","value":"street2"}},{"kind":"Field","name":{"kind":"Name","value":"city"}},{"kind":"Field","name":{"kind":"Name","value":"state"}},{"kind":"Field","name":{"kind":"Name","value":"postalCode"}},{"kind":"Field","name":{"kind":"Name","value":"countryCode"}},{"kind":"Field","name":{"kind":"Name","value":"isResidential"}},{"kind":"Field","name":{"kind":"Name","value":"contactName"}},{"kind":"Field","name":{"kind":"Name","value":"companyName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"geoLocation"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"latitude"}},{"kind":"Field","name":{"kind":"Name","value":"longitude"}}]}}]}},{"kind":"Field","name":{"kind":"Name","value":"operatingHours"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"dayOfWeek"}},{"kind":"Field","name":{"kind":"Name","value":"openTime"}},{"kind":"Field","name":{"kind":"Name","value":"closedTime"}},{"kind":"Field","name":{"kind":"Name","value":"isClosed"}}]}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<UpdateDepotMutation, UpdateDepotMutationVariables>;
export const DeleteDepotDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteDepot"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteDepot"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}]}]}}]} as unknown as DocumentNode<DeleteDepotMutation, DeleteDepotMutationVariables>;
export const GetDriversDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetDrivers"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"where"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"DriverFilterInput"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"order"}},"type":{"kind":"ListType","type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"DriverSortInput"}}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"drivers"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"where"},"value":{"kind":"Variable","name":{"kind":"Name","value":"where"}}},{"kind":"Argument","name":{"kind":"Name","value":"order"},"value":{"kind":"Variable","name":{"kind":"Name","value":"order"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"displayName"}},{"kind":"Field","name":{"kind":"Name","value":"firstName"}},{"kind":"Field","name":{"kind":"Name","value":"lastName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"licenseNumber"}},{"kind":"Field","name":{"kind":"Name","value":"licenseExpiryDate"}},{"kind":"Field","name":{"kind":"Name","value":"photoUrl"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"userId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"userName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetDriversQuery, GetDriversQueryVariables>;
export const GetDriverDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetDriver"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"driver"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"displayName"}},{"kind":"Field","name":{"kind":"Name","value":"firstName"}},{"kind":"Field","name":{"kind":"Name","value":"lastName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"licenseNumber"}},{"kind":"Field","name":{"kind":"Name","value":"licenseExpiryDate"}},{"kind":"Field","name":{"kind":"Name","value":"photoUrl"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"userId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"userName"}},{"kind":"Field","name":{"kind":"Name","value":"availabilitySchedule"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"dayOfWeek"}},{"kind":"Field","name":{"kind":"Name","value":"shiftStart"}},{"kind":"Field","name":{"kind":"Name","value":"shiftEnd"}},{"kind":"Field","name":{"kind":"Name","value":"isAvailable"}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetDriverQuery, GetDriverQueryVariables>;
export const CreateDriverDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateDriver"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateDriverInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createDriver"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"displayName"}},{"kind":"Field","name":{"kind":"Name","value":"firstName"}},{"kind":"Field","name":{"kind":"Name","value":"lastName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"licenseNumber"}},{"kind":"Field","name":{"kind":"Name","value":"licenseExpiryDate"}},{"kind":"Field","name":{"kind":"Name","value":"photoUrl"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"userId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"userName"}},{"kind":"Field","name":{"kind":"Name","value":"availabilitySchedule"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"dayOfWeek"}},{"kind":"Field","name":{"kind":"Name","value":"shiftStart"}},{"kind":"Field","name":{"kind":"Name","value":"shiftEnd"}},{"kind":"Field","name":{"kind":"Name","value":"isAvailable"}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<CreateDriverMutation, CreateDriverMutationVariables>;
export const UpdateDriverDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateDriver"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateDriverInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateDriver"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}},{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"displayName"}},{"kind":"Field","name":{"kind":"Name","value":"firstName"}},{"kind":"Field","name":{"kind":"Name","value":"lastName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"licenseNumber"}},{"kind":"Field","name":{"kind":"Name","value":"licenseExpiryDate"}},{"kind":"Field","name":{"kind":"Name","value":"photoUrl"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"userId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"userName"}},{"kind":"Field","name":{"kind":"Name","value":"availabilitySchedule"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"dayOfWeek"}},{"kind":"Field","name":{"kind":"Name","value":"shiftStart"}},{"kind":"Field","name":{"kind":"Name","value":"shiftEnd"}},{"kind":"Field","name":{"kind":"Name","value":"isAvailable"}}]}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<UpdateDriverMutation, UpdateDriverMutationVariables>;
export const DeleteDriverDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteDriver"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteDriver"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}]}]}}]} as unknown as DocumentNode<DeleteDriverMutation, DeleteDriverMutationVariables>;
export const GetRegisteredParcelsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetRegisteredParcels"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"registeredParcels"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"trackingNumber"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"serviceType"}},{"kind":"Field","name":{"kind":"Name","value":"weight"}},{"kind":"Field","name":{"kind":"Name","value":"weightUnit"}},{"kind":"Field","name":{"kind":"Name","value":"parcelType"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}}]}}]}}]} as unknown as DocumentNode<GetRegisteredParcelsQuery, GetRegisteredParcelsQueryVariables>;
export const GetPreLoadParcelsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetPreLoadParcels"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"preLoadParcels"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"trackingNumber"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"serviceType"}},{"kind":"Field","name":{"kind":"Name","value":"weight"}},{"kind":"Field","name":{"kind":"Name","value":"weightUnit"}},{"kind":"Field","name":{"kind":"Name","value":"parcelType"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedDeliveryDate"}}]}}]}}]} as unknown as DocumentNode<GetPreLoadParcelsQuery, GetPreLoadParcelsQueryVariables>;
export const GetParcelImportsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetParcelImports"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"parcelImports"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"fileName"}},{"kind":"Field","name":{"kind":"Name","value":"fileFormat"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"totalRows"}},{"kind":"Field","name":{"kind":"Name","value":"processedRows"}},{"kind":"Field","name":{"kind":"Name","value":"importedRows"}},{"kind":"Field","name":{"kind":"Name","value":"rejectedRows"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"startedAt"}},{"kind":"Field","name":{"kind":"Name","value":"completedAt"}}]}}]}}]} as unknown as DocumentNode<GetParcelImportsQuery, GetParcelImportsQueryVariables>;
export const GetParcelImportDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetParcelImport"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"parcelImport"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"fileName"}},{"kind":"Field","name":{"kind":"Name","value":"fileFormat"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"totalRows"}},{"kind":"Field","name":{"kind":"Name","value":"processedRows"}},{"kind":"Field","name":{"kind":"Name","value":"importedRows"}},{"kind":"Field","name":{"kind":"Name","value":"rejectedRows"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"failureMessage"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"startedAt"}},{"kind":"Field","name":{"kind":"Name","value":"completedAt"}},{"kind":"Field","name":{"kind":"Name","value":"createdTrackingNumbers"}},{"kind":"Field","name":{"kind":"Name","value":"rowFailuresPreview"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"rowNumber"}},{"kind":"Field","name":{"kind":"Name","value":"errorMessage"}},{"kind":"Field","name":{"kind":"Name","value":"originalRowValues"}}]}}]}}]}}]} as unknown as DocumentNode<GetParcelImportQuery, GetParcelImportQueryVariables>;
export const GetParcelsForRouteCreationDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetParcelsForRouteCreation"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"parcelsForRouteCreation"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"trackingNumber"}},{"kind":"Field","name":{"kind":"Name","value":"weight"}},{"kind":"Field","name":{"kind":"Name","value":"weightUnit"}}]}}]}}]} as unknown as DocumentNode<GetParcelsForRouteCreationQuery, GetParcelsForRouteCreationQueryVariables>;
export const GetParcelDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetParcel"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"parcel"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"trackingNumber"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"shipperAddressId"}},{"kind":"Field","name":{"kind":"Name","value":"serviceType"}},{"kind":"Field","name":{"kind":"Name","value":"weight"}},{"kind":"Field","name":{"kind":"Name","value":"weightUnit"}},{"kind":"Field","name":{"kind":"Name","value":"length"}},{"kind":"Field","name":{"kind":"Name","value":"width"}},{"kind":"Field","name":{"kind":"Name","value":"height"}},{"kind":"Field","name":{"kind":"Name","value":"dimensionUnit"}},{"kind":"Field","name":{"kind":"Name","value":"declaredValue"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"parcelType"}},{"kind":"Field","name":{"kind":"Name","value":"cancellationReason"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedDeliveryDate"}},{"kind":"Field","name":{"kind":"Name","value":"deliveryAttempts"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"lastModifiedAt"}},{"kind":"Field","name":{"kind":"Name","value":"canEdit"}},{"kind":"Field","name":{"kind":"Name","value":"canCancel"}},{"kind":"Field","name":{"kind":"Name","value":"recipientAddress"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"street1"}},{"kind":"Field","name":{"kind":"Name","value":"street2"}},{"kind":"Field","name":{"kind":"Name","value":"city"}},{"kind":"Field","name":{"kind":"Name","value":"state"}},{"kind":"Field","name":{"kind":"Name","value":"postalCode"}},{"kind":"Field","name":{"kind":"Name","value":"countryCode"}},{"kind":"Field","name":{"kind":"Name","value":"isResidential"}},{"kind":"Field","name":{"kind":"Name","value":"contactName"}},{"kind":"Field","name":{"kind":"Name","value":"companyName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"changeHistory"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"action"}},{"kind":"Field","name":{"kind":"Name","value":"fieldName"}},{"kind":"Field","name":{"kind":"Name","value":"beforeValue"}},{"kind":"Field","name":{"kind":"Name","value":"afterValue"}},{"kind":"Field","name":{"kind":"Name","value":"changedAt"}},{"kind":"Field","name":{"kind":"Name","value":"changedBy"}}]}}]}}]}}]} as unknown as DocumentNode<GetParcelQuery, GetParcelQueryVariables>;
export const RegisterParcelDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"RegisterParcel"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"RegisterParcelInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"registerParcel"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"trackingNumber"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"serviceType"}},{"kind":"Field","name":{"kind":"Name","value":"weight"}},{"kind":"Field","name":{"kind":"Name","value":"weightUnit"}},{"kind":"Field","name":{"kind":"Name","value":"length"}},{"kind":"Field","name":{"kind":"Name","value":"width"}},{"kind":"Field","name":{"kind":"Name","value":"height"}},{"kind":"Field","name":{"kind":"Name","value":"dimensionUnit"}},{"kind":"Field","name":{"kind":"Name","value":"declaredValue"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"parcelType"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedDeliveryDate"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"barcode"}}]}}]}}]} as unknown as DocumentNode<RegisterParcelMutation, RegisterParcelMutationVariables>;
export const UpdateParcelDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateParcel"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateParcelInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateParcel"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"trackingNumber"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"shipperAddressId"}},{"kind":"Field","name":{"kind":"Name","value":"serviceType"}},{"kind":"Field","name":{"kind":"Name","value":"weight"}},{"kind":"Field","name":{"kind":"Name","value":"weightUnit"}},{"kind":"Field","name":{"kind":"Name","value":"length"}},{"kind":"Field","name":{"kind":"Name","value":"width"}},{"kind":"Field","name":{"kind":"Name","value":"height"}},{"kind":"Field","name":{"kind":"Name","value":"dimensionUnit"}},{"kind":"Field","name":{"kind":"Name","value":"declaredValue"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"parcelType"}},{"kind":"Field","name":{"kind":"Name","value":"cancellationReason"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedDeliveryDate"}},{"kind":"Field","name":{"kind":"Name","value":"deliveryAttempts"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"lastModifiedAt"}},{"kind":"Field","name":{"kind":"Name","value":"canEdit"}},{"kind":"Field","name":{"kind":"Name","value":"canCancel"}},{"kind":"Field","name":{"kind":"Name","value":"recipientAddress"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"street1"}},{"kind":"Field","name":{"kind":"Name","value":"street2"}},{"kind":"Field","name":{"kind":"Name","value":"city"}},{"kind":"Field","name":{"kind":"Name","value":"state"}},{"kind":"Field","name":{"kind":"Name","value":"postalCode"}},{"kind":"Field","name":{"kind":"Name","value":"countryCode"}},{"kind":"Field","name":{"kind":"Name","value":"isResidential"}},{"kind":"Field","name":{"kind":"Name","value":"contactName"}},{"kind":"Field","name":{"kind":"Name","value":"companyName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"changeHistory"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"action"}},{"kind":"Field","name":{"kind":"Name","value":"fieldName"}},{"kind":"Field","name":{"kind":"Name","value":"beforeValue"}},{"kind":"Field","name":{"kind":"Name","value":"afterValue"}},{"kind":"Field","name":{"kind":"Name","value":"changedAt"}},{"kind":"Field","name":{"kind":"Name","value":"changedBy"}}]}}]}}]}}]} as unknown as DocumentNode<UpdateParcelMutation, UpdateParcelMutationVariables>;
export const CancelParcelDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CancelParcel"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CancelParcelInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"cancelParcel"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"trackingNumber"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"shipperAddressId"}},{"kind":"Field","name":{"kind":"Name","value":"serviceType"}},{"kind":"Field","name":{"kind":"Name","value":"weight"}},{"kind":"Field","name":{"kind":"Name","value":"weightUnit"}},{"kind":"Field","name":{"kind":"Name","value":"length"}},{"kind":"Field","name":{"kind":"Name","value":"width"}},{"kind":"Field","name":{"kind":"Name","value":"height"}},{"kind":"Field","name":{"kind":"Name","value":"dimensionUnit"}},{"kind":"Field","name":{"kind":"Name","value":"declaredValue"}},{"kind":"Field","name":{"kind":"Name","value":"currency"}},{"kind":"Field","name":{"kind":"Name","value":"description"}},{"kind":"Field","name":{"kind":"Name","value":"parcelType"}},{"kind":"Field","name":{"kind":"Name","value":"cancellationReason"}},{"kind":"Field","name":{"kind":"Name","value":"estimatedDeliveryDate"}},{"kind":"Field","name":{"kind":"Name","value":"deliveryAttempts"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"canEdit"}},{"kind":"Field","name":{"kind":"Name","value":"canCancel"}},{"kind":"Field","name":{"kind":"Name","value":"lastModifiedAt"}},{"kind":"Field","name":{"kind":"Name","value":"recipientAddress"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"street1"}},{"kind":"Field","name":{"kind":"Name","value":"street2"}},{"kind":"Field","name":{"kind":"Name","value":"city"}},{"kind":"Field","name":{"kind":"Name","value":"state"}},{"kind":"Field","name":{"kind":"Name","value":"postalCode"}},{"kind":"Field","name":{"kind":"Name","value":"countryCode"}},{"kind":"Field","name":{"kind":"Name","value":"isResidential"}},{"kind":"Field","name":{"kind":"Name","value":"contactName"}},{"kind":"Field","name":{"kind":"Name","value":"companyName"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"email"}}]}},{"kind":"Field","name":{"kind":"Name","value":"changeHistory"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"action"}},{"kind":"Field","name":{"kind":"Name","value":"fieldName"}},{"kind":"Field","name":{"kind":"Name","value":"beforeValue"}},{"kind":"Field","name":{"kind":"Name","value":"afterValue"}},{"kind":"Field","name":{"kind":"Name","value":"changedAt"}},{"kind":"Field","name":{"kind":"Name","value":"changedBy"}}]}}]}}]}}]} as unknown as DocumentNode<CancelParcelMutation, CancelParcelMutationVariables>;
export const GetRoutesDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetRoutes"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"where"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"RouteFilterInput"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"order"}},"type":{"kind":"ListType","type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"RouteSortInput"}}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"routes"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"where"},"value":{"kind":"Variable","name":{"kind":"Name","value":"where"}}},{"kind":"Argument","name":{"kind":"Name","value":"order"},"value":{"kind":"Variable","name":{"kind":"Name","value":"order"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"vehicleId"}},{"kind":"Field","name":{"kind":"Name","value":"vehiclePlate"}},{"kind":"Field","name":{"kind":"Name","value":"driverId"}},{"kind":"Field","name":{"kind":"Name","value":"driverName"}},{"kind":"Field","name":{"kind":"Name","value":"startDate"}},{"kind":"Field","name":{"kind":"Name","value":"endDate"}},{"kind":"Field","name":{"kind":"Name","value":"startMileage"}},{"kind":"Field","name":{"kind":"Name","value":"endMileage"}},{"kind":"Field","name":{"kind":"Name","value":"totalMileage"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"parcelCount"}},{"kind":"Field","name":{"kind":"Name","value":"parcelsDelivered"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}}]}}]}}]} as unknown as DocumentNode<GetRoutesQuery, GetRoutesQueryVariables>;
export const CreateRouteDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateRoute"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateRouteInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createRoute"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"vehicleId"}},{"kind":"Field","name":{"kind":"Name","value":"vehiclePlate"}},{"kind":"Field","name":{"kind":"Name","value":"driverId"}},{"kind":"Field","name":{"kind":"Name","value":"driverName"}},{"kind":"Field","name":{"kind":"Name","value":"startDate"}},{"kind":"Field","name":{"kind":"Name","value":"endDate"}},{"kind":"Field","name":{"kind":"Name","value":"startMileage"}},{"kind":"Field","name":{"kind":"Name","value":"endMileage"}},{"kind":"Field","name":{"kind":"Name","value":"totalMileage"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"parcelCount"}},{"kind":"Field","name":{"kind":"Name","value":"parcelsDelivered"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}}]}}]}}]} as unknown as DocumentNode<CreateRouteMutation, CreateRouteMutationVariables>;
export const UserManagementLookupsDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"UserManagementLookups"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"userManagementLookups"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"roles"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"value"}},{"kind":"Field","name":{"kind":"Name","value":"label"}}]}},{"kind":"Field","name":{"kind":"Name","value":"depots"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}}]}},{"kind":"Field","name":{"kind":"Name","value":"zones"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"name"}}]}}]}}]}}]} as unknown as DocumentNode<UserManagementLookupsQuery, UserManagementLookupsQueryVariables>;
export const UsersDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Users"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"search"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"isActive"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"Boolean"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"depotId"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"zoneId"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"users"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"search"},"value":{"kind":"Variable","name":{"kind":"Name","value":"search"}}},{"kind":"Argument","name":{"kind":"Name","value":"isActive"},"value":{"kind":"Variable","name":{"kind":"Name","value":"isActive"}}},{"kind":"Argument","name":{"kind":"Name","value":"depotId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"depotId"}}},{"kind":"Argument","name":{"kind":"Name","value":"zoneId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"zoneId"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"firstName"}},{"kind":"Field","name":{"kind":"Name","value":"lastName"}},{"kind":"Field","name":{"kind":"Name","value":"fullName"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"role"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"isProtected"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<UsersQuery, UsersQueryVariables>;
export const CreateUserDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateUser"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateUserInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createUser"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"firstName"}},{"kind":"Field","name":{"kind":"Name","value":"lastName"}},{"kind":"Field","name":{"kind":"Name","value":"fullName"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"role"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"isProtected"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<CreateUserMutation, CreateUserMutationVariables>;
export const UpdateUserDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateUser"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateUserInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateUser"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"firstName"}},{"kind":"Field","name":{"kind":"Name","value":"lastName"}},{"kind":"Field","name":{"kind":"Name","value":"fullName"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"role"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"isProtected"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<UpdateUserMutation, UpdateUserMutationVariables>;
export const DeactivateUserDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeactivateUser"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"userId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deactivateUser"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"userId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"userId"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"firstName"}},{"kind":"Field","name":{"kind":"Name","value":"lastName"}},{"kind":"Field","name":{"kind":"Name","value":"fullName"}},{"kind":"Field","name":{"kind":"Name","value":"email"}},{"kind":"Field","name":{"kind":"Name","value":"phone"}},{"kind":"Field","name":{"kind":"Name","value":"role"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"isProtected"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"zoneId"}},{"kind":"Field","name":{"kind":"Name","value":"zoneName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<DeactivateUserMutation, DeactivateUserMutationVariables>;
export const SendPasswordResetEmailDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"SendPasswordResetEmail"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"userId"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"sendPasswordResetEmail"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"userId"},"value":{"kind":"Variable","name":{"kind":"Name","value":"userId"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"success"}},{"kind":"Field","name":{"kind":"Name","value":"message"}}]}}]}}]} as unknown as DocumentNode<SendPasswordResetEmailMutation, SendPasswordResetEmailMutationVariables>;
export const CompletePasswordResetDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CompletePasswordReset"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CompletePasswordResetInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"completePasswordReset"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"success"}},{"kind":"Field","name":{"kind":"Name","value":"message"}}]}}]}}]} as unknown as DocumentNode<CompletePasswordResetMutation, CompletePasswordResetMutationVariables>;
export const RequestPasswordResetDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"RequestPasswordReset"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"email"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"String"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"requestPasswordReset"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"email"},"value":{"kind":"Variable","name":{"kind":"Name","value":"email"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"success"}},{"kind":"Field","name":{"kind":"Name","value":"message"}}]}}]}}]} as unknown as DocumentNode<RequestPasswordResetMutation, RequestPasswordResetMutationVariables>;
export const GetVehiclesDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetVehicles"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"where"}},"type":{"kind":"NamedType","name":{"kind":"Name","value":"VehicleFilterInput"}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"order"}},"type":{"kind":"ListType","type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"VehicleSortInput"}}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"vehicles"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"where"},"value":{"kind":"Variable","name":{"kind":"Name","value":"where"}}},{"kind":"Argument","name":{"kind":"Name","value":"order"},"value":{"kind":"Variable","name":{"kind":"Name","value":"order"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"registrationPlate"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"parcelCapacity"}},{"kind":"Field","name":{"kind":"Name","value":"weightCapacity"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"totalRoutes"}},{"kind":"Field","name":{"kind":"Name","value":"routesCompleted"}},{"kind":"Field","name":{"kind":"Name","value":"totalMileage"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetVehiclesQuery, GetVehiclesQueryVariables>;
export const CreateVehicleDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateVehicle"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateVehicleInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createVehicle"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"registrationPlate"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"parcelCapacity"}},{"kind":"Field","name":{"kind":"Name","value":"weightCapacity"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"totalRoutes"}},{"kind":"Field","name":{"kind":"Name","value":"routesCompleted"}},{"kind":"Field","name":{"kind":"Name","value":"totalMileage"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<CreateVehicleMutation, CreateVehicleMutationVariables>;
export const UpdateVehicleDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateVehicle"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateVehicleInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateVehicle"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}},{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"registrationPlate"}},{"kind":"Field","name":{"kind":"Name","value":"type"}},{"kind":"Field","name":{"kind":"Name","value":"parcelCapacity"}},{"kind":"Field","name":{"kind":"Name","value":"weightCapacity"}},{"kind":"Field","name":{"kind":"Name","value":"status"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"totalRoutes"}},{"kind":"Field","name":{"kind":"Name","value":"routesCompleted"}},{"kind":"Field","name":{"kind":"Name","value":"totalMileage"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<UpdateVehicleMutation, UpdateVehicleMutationVariables>;
export const DeleteVehicleDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteVehicle"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteVehicle"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}]}]}}]} as unknown as DocumentNode<DeleteVehicleMutation, DeleteVehicleMutationVariables>;
export const GetZonesDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetZones"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"zones"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"boundary"}},{"kind":"Field","name":{"kind":"Name","value":"boundaryGeoJson"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetZonesQuery, GetZonesQueryVariables>;
export const GetZoneDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"GetZone"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"zone"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"boundary"}},{"kind":"Field","name":{"kind":"Name","value":"boundaryGeoJson"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<GetZoneQuery, GetZoneQueryVariables>;
export const CreateZoneDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"CreateZone"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"CreateZoneInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"createZone"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"boundary"}},{"kind":"Field","name":{"kind":"Name","value":"boundaryGeoJson"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<CreateZoneMutation, CreateZoneMutationVariables>;
export const UpdateZoneDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"UpdateZone"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}},{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"input"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UpdateZoneInput"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"updateZone"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}},{"kind":"Argument","name":{"kind":"Name","value":"input"},"value":{"kind":"Variable","name":{"kind":"Name","value":"input"}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"id"}},{"kind":"Field","name":{"kind":"Name","value":"name"}},{"kind":"Field","name":{"kind":"Name","value":"boundary"}},{"kind":"Field","name":{"kind":"Name","value":"boundaryGeoJson"}},{"kind":"Field","name":{"kind":"Name","value":"isActive"}},{"kind":"Field","name":{"kind":"Name","value":"depotId"}},{"kind":"Field","name":{"kind":"Name","value":"depotName"}},{"kind":"Field","name":{"kind":"Name","value":"createdAt"}},{"kind":"Field","name":{"kind":"Name","value":"updatedAt"}}]}}]}}]} as unknown as DocumentNode<UpdateZoneMutation, UpdateZoneMutationVariables>;
export const DeleteZoneDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"mutation","name":{"kind":"Name","value":"DeleteZone"},"variableDefinitions":[{"kind":"VariableDefinition","variable":{"kind":"Variable","name":{"kind":"Name","value":"id"}},"type":{"kind":"NonNullType","type":{"kind":"NamedType","name":{"kind":"Name","value":"UUID"}}}}],"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"deleteZone"},"arguments":[{"kind":"Argument","name":{"kind":"Name","value":"id"},"value":{"kind":"Variable","name":{"kind":"Name","value":"id"}}}]}]}}]} as unknown as DocumentNode<DeleteZoneMutation, DeleteZoneMutationVariables>;