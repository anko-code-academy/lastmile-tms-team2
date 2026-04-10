import type {
  CreateDepotRequest,
  Depot,
  DepotOperatingHours,
  UpdateDepotRequest,
} from "@/types/depots";
import { timeSpanScalarToHms } from "@/lib/time/graphql-timespan";

const DAY_OF_WEEK_ENUMS = [
  "SUNDAY",
  "MONDAY",
  "TUESDAY",
  "WEDNESDAY",
  "THURSDAY",
  "FRIDAY",
  "SATURDAY",
] as const;

type DayOfWeekEnum = (typeof DAY_OF_WEEK_ENUMS)[number];
type GraphQLDepotOperatingHours = Omit<DepotOperatingHours, "dayOfWeek"> & {
  dayOfWeek: string | number;
};
type GraphQLDepot = Omit<Depot, "operatingHours"> & {
  operatingHours: GraphQLDepotOperatingHours[] | null;
};

function normalizeOperatingHoursTime(value: string | null | undefined): string | null {
  if (value == null) {
    return null;
  }

  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }

  if (trimmed.startsWith("P") || trimmed.startsWith("-P")) {
    return timeSpanScalarToHms(trimmed);
  }

  return trimmed;
}

function isDayOfWeekIndex(value: number): boolean {
  return Number.isInteger(value) && value >= 0 && value < DAY_OF_WEEK_ENUMS.length;
}

export function dayOfWeekToIndex(value: string | number): number {
  if (typeof value === "number") {
    if (isDayOfWeekIndex(value)) {
      return value;
    }

    throw new Error(`Unsupported dayOfWeek index: ${value}`);
  }

  const normalized = value.trim().toUpperCase();
  const index = DAY_OF_WEEK_ENUMS.indexOf(normalized as DayOfWeekEnum);

  if (index >= 0) {
    return index;
  }

  const parsed = Number.parseInt(value, 10);
  if (Number.isFinite(parsed) && isDayOfWeekIndex(parsed)) {
    return parsed;
  }

  throw new Error(`Unsupported dayOfWeek value: ${value}`);
}

export function dayOfWeekFromIndex(index: number): DayOfWeekEnum {
  const value = DAY_OF_WEEK_ENUMS[index];

  if (!value) {
    throw new Error(`Unsupported dayOfWeek index: ${index}`);
  }

  return value;
}

export function normalizeDepot(raw: GraphQLDepot): Depot {
  return {
    ...raw,
    address: raw.address
      ? {
          ...raw.address,
          geoLocation: raw.address.geoLocation ?? null,
        }
      : null,
    operatingHours: raw.operatingHours?.map((item) => ({
      ...item,
      dayOfWeek: dayOfWeekToIndex(item.dayOfWeek),
      openTime: normalizeOperatingHoursTime(item.openTime),
      closedTime: normalizeOperatingHoursTime(item.closedTime),
    })) ?? null,
  };
}

export function serializeDepotOperatingHours(
  operatingHours?: CreateDepotRequest["operatingHours"] | UpdateDepotRequest["operatingHours"],
) {
  return operatingHours?.map((item) => ({
    ...item,
    dayOfWeek: dayOfWeekFromIndex(item.dayOfWeek),
    openTime: normalizeOperatingHoursTime(item.openTime) ?? item.openTime,
    closedTime: normalizeOperatingHoursTime(item.closedTime) ?? item.closedTime,
  }));
}
