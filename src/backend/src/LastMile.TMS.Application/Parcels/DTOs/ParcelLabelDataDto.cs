namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed record ParcelLabelDataDto
{
    public Guid Id { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string? RecipientName { get; init; }
    public string? CompanyName { get; init; }
    public string Street1 { get; init; } = string.Empty;
    public string? Street2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string? SortZone { get; init; }
    public string? ParcelType { get; init; }

    public ParcelLabelDataDto() { }
}
