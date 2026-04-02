"use client";

import Image from "next/image";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  ArrowLeft,
  Pencil,
  Phone,
  Mail,
  IdCard,
  MapPin,
  User,
  UserCircle,
} from "lucide-react";
import { useSession } from "next-auth/react";

import {
  DetailBreadcrumb,
  DetailContainer,
  DetailEmptyState,
  DetailField,
  DetailFieldGrid,
  DetailHero,
  DetailPageSectionProvider,
  DetailPageSkeleton,
  DetailPanel,
  DetailShell,
  DETAIL_PAGE_CONTENT_PADDING,
} from "@/components/detail";
import { Button, buttonVariants } from "@/components/ui/button";
import { QueryErrorAlert } from "@/components/feedback/query-error-alert";
import { DAY_OF_WEEK_LABELS } from "@/lib/labels/drivers";
import { cn } from "@/lib/utils";
import { getErrorMessage } from "@/lib/network/error-message";
import { absoluteApiAssetUrl } from "@/lib/network/api";
import { formatShiftTimeLabel, normalizeTimeToHms } from "@/lib/time/shift-time";
import {
  DRIVER_STATUS_LABELS,
  driverStatusBadgeClass,
} from "@/lib/labels/drivers";
import { useDriver, useDeleteDriver } from "@/queries/drivers";
import { DeleteDriverDialog } from "@/components/drivers/delete-driver-dialog";
import { useState } from "react";

function DriverDetailProfilePhoto({
  photoSrc,
  displayName,
}: {
  photoSrc: string;
  displayName: string;
}) {
  const [photoLoadError, setPhotoLoadError] = useState(false);
  return (
    <>
      {!photoLoadError ? (
        <Image
          src={photoSrc}
          alt={`${displayName} profile`}
          width={128}
          height={128}
          sizes="128px"
          unoptimized
          className="h-32 w-32 rounded-full border object-cover"
          onError={() => setPhotoLoadError(true)}
        />
      ) : (
        <div
          className="flex h-32 w-32 flex-col items-center justify-center gap-1 rounded-full border border-dashed border-muted-foreground/35 bg-muted/40 p-2 text-center text-muted-foreground"
          role="img"
          aria-label="Photo file not found on server"
        >
          <UserCircle className="h-14 w-14 shrink-0 opacity-80" strokeWidth={1.25} />
          <span className="text-[10px] leading-tight">
            File missing — re-upload in Edit
          </span>
        </div>
      )}
    </>
  );
}

export default function DriverDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { status: sessionStatus } = useSession();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const { data: driver, isLoading, error } = useDriver(id);
  const deleteDriver = useDeleteDriver();

  if (sessionStatus === "loading" || isLoading)
    return <DetailPageSkeleton variant="driver" />;
  if (error)
    return (
      <DetailShell variant="driver">
        <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
          <QueryErrorAlert
            title="Could not load driver"
            message={getErrorMessage(error)}
          />
        </DetailContainer>
      </DetailShell>
    );
  if (!driver)
    return (
      <DetailShell variant="driver">
        <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
          <DetailBreadcrumb
            variant="driver"
            items={[{ label: "Drivers", href: "/drivers" }, { label: "Not found" }]}
          />
          <DetailEmptyState
            title="Driver not found"
            message="This driver may have been removed or the link is incorrect."
          />
        </DetailContainer>
      </DetailShell>
    );

  const handleDelete = async () => {
    await deleteDriver.mutateAsync({ id, displayName: driver.displayName });
    router.push("/drivers");
  };

  const photoSrc = absoluteApiAssetUrl(driver.photoUrl);

  return (
    <DetailShell variant="driver">
      <DetailContainer className={DETAIL_PAGE_CONTENT_PADDING}>
        <DetailPageSectionProvider section="driver">
          <DetailBreadcrumb
            variant="driver"
            items={[
              { label: "Drivers", href: "/drivers" },
              { label: driver.displayName },
            ]}
          />

          <DetailHero
            section="driver"
            eyebrow="Drivers"
            icon={<User strokeWidth={1.75} />}
            title={driver.displayName}
            subtitle={
              <>
                {driver.email && (
                  <span className="text-foreground/80">{driver.email}</span>
                )}
                {driver.depotName && (
                  <>
                    {" · "}
                    <span className="text-foreground/80">{driver.depotName}</span>
                  </>
                )}
              </>
            }
            badge={
              <span className={driverStatusBadgeClass(driver.status)}>
                {DRIVER_STATUS_LABELS[driver.status]}
              </span>
            }
            actions={
              <>
                <Button
                  variant="default"
                  size="sm"
                  onClick={() => setDeleteDialogOpen(true)}
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                >
                  Delete
                </Button>
                <Link
                  href={`/drivers/${id}/edit`}
                  className={cn(buttonVariants({ variant: "default", size: "sm" }))}
                >
                  <Pencil className="mr-2 size-4" aria-hidden />
                  Edit
                </Link>
                <Link
                  href="/drivers"
                  className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                >
                  <ArrowLeft className="mr-2 size-4" aria-hidden />
                  All drivers
                </Link>
              </>
            }
          />

          <DetailPanel
            className="detail-panel-animate"
            section="driver"
            title="Contact & License"
            description="Driver contact information and license credentials."
          >
            <DetailFieldGrid>
              {photoSrc && (
                <DetailField label="Photo">
                  <DriverDetailProfilePhoto
                    key={`${driver.id}-${driver.photoUrl ?? ""}`}
                    photoSrc={photoSrc}
                    displayName={driver.displayName}
                  />
                </DetailField>
              )}
              {driver.phone && (
                <DetailField label="Phone">
                  <div className="flex items-center gap-2">
                    <Phone className="size-4 text-muted-foreground" aria-hidden />
                    {driver.phone}
                  </div>
                </DetailField>
              )}
              {driver.email && (
                <DetailField label="Email">
                  <div className="flex items-center gap-2">
                    <Mail className="size-4 text-muted-foreground" aria-hidden />
                    {driver.email}
                  </div>
                </DetailField>
              )}
              {driver.licenseNumber && (
                <DetailField label="License number">
                  <div className="flex items-center gap-2">
                    <IdCard className="size-4 text-muted-foreground" aria-hidden />
                    {driver.licenseNumber}
                  </div>
                </DetailField>
              )}
              {driver.licenseExpiryDate && (
                <DetailField label="License expiry">
                  {new Date(driver.licenseExpiryDate).toLocaleDateString()}
                </DetailField>
              )}
            </DetailFieldGrid>
          </DetailPanel>

          <DetailPanel
            className="detail-panel-animate"
            section="driver"
            title="Assignment"
            description="Zone, depot, and user account linking."
          >
            <DetailFieldGrid>
              {driver.zoneName && (
                <DetailField label="Zone">
                  <div className="flex items-center gap-2">
                    <MapPin className="size-4 text-muted-foreground" aria-hidden />
                    {driver.zoneName}
                  </div>
                </DetailField>
              )}
              {driver.depotName && (
                <DetailField label="Depot">
                  <div className="flex items-center gap-2">
                    <MapPin className="size-4 text-muted-foreground" aria-hidden />
                    {driver.depotName}
                  </div>
                </DetailField>
              )}
              {driver.userName && (
                <DetailField label="Linked user">
                  <div className="flex items-center gap-2">
                    <User className="size-4 text-muted-foreground" aria-hidden />
                    {driver.userName}
                  </div>
                </DetailField>
              )}
            </DetailFieldGrid>
          </DetailPanel>

          <DetailPanel
            className="detail-panel-animate"
            section="driver"
            title="Availability Schedule"
            description="Weekly working hours and days off."
          >
            <div className="space-y-3">
              {driver.availabilitySchedule.map((entry) => (
                <div
                  key={entry.dayOfWeek}
                  className="flex items-center gap-3 rounded-lg border p-3"
                >
                  <span className="w-28 font-medium">
                    {DAY_OF_WEEK_LABELS[entry.dayOfWeek]}
                  </span>
                  {entry.isAvailable ? (
                    <span className="text-sm text-muted-foreground">
                      {formatShiftTimeLabel(
                        normalizeTimeToHms(entry.shiftStart ?? ""),
                      )}{" "}
                      —{" "}
                      {formatShiftTimeLabel(
                        normalizeTimeToHms(entry.shiftEnd ?? ""),
                      )}
                    </span>
                  ) : (
                    <span className="text-sm text-muted-foreground">Day off</span>
                  )}
                </div>
              ))}
            </div>
          </DetailPanel>

          {driver.createdAt && (
            <DetailPanel
              className="detail-panel-animate"
              section="driver"
              title="Audit"
              description="Record creation and modification timestamps."
            >
              <DetailFieldGrid>
                <DetailField label="Created">
                  {new Date(driver.createdAt).toLocaleString()}
                </DetailField>
                {driver.updatedAt && (
                  <DetailField label="Last modified">
                    {new Date(driver.updatedAt).toLocaleString()}
                  </DetailField>
                )}
              </DetailFieldGrid>
            </DetailPanel>
          )}
        </DetailPageSectionProvider>
      </DetailContainer>

      <DeleteDriverDialog
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        displayName={driver.displayName}
        onConfirm={handleDelete}
        isPending={deleteDriver.isPending}
      />
    </DetailShell>
  );
}
