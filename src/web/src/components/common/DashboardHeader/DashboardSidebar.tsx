"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { ChevronsLeft, ChevronsRight } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  getDashboardNavItems,
  isDashboardNavActive,
} from "@/lib/dashboard-nav";
import { cn } from "@/lib/utils";

const STORAGE_KEY = "lastmile.dashboard.sidebarExpanded";

export function DashboardSidebar({ roles }: { roles?: string[] }) {
  const pathname = usePathname();
  const [expanded, setExpanded] = useState(true);
  const visibleNavItems = getDashboardNavItems(roles);

  useEffect(() => {
    try {
      const v = localStorage.getItem(STORAGE_KEY);
      if (v === "0" || v === "1") {
        // eslint-disable-next-line react-hooks/set-state-in-effect -- restore sidebar width from localStorage after mount
        setExpanded(v === "1");
      }
    } catch {
      /* ignore */
    }
  }, []);

  const toggle = () => {
    setExpanded((prev) => {
      const next = !prev;
      try {
        localStorage.setItem(STORAGE_KEY, next ? "1" : "0");
      } catch {
        /* ignore */
      }
      return next;
    });
  };

  return (
    <aside
      className={cn(
        "flex shrink-0 flex-col border-r border-border bg-muted/35 transition-[width] duration-200 ease-out dark:bg-muted/25",
        expanded ? "w-56" : "w-17",
      )}
      aria-label="App navigation"
    >
      <div
        className={cn(
          "flex h-12 shrink-0 items-center border-b border-border",
          expanded ? "justify-end px-2" : "justify-center px-0",
        )}
      >
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={toggle}
          aria-expanded={expanded}
          aria-label={
            expanded
              ? "Show navigation icons only"
              : "Show full navigation panel"
          }
          title={
            expanded
              ? "Show navigation icons only"
              : "Show full navigation panel"
          }
        >
          {expanded ? (
            <ChevronsLeft className="size-4" aria-hidden />
          ) : (
            <ChevronsRight className="size-4" aria-hidden />
          )}
        </Button>
      </div>

      <nav className="flex flex-1 flex-col gap-1 p-2" aria-label="Sections">
        {visibleNavItems.map((item) => {
          const Icon = item.icon;
          const active = isDashboardNavActive(pathname, item.href);

          return (
            <Link
              key={item.href}
              href={item.href}
              title={expanded ? undefined : item.label}
              className={cn(
                "flex items-center gap-3 rounded-lg text-sm font-medium transition-colors",
                expanded ? "px-3 py-2.5" : "justify-center px-0 py-2.5",
                active
                  ? "bg-primary/15 text-primary"
                  : "text-muted-foreground hover:bg-muted hover:text-foreground",
              )}
            >
              <Icon
                className={cn(
                  "size-5 shrink-0",
                  active ? "text-primary" : "text-muted-foreground",
                )}
                aria-hidden
              />
              {expanded ? (
                <span className="truncate">{item.label}</span>
              ) : null}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
