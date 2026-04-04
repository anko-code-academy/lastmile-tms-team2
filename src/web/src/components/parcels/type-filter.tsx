"use client";

import { useMemo } from "react";

import { FilterListbox, type FilterListboxOption } from "@/components/form/filter-listbox";

interface TypeFilterProps {
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  parcelTypes: string[];
}

export function ParcelTypeFilter({ value, onChange, parcelTypes }: TypeFilterProps) {
  const typeOptions = useMemo<FilterListboxOption<string | undefined>[]>(() => {
    const unique = Array.from(new Set(parcelTypes.filter(Boolean))).sort();
    return [
      { value: undefined, label: "All types" },
      ...unique.map((t) => ({ value: t, label: t })),
    ];
  }, [parcelTypes]);

  return (
    <FilterListbox<string | undefined>
      value={value}
      onChange={onChange}
      options={typeOptions}
    />
  );
}
