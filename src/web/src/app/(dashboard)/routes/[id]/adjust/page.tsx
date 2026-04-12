import { redirect } from "next/navigation";
import RouteAdjustPage from "@/components/routes/route-adjust-page";
import { auth } from "@/lib/auth";
import { canManageRoutes } from "@/lib/routes/access";

export default async function Page() {
  const session = await auth();
  const roles = session?.user.roles;

  if (canManageRoutes(roles)) {
    return <RouteAdjustPage />;
  }

  redirect("/dashboard");
}
