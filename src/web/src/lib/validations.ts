import { z } from "zod";
import { USER_ROLES } from "@/types/user-management";

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Enter a valid email"),
  password: z
    .string()
    .min(1, "Password is required"),
});

export type LoginSchema = z.infer<typeof loginSchema>;

export const forgotPasswordSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Enter a valid email"),
});

export type ForgotPasswordSchema = z.infer<typeof forgotPasswordSchema>;

export const userFormSchema = z
  .object({
    firstName: z.string().min(1, "First name is required").max(100),
    lastName: z.string().min(1, "Last name is required").max(100),
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    phone: z.string().max(20, "Phone must be 20 characters or fewer").optional(),
    role: z.enum(USER_ROLES),
    depotId: z.string().optional(),
    zoneId: z.string().optional(),
    isActive: z.boolean(),
  })
  .refine((value) => !value.zoneId || !!value.depotId, {
    path: ["zoneId"],
    message: "A depot must be selected when a zone is assigned.",
  });

export type UserFormSchema = z.infer<typeof userFormSchema>;

export const resetPasswordSchema = z
  .object({
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    token: z.string().min(1, "Reset token is missing"),
    newPassword: z
      .string()
      .min(8, "Password must be at least 8 characters")
      .regex(/[A-Z]/, "Password must contain an uppercase letter")
      .regex(/[a-z]/, "Password must contain a lowercase letter")
      .regex(/[0-9]/, "Password must contain a digit"),
    confirmPassword: z.string().min(1, "Confirm your password"),
  })
  .refine((value) => value.newPassword === value.confirmPassword, {
    path: ["confirmPassword"],
    message: "Passwords do not match",
  });

export type ResetPasswordSchema = z.infer<typeof resetPasswordSchema>;
