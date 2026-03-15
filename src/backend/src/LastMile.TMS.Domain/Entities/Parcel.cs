using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Entities;

public class Parcel : BaseAuditableEntity
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public ParcelStatus Status { get; set; }
    public Guid ShipperAddressId { get; set; }
    public Guid RecipientAddressId { get; set; }
    public decimal Weight { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public DimensionUnit DimensionUnit { get; set; }
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset EstimatedDeliveryDate { get; set; }
    public DateTimeOffset? ActualDeliveryDate { get; set; }
    public int DeliveryAttempts { get; set; }
    public string? ParcelType { get; set; }
    public Guid ZoneId { get; set; }

    // Navigation properties
    public Address ShipperAddress { get; set; } = null!;
    public Address RecipientAddress { get; set; } = null!;
    public DeliveryConfirmation? DeliveryConfirmation { get; set; }
    public ICollection<ParcelContentItem> ContentItems { get; set; } = new List<ParcelContentItem>();
    public ICollection<TrackingEvent> TrackingEvents { get; set; } = new List<TrackingEvent>();
    public ICollection<ParcelWatcher> Watchers { get; set; } = new List<ParcelWatcher>();
    public Zone Zone { get; set; } = null!;

    public static string GenerateTrackingNumber()
    {
        return $"LM{DateTimeOffset.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private static readonly Dictionary<ParcelStatus, ParcelStatus[]> ValidTransitions = new()
    {
        [ParcelStatus.Registered] = [ParcelStatus.ReceivedAtDepot, ParcelStatus.Cancelled],
        [ParcelStatus.ReceivedAtDepot] = [ParcelStatus.Sorted, ParcelStatus.Exception, ParcelStatus.Cancelled],
        [ParcelStatus.Sorted] = [ParcelStatus.Staged, ParcelStatus.Exception, ParcelStatus.Cancelled],
        [ParcelStatus.Staged] = [ParcelStatus.Loaded, ParcelStatus.Exception, ParcelStatus.Cancelled],
        [ParcelStatus.Loaded] = [ParcelStatus.OutForDelivery, ParcelStatus.ReturnedToDepot, ParcelStatus.Exception],
        [ParcelStatus.OutForDelivery] = [ParcelStatus.Delivered, ParcelStatus.FailedAttempt, ParcelStatus.Exception],
        [ParcelStatus.Delivered] = [ParcelStatus.ReturnedToDepot],
        [ParcelStatus.FailedAttempt] = [ParcelStatus.OutForDelivery, ParcelStatus.ReturnedToDepot, ParcelStatus.Exception],
        [ParcelStatus.ReturnedToDepot] = [ParcelStatus.Sorted, ParcelStatus.Cancelled],
        [ParcelStatus.Cancelled] = [],
        [ParcelStatus.Exception] = [ParcelStatus.Sorted, ParcelStatus.OutForDelivery, ParcelStatus.ReturnedToDepot, ParcelStatus.Cancelled]
    };

    public bool CanTransitionTo(ParcelStatus newStatus)
    {
        if (ValidTransitions.TryGetValue(Status, out var allowedStatuses))
        {
            return allowedStatuses.Contains(newStatus);
        }
        return false;
    }

    public void TransitionTo(ParcelStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {Status} to {newStatus}. Valid transitions from {Status} are: {string.Join(", ", ValidTransitions.GetValueOrDefault(Status, []))}");
        }
        Status = newStatus;
    }
}
