"use client";

import Link from "next/link";
import { signOut } from "next-auth/react";
import { LogOut, Truck } from "lucide-react";

import { Button } from "@/components/ui/button";
import { dashboardHeaderEdgeGutterClass } from "@/lib/dashboard-layout";
import { cn } from "@/lib/utils";

interface DashboardHeaderProps {
  user: {
    email?: string | null;
    name?: string | null;
    roles?: string[];
  };
}

function userInitials(user: DashboardHeaderProps["user"]): string {
  const raw = (user.name ?? user.email ?? "?").trim();
  const parts = raw.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0]![0]!}${parts[1]![0]!}`.toUpperCase();
  }
  return raw.slice(0, 2).toUpperCase() || "?";
}

export function DashboardHeader({ user }: DashboardHeaderProps) {
  return (
    <div
      className={cn(
        "flex h-14 w-full items-center justify-between gap-3",
        dashboardHeaderEdgeGutterClass,
      )}
    >
      <div className="flex min-w-0 items-center gap-3 sm:gap-4">
        <Link
          href="/dashboard"
          className="group flex min-w-0 shrink-0 items-center gap-2.5 outline-none focus-visible:ring-2 focus-visible:ring-neutral-400 focus-visible:ring-offset-2 focus-visible:ring-offset-neutral-950"
        >
          <span className="flex size-9 items-center justify-center rounded-xl bg-white text-neutral-950 shadow-md transition-transform group-hover:scale-[1.02]">
            <Truck className="size-[18px]" strokeWidth={2.25} aria-hidden />
          </span>
          <span className="hidden min-[400px]:flex flex-col leading-none">
            <span className="font-mono text-[13px] font-semibold uppercase tracking-[0.12em] text-neutral-50">
              Last Mile
            </span>
            <span className="mt-0.5 text-[10px] font-medium uppercase tracking-widest text-neutral-500">
              TMS
            </span>
          </span>
        </Link>
      </div>

      <div className="flex min-w-0 shrink items-center gap-2 sm:gap-8">
        <div
          className="flex size-9 shrink-0 items-center justify-center rounded-full border border-neutral-700 bg-neutral-800/80 text-xs font-semibold text-neutral-300 sm:hidden"
          aria-hidden
        >
          {userInitials(user)}
        </div>
        <div className="hidden min-w-0 max-w-[min(100%,18rem)] flex-col text-right sm:flex">
          <p className="truncate text-sm font-medium leading-tight text-neutral-100">
            {user.name ?? user.email}
          </p>
          {user.roles && user.roles.length > 0 ? (
            <p className="truncate text-xs text-neutral-500">
              {user.roles.join(", ")}
            </p>
          ) : null}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => signOut({ callbackUrl: "/login" })}
          title="Sign out"
          className="shrink-0 border-neutral-600 bg-transparent text-neutral-100 hover:bg-neutral-800 hover:text-white"
        >
          <LogOut className="mr-2 size-3.5 sm:inline" aria-hidden />
          <span className="hidden sm:inline">Sign out</span>
        </Button>
      </div>
    </div>
  );
}
