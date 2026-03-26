"use client";

import { FilterListbox, type FilterListboxOption } from "@/components/ui/filter-listbox";
import type { DepotOption } from "@/types/depots";

interface DepotFilterDropdownProps {
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  depots: DepotOption[] | undefined;
}

export function DepotFilterDropdown({
  value,
  onChange,
  depots,
}: DepotFilterDropdownProps) {
  const options: FilterListboxOption<string>[] = [
    { value: undefined, label: "All Depots" },
    ...(depots?.map((d) => ({ value: d.id, label: d.name })) ?? []),
  ];

  return (
    <FilterListbox<string>
      value={value}
      onChange={onChange}
      options={options}
    />
  );
}
