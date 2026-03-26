import { auth } from "@/lib/auth";
import {
  DashboardHeader,
  DashboardSidebar,
} from "@/components/common/DashboardHeader";
import {
  dashboardContentMaxClass,
  dashboardGutterXClass,
  dashboardPageVerticalClass,
} from "@/lib/dashboard-layout";
import { redirect } from "next/navigation";
import { Tooltip } from "radix-ui";
import { cn } from "@/lib/utils";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session) {
    redirect("/login");
  }

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <header className="sticky top-0 z-50 border-b border-neutral-800/80 bg-neutral-950 shadow-[inset_0_-1px_0_0_rgba(255,255,255,0.04)] dark:border-neutral-800 dark:bg-neutral-950">
        <DashboardHeader user={session.user} />
      </header>
      <Tooltip.Provider delayDuration={250} skipDelayDuration={200}>
        <div className="flex min-h-0 flex-1">
          <DashboardSidebar roles={session.user.roles} />
          <main
            className={cn(
              "min-h-0 min-w-0 flex-1 overflow-y-auto",
              "bg-[radial-gradient(ellipse_100%_55%_at_50%_-15%,oklch(0.94_0.025_210/0.5),transparent_58%)]",
              "dark:bg-[radial-gradient(ellipse_100%_50%_at_50%_-12%,oklch(0.22_0.04_220/0.55),transparent_55%)]",
              dashboardContentMaxClass,
              dashboardGutterXClass,
              dashboardPageVerticalClass,
            )}
          >
            {children}
          </main>
        </div>
      </Tooltip.Provider>
    </div>
  );
}
