"use client";

import { signOut } from "next-auth/react";
import { LogOut, Truck } from "lucide-react";
import { Button } from "@/components/ui/button";

interface DashboardHeaderProps {
  user: {
    email?: string | null;
    name?: string | null;
    roles?: string[];
  };
}

export function DashboardHeader({ user }: DashboardHeaderProps) {
  return (
    <header className="flex items-center justify-between border-b border-border bg-card px-6 py-3">
      <div className="flex items-center gap-3">
        <div className="flex size-8 items-center justify-center rounded-lg bg-primary">
          <Truck className="size-4 text-primary-foreground" />
        </div>
        <span className="text-lg font-semibold">Last Mile TMS</span>
      </div>

      <div className="flex items-center gap-4">
        <div className="text-right">
          <p className="text-sm font-medium">{user.name ?? user.email}</p>
          {user.roles && user.roles.length > 0 && (
            <p className="text-xs text-muted-foreground">
              {user.roles.join(", ")}
            </p>
          )}
        </div>
        <Button
          variant="ghost"
          size="icon"
          onClick={() => signOut({ callbackUrl: "/login" })}
          title="Logout"
          className="cursor-pointer"
        >
          <LogOut className="size-4" />
        </Button>
      </div>
    </header>
  );
}
