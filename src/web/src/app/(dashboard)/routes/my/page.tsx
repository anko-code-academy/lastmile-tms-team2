import { redirect } from "next/navigation";
import DriverSchedulePage from "@/components/routes/driver-schedule-page";
import { auth } from "@/lib/auth";
import { canManageRoutes, isDriver } from "@/lib/routes/access";

export default async function DriverScheduleRoute() {
  const session = await auth();
  const roles = session?.user.roles;

  if (isDriver(roles) && !canManageRoutes(roles)) {
    return <DriverSchedulePage />;
  }

  if (canManageRoutes(roles)) {
    redirect("/routes");
  }

  redirect("/dashboard");
}
