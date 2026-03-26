import { Suspense } from "react";
import type { Metadata } from "next";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { ResetPasswordForm } from "@/components/common/ResetPasswordForm";

export const metadata: Metadata = {
  title: "Reset Password - Last Mile TMS",
  description: "Set a new password for your Last Mile TMS account.",
};

export default function ResetPasswordPage() {
  return (
    <Card className="border-border/50 shadow-lg">
      <CardHeader className="space-y-1 text-center">
        <CardTitle className="text-xl">Set Your Password</CardTitle>
        <CardDescription>
          Choose a new password to access your account.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Suspense>
          <ResetPasswordForm />
        </Suspense>
      </CardContent>
    </Card>
  );
}
