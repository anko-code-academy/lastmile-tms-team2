import { redirect } from "next/navigation";
import RoutesPage from "@/components/routes/routes-page";
import { auth } from "@/lib/auth";
import { canManageRoutes, isDriver } from "@/lib/routes/access";

export default async function Page() {
  const session = await auth();
  const roles = session?.user.roles;

  if (isDriver(roles) && !canManageRoutes(roles)) {
    redirect("/routes/my");
  }

  if (!canManageRoutes(roles)) {
    redirect("/dashboard");
  }

  return <RoutesPage />;
}
