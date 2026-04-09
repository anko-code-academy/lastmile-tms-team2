import type { SelectOption } from "@/types/forms";
import type { DriverOption } from "@/types/drivers";

export function driverSelectOptions(
  drivers: Array<DriverOption | { id: string; displayName: string }>,
): SelectOption<string>[] {
  return drivers.map((d) => ({ value: d.id, label: d.displayName }));
}
