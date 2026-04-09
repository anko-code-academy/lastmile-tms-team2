import type { SelectOption } from "@/types/forms";
import type { Vehicle } from "@/types/vehicles";

/** Vehicle picker on route creation (shows capacity in the label). */
export function vehicleSelectOptionsForRoute(
  vehicles:
    | Array<
        Pick<
          Vehicle,
          "id" | "registrationPlate" | "parcelCapacity" | "weightCapacity"
        >
      >
    | undefined,
): SelectOption<string>[] {
  if (!vehicles?.length) return [];
  return vehicles.map((v) => ({
    value: v.id,
    label: `${v.registrationPlate} (${v.parcelCapacity} parcels, ${v.weightCapacity} kg)`,
  }));
}
