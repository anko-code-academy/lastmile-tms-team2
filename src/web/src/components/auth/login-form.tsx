"use client";

import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { signIn } from "next-auth/react";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { appToast } from "@/lib/toast/app-toast";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { loginSchema, type LoginSchema } from "@/lib/validation/auth";

const authErrorMessages: Record<string, string> = {
  CredentialsSignin:
    "Invalid email or password, or the API could not be reached.",
  Configuration:
    "Sign-in is misconfigured. Check AUTH_SECRET and API_URL on the server.",
  AccessDenied: "You do not have permission to sign in.",
};

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") ?? "/dashboard";
  const urlAuthError = searchParams.get("error");
  const [isLoading, setIsLoading] = useState(false);

  // Auth.js redirects here with ?error=CredentialsSignin when credentials fail (e.g. server signIn flow)
  useEffect(() => {
    if (!urlAuthError) return;

    appToast.error(
      authErrorMessages[urlAuthError] ??
        "Sign in failed. If this persists, check the terminal for [auth] Login failed.",
      { id: "login-url-auth-error" },
    );

    const params = new URLSearchParams(searchParams.toString());
    params.delete("error");
    params.delete("code");
    const qs = params.toString();
    router.replace(`/login${qs ? `?${qs}` : ""}`, { scroll: false });
  }, [urlAuthError, router, searchParams]);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginSchema>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: "",
      password: "",
    },
  });

  async function onSubmit(data: LoginSchema) {
    setIsLoading(true);

    try {
      const result = await signIn("credentials", {
        email: data.email,
        password: data.password,
        redirect: false,
      });

      if (result?.error) {
        appToast.error("Invalid email or password");
        return;
      }

      appToast.success("Login successful");
      router.push(callbackUrl);
      router.refresh();
    } catch {
      appToast.error("An error occurred during login");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="email">Email</Label>
        <Input
          id="email"
          type="email"
          placeholder="admin@lastmile.com"
          autoComplete="email"
          disabled={isLoading}
          {...register("email")}
        />
        {errors.email && (
          <p className="text-sm text-destructive">{errors.email.message}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="password">Password</Label>
        <Input
          id="password"
          type="password"
          placeholder="Admin@12345"
          autoComplete="current-password"
          disabled={isLoading}
          {...register("password")}
        />
        {errors.password && (
          <p className="text-sm text-destructive">{errors.password.message}</p>
        )}
      </div>

      <div className="flex justify-end">
        <Link
          href="/forgot-password"
          className="text-sm font-medium text-primary underline-offset-4 hover:underline"
        >
          Forgot password?
        </Link>
      </div>

      <Button
        type="submit"
        className="w-full cursor-pointer"
        size="lg"
        disabled={isLoading}
      >
        {isLoading ? (
          <>
            <Loader2 className="mr-2 size-4 animate-spin" />
            Logging in...
          </>
        ) : (
          "Login"
        )}
      </Button>
    </form>
  );
}
