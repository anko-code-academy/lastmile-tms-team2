import {
  DRIVERS_LIST,
  DRIVER_DETAIL,
  CREATE_DRIVER,
  UPDATE_DRIVER,
  DELETE_DRIVER,
} from "@/graphql/drivers";
import type {
  GetDriversQuery,
  GetDriverQuery,
  CreateDriverMutation,
  UpdateDriverMutation,
} from "@/graphql/drivers";
import type { DriverFilterInput } from "@/graphql/generated";
import { graphqlRequest } from "@/lib/network/graphql-client";
import { toGraphQLDateTimeFromDateInput } from "@/lib/datetime/graphql-datetime";
import {
  timeSpanScalarToHms,
  toGraphQLTimeSpanFromHms,
} from "@/lib/time/graphql-timespan";
import { sortByDayOfWeek } from "@/lib/labels/drivers";
import type {
  Driver,
  CreateDriverRequest,
  UpdateDriverRequest,
  CreateDriverAvailabilityRequest,
  UpdateDriverAvailabilityRequest,
} from "@/types/drivers";

function mapCreateAvailabilityForGraphQL(
  rows: CreateDriverAvailabilityRequest[],
) {
  return rows.map((a) => ({
    dayOfWeek: a.dayOfWeek,
    isAvailable: a.isAvailable,
    shiftStart:
      a.isAvailable && a.shiftStart?.trim()
        ? toGraphQLTimeSpanFromHms(a.shiftStart)
        : null,
    shiftEnd:
      a.isAvailable && a.shiftEnd?.trim()
        ? toGraphQLTimeSpanFromHms(a.shiftEnd)
        : null,
  }));
}

function mapUpdateAvailabilityForGraphQL(
  rows: UpdateDriverAvailabilityRequest[],
) {
  return rows.map((a) => ({
    id: a.id,
    dayOfWeek: a.dayOfWeek,
    isAvailable: a.isAvailable,
    shiftStart:
      a.isAvailable && a.shiftStart?.trim()
        ? toGraphQLTimeSpanFromHms(a.shiftStart)
        : null,
    shiftEnd:
      a.isAvailable && a.shiftEnd?.trim()
        ? toGraphQLTimeSpanFromHms(a.shiftEnd)
        : null,
  }));
}

export const driversService = {
  getAll: async (where?: DriverFilterInput): Promise<Driver[]> => {
    const variables: Record<string, unknown> = {};
    if (where !== undefined) {
      variables.where = where;
    }

    const data = await graphqlRequest<GetDriversQuery>(
      DRIVERS_LIST,
      Object.keys(variables).length ? variables : undefined
    );
    return data.drivers.map((d) => ({
      id: d.id,
      displayName: d.displayName,
      firstName: d.firstName,
      lastName: d.lastName,
      phone: d.phone,
      email: d.email,
      licenseNumber: d.licenseNumber,
      licenseExpiryDate: d.licenseExpiryDate,
      photoUrl: d.photoUrl,
      zoneId: d.zoneId,
      depotId: d.depotId,
      status: d.status,
      userId: d.userId,
      zoneName: d.zoneName ?? null,
      depotName: d.depotName ?? null,
      userName: d.userName ?? null,
      availabilitySchedule: [],
      createdAt: d.createdAt,
      updatedAt: d.updatedAt ?? null,
    }));
  },

  getById: async (id: string): Promise<Driver> => {
    const data = await graphqlRequest<GetDriverQuery>(DRIVER_DETAIL, { id });
    if (!data.driver) throw new Error("Driver not found");
    const d = data.driver;
    return {
      id: d.id,
      displayName: d.displayName,
      firstName: d.firstName,
      lastName: d.lastName,
      phone: d.phone,
      email: d.email,
      licenseNumber: d.licenseNumber,
      licenseExpiryDate: d.licenseExpiryDate,
      photoUrl: d.photoUrl,
      zoneId: d.zoneId,
      depotId: d.depotId,
      status: d.status,
      userId: d.userId,
      zoneName: d.zoneName ?? null,
      depotName: d.depotName ?? null,
      userName: d.userName ?? null,
      availabilitySchedule: sortByDayOfWeek(
        (d.availabilitySchedule ?? []).map((a) => ({
          id: a.id,
          dayOfWeek: a.dayOfWeek,
          shiftStart: timeSpanScalarToHms(a.shiftStart),
          shiftEnd: timeSpanScalarToHms(a.shiftEnd),
          isAvailable: a.isAvailable,
        })),
      ),
      createdAt: d.createdAt,
      updatedAt: d.updatedAt ?? null,
    };
  },

  create: async (data: CreateDriverRequest): Promise<Driver> => {
    const res = await graphqlRequest<CreateDriverMutation>(CREATE_DRIVER, {
      input: {
        firstName: data.firstName,
        lastName: data.lastName,
        phone: data.phone,
        email: data.email,
        licenseNumber: data.licenseNumber,
        licenseExpiryDate: toGraphQLDateTimeFromDateInput(
          data.licenseExpiryDate ?? null,
        ),
        photoUrl: data.photoUrl,
        zoneId: data.zoneId,
        depotId: data.depotId,
        status: data.status,
        userId: data.userId,
        availabilitySchedule: mapCreateAvailabilityForGraphQL(
          data.availabilitySchedule,
        ),
      },
    });
    const d = res.createDriver;
    return {
      id: d.id,
      displayName: d.displayName,
      firstName: d.firstName,
      lastName: d.lastName,
      phone: d.phone,
      email: d.email,
      licenseNumber: d.licenseNumber,
      licenseExpiryDate: d.licenseExpiryDate,
      photoUrl: d.photoUrl,
      zoneId: d.zoneId,
      depotId: d.depotId,
      status: d.status,
      userId: d.userId,
      zoneName: d.zoneName ?? null,
      depotName: d.depotName ?? null,
      userName: d.userName ?? null,
      availabilitySchedule: sortByDayOfWeek(
        (d.availabilitySchedule ?? []).map((a) => ({
          id: a.id,
          dayOfWeek: a.dayOfWeek,
          shiftStart: timeSpanScalarToHms(a.shiftStart),
          shiftEnd: timeSpanScalarToHms(a.shiftEnd),
          isAvailable: a.isAvailable,
        })),
      ),
      createdAt: d.createdAt,
      updatedAt: d.updatedAt ?? null,
    };
  },

  update: async (id: string, data: UpdateDriverRequest): Promise<Driver> => {
    const res = await graphqlRequest<UpdateDriverMutation>(UPDATE_DRIVER, {
      id,
      input: {
        firstName: data.firstName,
        lastName: data.lastName,
        phone: data.phone,
        email: data.email,
        licenseNumber: data.licenseNumber,
        licenseExpiryDate: toGraphQLDateTimeFromDateInput(
          data.licenseExpiryDate ?? null,
        ),
        photoUrl: data.photoUrl,
        zoneId: data.zoneId,
        depotId: data.depotId,
        status: data.status,
        userId: data.userId,
        availabilitySchedule: mapUpdateAvailabilityForGraphQL(
          data.availabilitySchedule,
        ),
      },
    });
    if (!res.updateDriver) throw new Error("Driver not found");
    const d = res.updateDriver;
    return {
      id: d.id,
      displayName: d.displayName,
      firstName: d.firstName,
      lastName: d.lastName,
      phone: d.phone,
      email: d.email,
      licenseNumber: d.licenseNumber,
      licenseExpiryDate: d.licenseExpiryDate,
      photoUrl: d.photoUrl,
      zoneId: d.zoneId,
      depotId: d.depotId,
      status: d.status,
      userId: d.userId,
      zoneName: d.zoneName ?? null,
      depotName: d.depotName ?? null,
      userName: d.userName ?? null,
      availabilitySchedule: sortByDayOfWeek(
        (d.availabilitySchedule ?? []).map((a) => ({
          id: a.id,
          dayOfWeek: a.dayOfWeek,
          shiftStart: timeSpanScalarToHms(a.shiftStart),
          shiftEnd: timeSpanScalarToHms(a.shiftEnd),
          isAvailable: a.isAvailable,
        })),
      ),
      createdAt: d.createdAt,
      updatedAt: d.updatedAt ?? null,
    };
  },

  delete: async (id: string): Promise<boolean> => {
    const res = await graphqlRequest<{ deleteDriver: boolean }>(
      DELETE_DRIVER,
      { id }
    );
    return res.deleteDriver;
  },
};
