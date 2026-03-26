import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { UserManagementClient } from "@/components/common/UserManagement";

export const metadata: Metadata = {
  title: "Users - Last Mile TMS",
  description: "Manage user accounts and access controls.",
};

export default async function UsersPage() {
  const session = await auth();

  if (!session?.user.roles.includes("Admin")) {
    redirect("/dashboard");
  }

  return <UserManagementClient accessToken={session.accessToken} />;
}
