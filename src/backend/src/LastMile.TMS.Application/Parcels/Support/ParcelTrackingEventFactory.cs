using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Parcels.Support;

public static class ParcelTrackingEventFactory
{
    public static EventType MapEventTypeForParcelStatus(ParcelStatus status) => status switch
    {
        ParcelStatus.Registered => EventType.LabelCreated,
        ParcelStatus.ReceivedAtDepot => EventType.ArrivedAtFacility,
        ParcelStatus.Sorted => EventType.DepartedFacility,
        ParcelStatus.Staged => EventType.HeldAtFacility,
        ParcelStatus.Loaded => EventType.DepartedFacility,
        ParcelStatus.OutForDelivery => EventType.OutForDelivery,
        ParcelStatus.Delivered => EventType.Delivered,
        ParcelStatus.FailedAttempt => EventType.DeliveryAttempted,
        ParcelStatus.ReturnedToDepot => EventType.Returned,
        ParcelStatus.Exception => EventType.Exception,
        ParcelStatus.Cancelled => EventType.Exception,
        _ => EventType.InTransit,
    };

    public static TrackingEvent CreateForParcelStatus(
        Guid parcelId,
        ParcelStatus newStatus,
        DateTimeOffset timestamp,
        string? location,
        string? description,
        string? actor)
    {
        return new TrackingEvent
        {
            ParcelId = parcelId,
            Timestamp = timestamp,
            EventType = MapEventTypeForParcelStatus(newStatus),
            Description = description ?? string.Empty,
            Location = location,
            Operator = actor,
            CreatedBy = actor,
        };
    }
}
