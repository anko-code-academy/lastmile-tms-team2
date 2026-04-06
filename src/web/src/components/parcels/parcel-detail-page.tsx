"use client";

import { useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import {
  ArrowLeft,
  MapPin,
  Package,
  Pencil,
  Printer,
  UserRound,
} from "lucide-react";
import { useSession } from "next-auth/react";

import {
  DetailBreadcrumb,
  DetailContainer,
  DetailEmptyState,
  DetailField,
  DetailFieldGrid,
  DetailHero,
  DetailMetricStrip,
  DetailPageSkeleton,
  DetailPanel,
  DetailShell,
  DETAIL_PAGE_CONTENT_PADDING,
} from "@/components/detail";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { CancelParcelDialog } from "@/components/parcels/cancel-parcel-dialog";
import { buttonVariants } from "@/components/ui/button";
import {
  formatParcelServiceType,
  formatParcelStatus,
  parcelStatusBadgeClass,
} from "@/lib/labels/parcels";
import { getErrorMessage } from "@/lib/network/error-message";
import { appToast } from "@/lib/toast/app-toast";
import { cn } from "@/lib/utils";
import { useCancelParcel, useParcel } from "@/queries/parcels";
import { parcelsService } from "@/services/parcels.service";
import { ParcelStatusTransition } from "./parcel-status-transition";
import { ParcelTimeline } from "./parcel-timeline";

function formatAddressLineTwo(
  city: string,
  state: string,
  postalCode: string,
  countryCode: string,
) {
  return [city, [state, postalCode].filter(Boolean).join(" "), countryCode]
    .filter(Boolean)
    .join(", ");
}

function formatHistoryValue(value: string | null) {
  return value && value.trim() ? value : "-";
}

export default function ParcelDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { status: sessionStatus } = useSession();
  const { data: parcel, isLoading, error } = useParcel(id);
  const cancelParcel = useCancelParcel();
  const [isCancelDialogOpen, setIsCancelDialogOpen] = useState(false);

  async function handleDownload(format: "zpl" | "pdf") {
    if (!parcel) {
      return;
    }

    try {
      await parcelsService.downloadLabel(parcel.id, format);
    } catch (downloadError) {
      appToast.errorFromUnknown(downloadError);
    }
  }

  if (sessionStatus === "loading" || isLoading) {
    return <DetailPageSkeleton variant="neutral" />;
  }

  if (error) {
    return (
      <DetailShell variant="neutral">
        <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
          <QueryErrorAlert
            title="Could not load parcel"
            message={getErrorMessage(error)}
          />
        </DetailContainer>
      </DetailShell>
    );
  }

  if (!parcel) {
    return (
      <DetailShell variant="neutral">
        <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
          <DetailBreadcrumb
            items={[{ label: "Parcels", href: "/parcels" }, { label: "Not found" }]}
          />
          <DetailEmptyState
            title="Parcel not found"
            message="This parcel may have been removed or the link is incorrect."
          />
        </DetailContainer>
      </DetailShell>
    );
  }

  const recipientName =
    parcel.recipientAddress.contactName ??
    parcel.recipientAddress.companyName ??
    "Recipient";
  const changeHistory = [...parcel.changeHistory].sort(
    (left, right) =>
      new Date(right.changedAt).getTime() - new Date(left.changedAt).getTime(),
  );

  return (
    <DetailShell variant="neutral">
      <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
        <DetailBreadcrumb
          items={[
            { label: "Parcels", href: "/parcels" },
            { label: parcel.trackingNumber },
          ]}
        />

        <DetailHero
          title={parcel.trackingNumber}
          eyebrow="Warehouse"
          icon={<Package strokeWidth={1.75} />}
          subtitle={
            <>
              {recipientName}
              {" | "}
              {formatParcelServiceType(parcel.serviceType)}
              {parcel.zoneName ? (
                <>
                  {" | "}
                  <span className="text-foreground/80">{parcel.zoneName}</span>
                </>
              ) : null}
            </>
          }
          badge={
            <span className={parcelStatusBadgeClass(parcel.status)}>
              {formatParcelStatus(parcel.status)}
            </span>
          }
          actions={
            <div className="flex w-full flex-col gap-3 sm:ml-auto sm:w-auto sm:items-end">
              <div className="flex w-full flex-wrap items-center justify-end gap-2">
                {parcel.canEdit ? (
                  <Link
                    href={`/parcels/${parcel.id}/edit`}
                    className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                  >
                    <Pencil className="mr-2 size-4" aria-hidden />
                    Edit parcel
                  </Link>
                ) : null}
                {parcel.canCancel ? (
                  <button
                    type="button"
                    className={cn(
                      buttonVariants({ variant: "destructive", size: "sm" }),
                    )}
                    onClick={() => setIsCancelDialogOpen(true)}
                  >
                    Cancel parcel
                  </button>
                ) : null}
                <Link
                  href="/parcels"
                  className={cn(buttonVariants({ variant: "ghost", size: "sm" }))}
                >
                  <ArrowLeft className="mr-2 size-4" aria-hidden />
                  Back to queue
                </Link>
              </div>
              <div className="flex w-full flex-col gap-2 sm:w-auto sm:items-end">
                <button
                  type="button"
                  className={cn(
                    buttonVariants({ variant: "outline", size: "sm" }),
                    "w-full justify-center sm:w-auto",
                  )}
                  onClick={() => void handleDownload("zpl")}
                >
                  <Printer className="mr-2 size-4" aria-hidden />
                  Download 4x6 ZPL
                </button>
                <button
                  type="button"
                  className={cn(
                    buttonVariants({ variant: "outline", size: "sm" }),
                    "w-full justify-center sm:w-auto",
                  )}
                  onClick={() => void handleDownload("pdf")}
                >
                  Download A4 PDF
                </button>
              </div>
            </div>
          }
        />

        <DetailMetricStrip
          items={[
            {
              label: "Sort zone",
              value: parcel.zoneName ?? "-",
              icon: <MapPin className="size-5" aria-hidden />,
            },
            {
              label: "Parcel type",
              value: parcel.parcelType ?? "-",
              icon: <Package className="size-5" aria-hidden />,
            },
            {
              label: "Weight",
              value: `${parcel.weight} ${parcel.weightUnit}`,
              icon: <Package className="size-5" aria-hidden />,
            },
            {
              label: "Delivery attempts",
              value: parcel.deliveryAttempts.toString(),
              icon: <UserRound className="size-5" aria-hidden />,
            },
          ]}
        />

        <DetailPanel
          title="Parcel details"
          description="Tracking, parcel metadata, and current warehouse routing context."
        >
          <DetailFieldGrid>
            <DetailField label="Tracking number">{parcel.trackingNumber}</DetailField>
            <DetailField label="Status">
              <span className={parcelStatusBadgeClass(parcel.status)}>
                {formatParcelStatus(parcel.status)}
              </span>
            </DetailField>
            <DetailField label="Service type">
              {formatParcelServiceType(parcel.serviceType)}
            </DetailField>
            <DetailField label="Sort zone">{parcel.zoneName ?? "-"}</DetailField>
            <DetailField label="Depot">{parcel.depotName ?? "-"}</DetailField>
            <DetailField label="Parcel type">{parcel.parcelType ?? "-"}</DetailField>
            <DetailField label="Dimensions">
              {parcel.length} x {parcel.width} x {parcel.height} {parcel.dimensionUnit}
            </DetailField>
            <DetailField label="Declared value">
              {parcel.declaredValue} {parcel.currency}
            </DetailField>
            <DetailField label="Est. delivery date">
              {new Date(parcel.estimatedDeliveryDate).toLocaleDateString()}
            </DetailField>
            <DetailField label="Cancellation reason">
              {parcel.cancellationReason ?? "-"}
            </DetailField>
            <DetailField label="Created">
              {new Date(parcel.createdAt).toLocaleString()}
            </DetailField>
            <DetailField label="Last modified">
              {parcel.lastModifiedAt
                ? new Date(parcel.lastModifiedAt).toLocaleString()
                : "-"}
            </DetailField>
          </DetailFieldGrid>
        </DetailPanel>

        <DetailPanel
          title="Recipient"
          description="Address and contact details shown on the printed shipping label."
        >
          <DetailFieldGrid>
            <DetailField label="Name">
              {parcel.recipientAddress.contactName ?? "-"}
            </DetailField>
            <DetailField label="Company">
              {parcel.recipientAddress.companyName ?? "-"}
            </DetailField>
            <DetailField label="Address">
              <div className="space-y-1">
                <p>{parcel.recipientAddress.street1}</p>
                {parcel.recipientAddress.street2 ? (
                  <p>{parcel.recipientAddress.street2}</p>
                ) : null}
                <p>
                  {formatAddressLineTwo(
                    parcel.recipientAddress.city,
                    parcel.recipientAddress.state,
                    parcel.recipientAddress.postalCode,
                    parcel.recipientAddress.countryCode,
                  )}
                </p>
              </div>
            </DetailField>
            <DetailField label="Residential">
              {parcel.recipientAddress.isResidential ? "Yes" : "No"}
            </DetailField>
            <DetailField label="Phone">
              {parcel.recipientAddress.phone ?? "-"}
            </DetailField>
            <DetailField label="Email">
              {parcel.recipientAddress.email ?? "-"}
            </DetailField>
          </DetailFieldGrid>
        </DetailPanel>

        <DetailPanel
          title="Change history"
          description="Field-level edits and cancellation activity, newest changes first."
        >
          {changeHistory.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No changes have been recorded for this parcel yet.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm">
                <thead>
                  <tr className="border-b border-border/60 text-left text-xs uppercase tracking-[0.14em] text-muted-foreground">
                    <th className="px-3 py-3 font-semibold">When</th>
                    <th className="px-3 py-3 font-semibold">Who</th>
                    <th className="px-3 py-3 font-semibold">Action</th>
                    <th className="px-3 py-3 font-semibold">Field</th>
                    <th className="px-3 py-3 font-semibold">Before</th>
                    <th className="px-3 py-3 font-semibold">After</th>
                  </tr>
                </thead>
                <tbody>
                  {changeHistory.map((entry, index) => (
                    <tr
                      key={`${entry.changedAt}-${entry.fieldName}-${index}`}
                      className="border-b border-border/40 last:border-b-0"
                    >
                      <td className="px-3 py-3 align-top tabular-nums">
                        {new Date(entry.changedAt).toLocaleString()}
                      </td>
                      <td className="px-3 py-3 align-top">
                        {entry.changedBy ?? "System"}
                      </td>
                      <td className="px-3 py-3 align-top">{entry.action}</td>
                      <td className="px-3 py-3 align-top">{entry.fieldName}</td>
                      <td className="px-3 py-3 align-top text-muted-foreground">
                        {formatHistoryValue(entry.beforeValue)}
                      </td>
                      <td className="px-3 py-3 align-top">
                        {formatHistoryValue(entry.afterValue)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </DetailPanel>

        <ParcelStatusTransition parcel={parcel} />

        <ParcelTimeline parcelId={parcel.id} />
      </DetailContainer>

      <CancelParcelDialog
        open={isCancelDialogOpen}
        onOpenChange={setIsCancelDialogOpen}
        trackingNumber={parcel.trackingNumber}
        onConfirm={async (reason) => {
          await cancelParcel.mutateAsync({ id: parcel.id, reason });
          setIsCancelDialogOpen(false);
        }}
        isPending={cancelParcel.isPending}
      />
    </DetailShell>
  );
}
