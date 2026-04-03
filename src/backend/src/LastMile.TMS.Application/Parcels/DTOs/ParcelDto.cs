namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed record ParcelDto
{
    public Guid Id { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ServiceType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Weight { get; init; }
    public string WeightUnit { get; init; } = string.Empty;
    public decimal Length { get; init; }
    public decimal Width { get; init; }
    public decimal Height { get; init; }
    public string DimensionUnit { get; init; } = string.Empty;
    public decimal DeclaredValue { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset EstimatedDeliveryDate { get; init; }
    public DateTimeOffset? ActualDeliveryDate { get; init; }
    public int DeliveryAttempts { get; init; }
    public string? ParcelType { get; init; }
    public Guid ZoneId { get; init; }
    public string? ZoneName { get; init; }
    public Guid DepotId { get; init; }
    public string? DepotName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastModifiedAt { get; init; }

    public ParcelDto() { }
}
