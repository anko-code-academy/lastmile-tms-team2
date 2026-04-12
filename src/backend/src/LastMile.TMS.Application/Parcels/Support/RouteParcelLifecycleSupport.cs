using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Parcels.Support;

internal static class RouteParcelLifecycleSupport
{
    public static bool TransitionStatus(
        IAppDbContext dbContext,
        Parcel parcel,
        ParcelStatus newStatus,
        DateTimeOffset timestamp,
        string actor,
        string? location,
        string description)
    {
        if (parcel.Status == newStatus)
        {
            return false;
        }

        var previousStatus = parcel.Status;
        if (!parcel.CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Parcel {parcel.TrackingNumber} cannot transition from {previousStatus} to {newStatus}.");
        }

        parcel.TransitionTo(newStatus);
        parcel.LastModifiedAt = timestamp;
        parcel.LastModifiedBy = actor;

        if (newStatus == ParcelStatus.Delivered)
        {
            parcel.ActualDeliveryDate = timestamp;
            parcel.DeliveryAttempts = Math.Max(parcel.DeliveryAttempts + 1, 1);
        }

        var historyEntry = new ParcelChangeHistoryEntry
        {
            ParcelId = parcel.Id,
            Action = ParcelChangeAction.Updated,
            FieldName = "Status",
            BeforeValue = ParcelChangeSupport.FormatEnum(previousStatus),
            AfterValue = ParcelChangeSupport.FormatEnum(newStatus),
            ChangedAt = timestamp,
            ChangedBy = actor,
        };

        parcel.ChangeHistory.Add(historyEntry);
        dbContext.ParcelChangeHistoryEntries.Add(historyEntry);
        parcel.TrackingEvents.Add(
            ParcelTrackingEventFactory.CreateForParcelStatus(
                parcel.Id,
                newStatus,
                timestamp,
                location,
                description,
                actor));

        return true;
    }

    public static bool ReturnToStagedFromDispatchedRouteAdjustment(
        IAppDbContext dbContext,
        Parcel parcel,
        DateTimeOffset timestamp,
        string actor,
        string? location,
        string description)
    {
        if (parcel.Status == ParcelStatus.Staged)
        {
            return false;
        }

        var previousStatus = parcel.Status;
        parcel.ReturnToStagedFromDispatchedRouteAdjustment();
        parcel.LastModifiedAt = timestamp;
        parcel.LastModifiedBy = actor;

        var historyEntry = new ParcelChangeHistoryEntry
        {
            ParcelId = parcel.Id,
            Action = ParcelChangeAction.Updated,
            FieldName = "Status",
            BeforeValue = ParcelChangeSupport.FormatEnum(previousStatus),
            AfterValue = ParcelChangeSupport.FormatEnum(ParcelStatus.Staged),
            ChangedAt = timestamp,
            ChangedBy = actor,
        };

        parcel.ChangeHistory.Add(historyEntry);
        dbContext.ParcelChangeHistoryEntries.Add(historyEntry);
        parcel.TrackingEvents.Add(
            ParcelTrackingEventFactory.CreateForParcelStatus(
                parcel.Id,
                ParcelStatus.Staged,
                timestamp,
                location,
                description,
                actor));

        return true;
    }

    public static bool PromoteToOutForDeliveryFromDispatchedRouteAdjustment(
        IAppDbContext dbContext,
        Parcel parcel,
        DateTimeOffset timestamp,
        string actor,
        string? location,
        string description)
    {
        if (parcel.Status == ParcelStatus.OutForDelivery)
        {
            return false;
        }

        var previousStatus = parcel.Status;
        parcel.PromoteToOutForDeliveryFromDispatchedRouteAdjustment();
        parcel.LastModifiedAt = timestamp;
        parcel.LastModifiedBy = actor;

        var historyEntry = new ParcelChangeHistoryEntry
        {
            ParcelId = parcel.Id,
            Action = ParcelChangeAction.Updated,
            FieldName = "Status",
            BeforeValue = ParcelChangeSupport.FormatEnum(previousStatus),
            AfterValue = ParcelChangeSupport.FormatEnum(ParcelStatus.OutForDelivery),
            ChangedAt = timestamp,
            ChangedBy = actor,
        };

        parcel.ChangeHistory.Add(historyEntry);
        dbContext.ParcelChangeHistoryEntries.Add(historyEntry);
        parcel.TrackingEvents.Add(
            ParcelTrackingEventFactory.CreateForParcelStatus(
                parcel.Id,
                ParcelStatus.OutForDelivery,
                timestamp,
                location,
                description,
                actor));

        return true;
    }

    public static string GetStagingAreaLocation(StagingArea stagingArea) => $"Staging Area {stagingArea}";

    public static string GetVehicleLocation(string? registrationPlate) =>
        string.IsNullOrWhiteSpace(registrationPlate)
            ? "Vehicle"
            : $"Vehicle {registrationPlate.Trim()}";
}
