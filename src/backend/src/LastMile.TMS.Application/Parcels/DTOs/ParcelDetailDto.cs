namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed record ParcelDetailAddressDto
{
    public string Street1 { get; init; } = string.Empty;
    public string? Street2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public bool IsResidential { get; init; }
    public string? ContactName { get; init; }
    public string? CompanyName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }

    public ParcelDetailAddressDto() { }
}

public sealed record ParcelDetailDto
{
    public Guid Id { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid ShipperAddressId { get; init; }
    public string ServiceType { get; init; } = string.Empty;
    public decimal Weight { get; init; }
    public string WeightUnit { get; init; } = string.Empty;
    public decimal Length { get; init; }
    public decimal Width { get; init; }
    public decimal Height { get; init; }
    public string DimensionUnit { get; init; } = string.Empty;
    public decimal DeclaredValue { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ParcelType { get; init; }
    public string? CancellationReason { get; init; }
    public DateTimeOffset EstimatedDeliveryDate { get; init; }
    public int DeliveryAttempts { get; init; }
    public Guid ZoneId { get; init; }
    public string? ZoneName { get; init; }
    public Guid DepotId { get; init; }
    public string? DepotName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastModifiedAt { get; init; }
    public bool CanEdit { get; init; }
    public bool CanCancel { get; init; }
    public ParcelDetailAddressDto RecipientAddress { get; init; } = new();
    public IReadOnlyList<ParcelChangeHistoryDto> ChangeHistory { get; init; } = [];

    /// <summary>
    /// GraphQL <c>ParcelStatus</c> enum names (e.g. RECEIVED_AT_DEPOT) valid from the current status.
    /// </summary>
    public IReadOnlyList<string> AllowedNextStatuses { get; init; } = [];

    public ParcelDetailDto() { }
}

public sealed record ParcelChangeHistoryDto
{
    public string Action { get; init; } = string.Empty;
    public string FieldName { get; init; } = string.Empty;
    public string? BeforeValue { get; init; }
    public string? AfterValue { get; init; }
    public DateTimeOffset ChangedAt { get; init; }
    public string? ChangedBy { get; init; }

    public ParcelChangeHistoryDto() { }
}
