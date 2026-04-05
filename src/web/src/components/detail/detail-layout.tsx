import {
  createContext,
  Fragment,
  type ReactNode,
  useContext,
} from "react";
import Link from "next/link";
import { ChevronRight } from "lucide-react";

import { cn } from "@/lib/utils";

/** Same horizontal + vertical inset as `DetailFormPageShell` (forms + detail views). */
export const DETAIL_PAGE_CONTENT_PADDING =
  "px-3 py-10 sm:px-5 sm:py-12 lg:px-6 lg:py-16";

export type DetailSection = "neutral" | "vehicle" | "route" | "driver";

const DetailPageSectionContext = createContext<"vehicle" | "route" | "driver" | null>(
  null,
);

/** Wrap detail view content so `DetailField` dots match fleet vs dispatch. */
export function DetailPageSectionProvider({
  section,
  children,
}: {
  section: "vehicle" | "route" | "driver";
  children: ReactNode;
}) {
  return (
    <DetailPageSectionContext.Provider value={section}>
      {children}
    </DetailPageSectionContext.Provider>
  );
}

const detailShellBg: Record<
  DetailSection,
  { gradient: string; radial: string; radialDark: string }
> = {
  neutral: {
    gradient:
      "bg-linear-to-b from-muted/55 via-background via-40% to-muted/28 dark:from-muted/22 dark:via-background dark:via-40% dark:to-muted/38",
    radial:
      "before:bg-[radial-gradient(ellipse_100%_52%_at_50%_-8%,oklch(0.96_0.012_220/0.55),transparent_58%)]",
    radialDark:
      "before:dark:bg-[radial-gradient(ellipse_100%_48%_at_50%_-6%,oklch(0.26_0.03_230/0.5),transparent_55%)]",
  },
  vehicle: {
    gradient:
      "bg-linear-to-b from-teal-500/12 via-background via-40% to-muted/24 dark:from-teal-950/42 dark:via-background dark:via-40% dark:to-teal-950/18",
    radial:
      "before:bg-[radial-gradient(ellipse_100%_52%_at_50%_-8%,oklch(0.9_0.055_195/0.5),transparent_58%)]",
    radialDark:
      "before:dark:bg-[radial-gradient(ellipse_100%_48%_at_50%_-6%,oklch(0.21_0.055_195/0.52),transparent_55%)]",
  },
  route: {
    gradient:
      "bg-linear-to-b from-violet-500/11 via-background via-40% to-amber-950/10 dark:from-violet-950/38 dark:via-background dark:via-40% dark:to-amber-950/18",
    radial:
      "before:bg-[radial-gradient(ellipse_100%_52%_at_50%_-8%,oklch(0.92_0.05_290/0.48),transparent_58%)]",
    radialDark:
      "before:dark:bg-[radial-gradient(ellipse_100%_48%_at_50%_-6%,oklch(0.25_0.05_285/0.48),transparent_55%)]",
  },
  driver: {
    gradient:
      "bg-linear-to-b from-blue-500/11 via-background via-40% to-slate-950/10 dark:from-blue-950/38 dark:via-background dark:via-40% dark:to-slate-950/18",
    radial:
      "before:bg-[radial-gradient(ellipse_100%_52%_at_50%_-8%,oklch(0.92_0.04_250/0.48),transparent_58%)]",
    radialDark:
      "before:dark:bg-[radial-gradient(ellipse_100%_48%_at_50%_-6%,oklch(0.25_0.04_250/0.48),transparent_55%)]",
  },
};

export function DetailShell({
  children,
  className,
  variant = "neutral",
}: {
  children: ReactNode;
  className?: string;
  /** Fleet (teal) vs dispatch (violet/amber) vs generic gray. */
  variant?: DetailSection;
}) {
  const bg = detailShellBg[variant];
  return (
    <div
      className={cn(
        "relative min-h-[calc(100dvh-7.25rem)] overflow-hidden rounded-2xl",
        bg.gradient,
        "before:pointer-events-none before:absolute before:inset-0",
        bg.radial,
        bg.radialDark,
        className,
      )}
    >
      {children}
    </div>
  );
}

export function DetailContainer({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("w-full", className)}>
      {children}
    </div>
  );
}

export type BreadcrumbItem = { label: string; href?: string };

export function DetailBreadcrumb({
  items,
  className,
  variant = "neutral",
}: {
  items: BreadcrumbItem[];
  className?: string;
  variant?: DetailSection;
}) {
  return (
    <nav
      className={cn(
        "mb-6 flex flex-wrap items-center gap-x-1 gap-y-1 text-sm text-muted-foreground/95",
        variant === "vehicle" &&
          "rounded-l-lg border-l-2 border-teal-500/55 pl-3 sm:pl-4",
        variant === "route" &&
          "rounded-l-lg border-l-2 border-violet-500/50 pl-3 sm:pl-4",
        className,
      )}
      aria-label="Breadcrumb"
    >
      {items.map((item, i) => (
        <Fragment key={`${item.label}-${i}`}>
          {i > 0 && (
            <ChevronRight
              className="size-3.5 shrink-0 opacity-40"
              aria-hidden
            />
          )}
          {item.href ? (
            <Link
              href={item.href}
              className="rounded-sm transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {item.label}
            </Link>
          ) : (
            <span className="font-medium text-foreground">{item.label}</span>
          )}
        </Fragment>
      ))}
    </nav>
  );
}

const heroIconTile: Record<
  "vehicle" | "route" | "driver",
  { tile: string; eyebrowLine: string; blobA: string; blobC: string }
> = {
  vehicle: {
    tile:
      "bg-teal-500/14 text-teal-700 shadow-sm ring-teal-600/15 dark:bg-teal-400/12 dark:text-teal-300 dark:ring-teal-400/22",
    eyebrowLine: "from-teal-600/80 to-transparent dark:from-teal-400/70",
    blobA:
      "bg-[radial-gradient(circle,oklch(0.72_0.11_195/0.22),transparent_68%)]",
    blobC:
      "bg-[radial-gradient(circle,oklch(0.68_0.08_260/0.12),transparent_72%)]",
  },
  route: {
    tile:
      "bg-violet-500/14 text-violet-800 shadow-sm ring-violet-500/15 dark:bg-violet-400/12 dark:text-violet-200 dark:ring-violet-400/25",
    eyebrowLine: "from-violet-600/80 to-transparent dark:from-violet-400/70",
    blobA:
      "bg-[radial-gradient(circle,oklch(0.72_0.11_290/0.22),transparent_68%)]",
    blobC:
      "bg-[radial-gradient(circle,oklch(0.68_0.1_300/0.14),transparent_72%)]",
  },
  driver: {
    tile:
      "bg-blue-500/14 text-blue-800 shadow-sm ring-blue-500/15 dark:bg-blue-400/12 dark:text-blue-200 dark:ring-blue-400/25",
    eyebrowLine: "from-blue-600/80 to-transparent dark:from-blue-400/70",
    blobA:
      "bg-[radial-gradient(circle,oklch(0.72_0.11_250/0.22),transparent_68%)]",
    blobC:
      "bg-[radial-gradient(circle,oklch(0.68_0.1_280/0.14),transparent_72%)]",
  },
};

export function DetailHero({
  title,
  subtitle,
  badge,
  actions,
  icon,
  eyebrow,
  section = "vehicle",
  actionsLayout = "row",
}: {
  title: string;
  subtitle?: ReactNode;
  badge?: ReactNode;
  actions?: ReactNode;
  /** Decorative icon (e.g. Lucide) — subtle hover scale on wrapper */
  icon?: ReactNode;
  /** Short label above title, e.g. "Fleet" */
  eyebrow?: string;
  /** Visual accent: fleet (teal) vs dispatch (violet). */
  section?: "vehicle" | "route" | "driver";
  /** Stack primary actions in a column (e.g. parcel detail downloads). */
  actionsLayout?: "row" | "column";
}) {
  const hi = heroIconTile[section];
  return (
    <div
      className="detail-hero-animate relative mb-10 overflow-hidden rounded-2xl border border-border/50 bg-card shadow-[0_1px_0_0_oklch(0_0_0/0.04),0_14px_44px_-18px_oklch(0.35_0.04_250/0.14)] dark:border-white/10 dark:bg-card/85 dark:shadow-[0_1px_0_0_oklch(1_0_0/0.06),0_22px_50px_-26px_oklch(0_0_0/0.5)]"
    >
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.7] dark:opacity-45"
        aria-hidden
      >
        <div
          className={cn(
            "absolute -left-20 top-0 h-56 w-56 rounded-full",
            hi.blobA,
          )}
        />
        <div className="absolute -right-12 -top-16 h-64 w-64 rounded-full bg-[radial-gradient(circle,oklch(0.78_0.12_85/0.16),transparent_65%)]" />
        <div
          className={cn(
            "absolute bottom-0 right-1/3 h-36 w-80 rounded-full",
            hi.blobC,
          )}
        />
      </div>
      <div className="relative flex flex-col gap-6 px-6 py-7 sm:flex-row sm:items-start sm:justify-between sm:px-8 sm:py-8">
        <div className="flex min-w-0 flex-1 flex-col gap-4 sm:flex-row sm:items-start sm:gap-6">
          {icon ? (
            <div
              className={cn(
                "group/icon flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl ring-1 transition-transform duration-300 ease-out hover:scale-[1.06] [&_svg]:size-7",
                hi.tile,
              )}
              aria-hidden
            >
              <span className="transition-transform duration-500 ease-out group-hover/icon:rotate-6">
                {icon}
              </span>
            </div>
          ) : null}
          <div className="min-w-0 space-y-3">
            {eyebrow ? (
              <div className="flex items-center gap-3">
                <span
                  className={cn(
                    "h-px w-8 shrink-0 bg-linear-to-r to-transparent",
                    hi.eyebrowLine,
                  )}
                  aria-hidden
                />
                <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-muted-foreground">
                  {eyebrow}
                </span>
              </div>
            ) : null}
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="font-mono text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
                {title}
              </h1>
              {badge}
            </div>
            {subtitle && (
              <p className="max-w-2xl text-sm leading-relaxed text-muted-foreground sm:text-[15px]">
                {subtitle}
              </p>
            )}
          </div>
        </div>
        {actions && (
          <div
            className={cn(
              "flex shrink-0 gap-2 sm:pt-1",
              actionsLayout === "column"
                ? "w-full flex-col items-stretch sm:ml-auto sm:w-auto sm:min-w-48 sm:items-end [&>a]:w-full [&>button]:w-full [&>a]:justify-center [&>button]:justify-center sm:[&>a]:w-auto sm:[&>button]:w-auto"
                : "flex-wrap items-center",
            )}
          >
            {actions}
          </div>
        )}
      </div>
    </div>
  );
}

const METRIC_ACCENTS = [
  {
    border: "border-teal-500/25",
    iconBg:
      "bg-teal-500/12 text-teal-700 ring-teal-600/15 dark:bg-teal-400/10 dark:text-teal-300 dark:ring-teal-400/20",
    glow: "from-teal-500/15",
  },
  {
    border: "border-emerald-500/25",
    iconBg:
      "bg-emerald-500/12 text-emerald-700 ring-emerald-600/15 dark:bg-emerald-400/10 dark:text-emerald-300 dark:ring-emerald-400/20",
    glow: "from-emerald-500/15",
  },
  {
    border: "border-amber-500/25",
    iconBg:
      "bg-amber-500/12 text-amber-800 ring-amber-600/15 dark:bg-amber-400/12 dark:text-amber-200 dark:ring-amber-400/25",
    glow: "from-amber-500/15",
  },
  {
    border: "border-sky-500/25",
    iconBg:
      "bg-sky-500/12 text-sky-800 ring-sky-600/15 dark:bg-sky-400/10 dark:text-sky-200 dark:ring-sky-400/22",
    glow: "from-sky-500/15",
  },
] as const;

const METRIC_ICON_MOTION = [
  "detail-metric-icon-motion",
  "detail-metric-icon-motion detail-metric-icon-motion-delay-1",
  "detail-metric-icon-motion detail-metric-icon-motion-delay-2",
  "detail-metric-icon-motion detail-metric-icon-motion-delay-3",
] as const;

export function DetailMetricStrip({
  items,
}: {
  items: {
    label: string;
    value: ReactNode;
    icon?: ReactNode;
    hint?: string;
  }[];
}) {
  return (
    <div className="detail-metrics-animate mt-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {items.map((item, i) => {
        const accent = METRIC_ACCENTS[i % METRIC_ACCENTS.length];
        const motionClass = METRIC_ICON_MOTION[i % METRIC_ICON_MOTION.length];
        return (
          <div
            key={item.label}
            className={cn(
              "group relative overflow-hidden rounded-2xl border border-border/55 bg-card/90 p-4 shadow-sm ring-1 ring-black/3 transition-[box-shadow,transform] duration-300 hover:-translate-y-0.5 hover:shadow-lg dark:bg-card/55 dark:ring-white/[0.07]",
              accent.border,
            )}
          >
            <div
              className={cn(
                "pointer-events-none absolute -right-8 -top-10 h-28 w-28 rounded-full bg-linear-to-br to-transparent opacity-80 dark:opacity-60",
                accent.glow,
              )}
              aria-hidden
            />
            <div className="relative flex items-start justify-between gap-3">
              <div className="min-w-0 space-y-1">
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {item.label}
                </p>
                <p className="text-2xl font-semibold tabular-nums tracking-tight text-foreground">
                  {item.value}
                </p>
                {item.hint && (
                  <p className="text-xs text-muted-foreground">{item.hint}</p>
                )}
              </div>
              {item.icon && (
                <div
                  className={cn(
                    "rounded-xl p-2.5 ring-1 transition-shadow duration-300 group-hover:shadow-md",
                    motionClass,
                    accent.iconBg,
                  )}
                >
                  {item.icon}
                </div>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}

const panelTitleAccent: Record<
  "neutral" | "vehicle" | "route" | "driver",
  string
> = {
  neutral:
    "bg-linear-to-r from-teal-500/90 to-teal-500/20 dark:from-teal-400/80 dark:to-teal-400/15",
  vehicle:
    "bg-linear-to-r from-teal-500/90 to-teal-500/20 dark:from-teal-400/80 dark:to-teal-400/15",
  route:
    "bg-linear-to-r from-violet-500/90 to-violet-500/20 dark:from-violet-400/80 dark:to-violet-400/15",
  driver:
    "bg-linear-to-r from-blue-500/90 to-blue-500/20 dark:from-blue-400/80 dark:to-blue-400/15",
};

export function DetailPanel({
  title,
  description,
  children,
  className,
  section = "neutral",
}: {
  title: string;
  description?: string;
  children: ReactNode;
  className?: string;
  section?: "neutral" | "vehicle" | "route" | "driver";
}) {
  return (
    <section className={cn("mt-10", className)}>
      <div className="mb-5 space-y-1.5">
        <div className="flex items-center gap-2">
          <span
            className={cn(
              "h-1 w-6 rounded-full",
              panelTitleAccent[section],
            )}
            aria-hidden
          />
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {title}
          </h2>
        </div>
        {description && (
          <p className="text-sm leading-relaxed text-muted-foreground">
            {description}
          </p>
        )}
      </div>
      <div className="relative rounded-2xl border border-border/50 bg-card/85 shadow-[0_1px_0_0_oklch(0_0_0/0.04),0_12px_36px_-18px_oklch(0.4_0.03_250/0.14)] ring-1 ring-black/4 dark:bg-card/50 dark:shadow-[0_1px_0_0_oklch(1_0_0/0.05),0_20px_44px_-24px_oklch(0_0_0/0.45)] dark:ring-white/[0.07]">
        <div
          className="pointer-events-none absolute inset-0 overflow-hidden rounded-[inherit]"
          aria-hidden
        >
          <div className="absolute -right-16 -top-20 h-52 w-52 rounded-full bg-[radial-gradient(circle,oklch(0.72_0.1_195/0.14),transparent_68%)] dark:opacity-75" />
          <div className="absolute -bottom-24 left-0 h-40 w-64 rounded-full bg-[radial-gradient(circle,oklch(0.78_0.08_85/0.08),transparent_70%)] dark:opacity-60" />
        </div>
        <div className="relative overflow-visible p-5 sm:p-6">{children}</div>
      </div>
    </section>
  );
}

export function DetailFieldGrid({
  children,
  columns = 2,
}: {
  children: ReactNode;
  columns?: 1 | 2;
}) {
  return (
    <dl
      className={cn(
        "grid gap-x-10 gap-y-6",
        columns === 2 && "sm:grid-cols-2"
      )}
    >
      {children}
    </dl>
  );
}

export function DetailField({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  const pageSection = useContext(DetailPageSectionContext);
  const dot =
    pageSection === "route"
      ? "bg-violet-600/85 shadow-[0_0_0_2px_oklch(0.72_0.12_290/0.22)] dark:bg-violet-400/85 dark:shadow-[0_0_0_2px_oklch(0.35_0.1_290/0.35)]"
      : pageSection === "driver"
        ? "bg-blue-600/85 shadow-[0_0_0_2px_oklch(0.72_0.12_250/0.22)] dark:bg-blue-400/85 dark:shadow-[0_0_0_2px_oklch(0.35_0.1_250/0.35)]"
        : "bg-teal-600/85 shadow-[0_0_0_2px_oklch(0.72_0.1_195/0.2)] dark:bg-teal-400/80 dark:shadow-[0_0_0_2px_oklch(0.35_0.08_195/0.35)]";
  return (
    <div className="min-w-0">
      <dt className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
        <span
          className={cn("h-1.5 w-1.5 shrink-0 rounded-full", dot)}
          aria-hidden
        />
        {label}
      </dt>
      <dd className="mt-1.5 text-sm font-medium leading-snug text-foreground">
        {children}
      </dd>
    </div>
  );
}

export function DetailPageSkeleton({
  variant = "neutral",
}: {
  variant?: DetailSection;
}) {
  return (
    <DetailShell variant={variant}>
      <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
        <div className="h-4 w-40 animate-pulse rounded-md bg-muted" />
        <div className="mt-6 h-10 w-56 animate-pulse rounded-md bg-muted" />
        <div className="mt-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {[1, 2, 3, 4].map((i) => (
            <div
              key={i}
              className="h-28 animate-pulse rounded-2xl bg-muted/80"
            />
          ))}
        </div>
        <div className="mt-10 h-64 animate-pulse rounded-2xl bg-muted/60" />
      </DetailContainer>
    </DetailShell>
  );
}

export function DetailEmptyState({
  title,
  message,
}: {
  title: string;
  message: string;
}) {
  return (
    <div className="relative overflow-hidden rounded-2xl border border-dashed border-teal-500/25 bg-linear-to-br from-teal-500/6 via-muted/30 to-amber-500/5 px-6 py-14 text-center shadow-inner dark:border-teal-400/20 dark:from-teal-950/40 dark:via-muted/20 dark:to-amber-950/30">
      <div
        className="pointer-events-none absolute -right-16 -top-20 h-40 w-40 rounded-full bg-[radial-gradient(circle,oklch(0.72_0.1_195/0.15),transparent_70%)]"
        aria-hidden
      />
      <div className="relative">
        <p className="font-semibold text-foreground">{title}</p>
        <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
          {message}
        </p>
      </div>
    </div>
  );
}
