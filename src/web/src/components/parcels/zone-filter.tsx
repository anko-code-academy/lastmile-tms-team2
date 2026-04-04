"use client";

import { FilterListbox, type FilterListboxOption } from "@/components/form/filter-listbox";

const ALL: FilterListboxOption<string | undefined> = {
  value: undefined,
  label: "All zones",
};

interface ZoneFilterProps {
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  zones: { id: string; name: string }[] | undefined;
  isLoading?: boolean;
}

export function ParcelZoneFilter({ value, onChange, zones, isLoading }: ZoneFilterProps) {
  const zoneOptions: FilterListboxOption<string | undefined>[] = [
    ALL,
    ...(zones?.map((z) => ({ value: z.id, label: z.name })) ?? []),
  ];

  if (isLoading) {
    return (
      <div className="flex h-10 min-w-[180px] items-center rounded-xl border border-input/90 bg-background px-4 py-2 text-sm text-muted-foreground shadow-sm">
        Loading zones...
      </div>
    );
  }

  return (
    <FilterListbox<string | undefined>
      value={value}
      onChange={onChange}
      options={zoneOptions}
    />
  );
}
