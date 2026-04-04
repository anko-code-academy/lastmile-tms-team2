import { z } from "zod";

export const parcelFormSchema = z.object({
  shipperAddressId: z.string().min(1, "Select a depot."),
  recipientStreet1: z.string().trim().min(1, "Required."),
  recipientStreet2: z.string(),
  recipientCity: z.string().trim().min(1, "Required."),
  recipientState: z.string().trim().min(1, "Required."),
  recipientPostalCode: z.string().trim().min(1, "Required."),
  recipientCountryCode: z
    .string()
    .trim()
    .min(1, "Required.")
    .max(3, "Country code must be 2-3 characters."),
  recipientIsResidential: z.boolean(),
  recipientContactName: z.string().trim().min(1, "Required."),
  recipientCompanyName: z.string(),
  recipientPhone: z.string(),
  recipientEmail: z.string(),
  description: z.string(),
  parcelType: z.string(),
  serviceType: z.enum(["ECONOMY", "STANDARD", "EXPRESS", "OVERNIGHT"]),
  weight: z.number().gt(0, "Must be greater than 0."),
  weightUnit: z.number(),
  length: z.number().gt(0, "Must be greater than 0."),
  width: z.number().gt(0, "Must be greater than 0."),
  height: z.number().gt(0, "Must be greater than 0."),
  dimensionUnit: z.enum(["CM", "IN"]),
  declaredValue: z.number().gte(0, "Cannot be negative."),
  currency: z.string().trim().length(3, "Use a 3-letter currency."),
  estimatedDeliveryDate: z
    .string()
    .min(1, "Required.")
    .refine((value) => {
      const parsed = new Date(`${value}T00:00:00Z`);
      return !Number.isNaN(parsed.getTime()) && parsed > new Date();
    }, "Must be a future date."),
});
