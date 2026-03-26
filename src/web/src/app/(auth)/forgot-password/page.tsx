import type { Metadata } from "next";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { ForgotPasswordForm } from "@/components/common/ForgotPasswordForm";

export const metadata: Metadata = {
  title: "Forgot Password - Last Mile TMS",
  description: "Request a password reset link for your Last Mile TMS account.",
};

export default function ForgotPasswordPage() {
  return (
    <Card className="border-border/50 shadow-lg">
      <CardHeader className="space-y-1 text-center">
        <CardTitle className="text-xl">Forgot Your Password?</CardTitle>
        <CardDescription>
          Enter your email and we will send you a link to reset your password.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ForgotPasswordForm />
      </CardContent>
    </Card>
  );
}
