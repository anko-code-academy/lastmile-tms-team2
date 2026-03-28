import { Suspense } from "react";
import type { Metadata } from "next";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { LoginForm } from "@/components/auth";

export const metadata: Metadata = {
  title: "Login - Last Mile TMS",
  description: "Login to Last Mile TMS delivery management system",
};

export default function LoginPage() {
  return (
    <Card className="border-border/50 shadow-lg">
      <CardHeader className="space-y-1 text-center">
        <CardTitle className="text-xl">Login to System</CardTitle>
        <CardDescription>
          Enter your credentials to login
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Suspense>
          <LoginForm />
        </Suspense>
      </CardContent>
    </Card>
  );
}
