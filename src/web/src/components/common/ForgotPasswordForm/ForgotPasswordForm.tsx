"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { requestPasswordReset } from "@/services/user-management.service";
import {
  forgotPasswordSchema,
  type ForgotPasswordSchema,
} from "@/lib/validations";

export function ForgotPasswordForm() {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordSchema>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      email: "",
    },
  });

  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null);

  async function onSubmit(values: ForgotPasswordSchema) {
    try {
      const result = await requestPasswordReset({
        email: values.email.trim(),
      });

      setSubmittedEmail(values.email.trim());
      toast.success(result.message);
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Could not request a reset link"
      );
    }
  }

  if (submittedEmail) {
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          If an account exists for <span className="font-medium">{submittedEmail}</span>,
          a reset link has been sent.
        </p>
        <div className="flex gap-3">
          <Button
            type="button"
            variant="outline"
            onClick={() => setSubmittedEmail(null)}
          >
            Use another email
          </Button>
          <Link
            href="/login"
            className="inline-flex h-8 items-center justify-center rounded-lg bg-primary px-2.5 text-sm font-medium text-primary-foreground transition hover:bg-primary/80"
          >
            Back to Login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="email">Email</Label>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="admin@lastmile.com"
          disabled={isSubmitting}
          {...register("email")}
        />
        {errors.email && (
          <p className="text-sm text-destructive">{errors.email.message}</p>
        )}
      </div>

      <Button type="submit" className="w-full" disabled={isSubmitting}>
        {isSubmitting ? "Sending Reset Link..." : "Send Reset Link"}
      </Button>

      <div className="text-center text-sm text-muted-foreground">
        Remembered your password?{" "}
        <Link
          href="/login"
          className="font-medium text-primary underline-offset-4 hover:underline"
        >
          Back to Login
        </Link>
      </div>
    </form>
  );
}
