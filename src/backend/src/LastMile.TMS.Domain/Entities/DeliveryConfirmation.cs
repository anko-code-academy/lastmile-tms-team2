using LastMile.TMS.Domain.Common;

namespace LastMile.TMS.Domain.Entities;

public class DeliveryConfirmation : BaseAuditableEntity
{
    public Guid ParcelId { get; set; }
    public string? ReceivedBy { get; set; } = string.Empty;
    public string? DeliveryLocation { get; set; } = string.Empty;
    public string? SignatureImageKey { get; set; }
    public string? PhotoKey { get; set; }
    public byte[]? SignatureImage { get; set; }
    public byte[]? Photo { get; set; }
    public DateTimeOffset DeliveredAt { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }

    // Navigation property
    public Parcel Parcel { get; set; } = null!;
}
