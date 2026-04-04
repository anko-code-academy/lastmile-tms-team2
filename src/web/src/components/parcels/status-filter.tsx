"use client";

import { FilterListbox, type FilterListboxOption } from "@/components/form/filter-listbox";
import type { ParcelStatus } from "@/graphql/generated";

const ALL: FilterListboxOption<ParcelStatus | undefined> = {
  value: undefined,
  label: "All statuses",
};

const STATUSES: ParcelStatus[] = [
  "REGISTERED",
  "RECEIVED_AT_DEPOT",
  "SORTED",
  "STAGED",
  "LOADED",
  "OUT_FOR_DELIVERY",
  "DELIVERED",
  "EXCEPTION",
];

const STATUS_LABELS: Partial<Record<ParcelStatus, string>> = {
  REGISTERED: "Registered",
  RECEIVED_AT_DEPOT: "Received at Depot",
  SORTED: "Sorted",
  STAGED: "Staged",
  LOADED: "Loaded",
  OUT_FOR_DELIVERY: "Out for Delivery",
  DELIVERED: "Delivered",
  EXCEPTION: "Exception",
};

const STATUS_OPTIONS: FilterListboxOption<ParcelStatus>[] = STATUSES.map(
  (s) => ({ value: s, label: STATUS_LABELS[s]! }),
);

interface StatusFilterProps {
  value: ParcelStatus | undefined;
  onChange: (value: ParcelStatus | undefined) => void;
}

export function ParcelStatusFilter({ value, onChange }: StatusFilterProps) {
  return (
    <FilterListbox<ParcelStatus | undefined>
      value={value}
      onChange={onChange}
      options={[ALL, ...STATUS_OPTIONS]}
    />
  );
}
