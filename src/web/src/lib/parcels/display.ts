import {
  ParcelDimensionUnit,
  ParcelServiceType,
  ParcelWeightUnit,
} from "@/types/parcels";

export function parcelWeightKg(weight: number, weightUnit: number): number {
  return weightUnit === ParcelWeightUnit.Lb ? weight * 0.453592 : weight;
}

export function formatParcelWeightUnitLabel(weightUnit: number): string {
  return weightUnit === ParcelWeightUnit.Lb ? "Lb" : "Kg";
}

/** Maps numeric local WeightUnit to GraphQL string enum */
export function toGraphQLWeightUnit(unit: number): "KG" | "LB" {
  return unit === ParcelWeightUnit.Lb ? "LB" : "KG";
}

/** Maps numeric local DimensionUnit to GraphQL string enum */
export function toGraphQLDimensionUnit(unit: string): "CM" | "IN" {
  return unit === ParcelDimensionUnit.Cm ? "CM" : "IN";
}

/** Maps numeric local ServiceType to GraphQL string enum */
export function toGraphQLServiceType(
  type: (typeof ParcelServiceType)[keyof typeof ParcelServiceType]
): "ECONOMY" | "STANDARD" | "EXPRESS" | "OVERNIGHT" {
  return type;
}
