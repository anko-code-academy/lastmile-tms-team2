import { redirect } from "next/navigation";
import RouteDetailPage from "@/components/routes/route-detail-page";
import { auth } from "@/lib/auth";
import { canManageRoutes, isDriver } from "@/lib/routes/access";

export default async function Page(props: { params: Promise<{ id: string }> }) {
  const session = await auth();
  const roles = session?.user.roles;

  if (canManageRoutes(roles)) {
    return <RouteDetailPage {...props} />;
  }

  if (isDriver(roles)) {
    return <RouteDetailPage {...props} mode="driver" />;
  }

  redirect("/dashboard");
}
