using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed record RegisterParcelRecipientAddressDto
{
    public string Street1 { get; init; } = string.Empty;
    public string? Street2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public bool IsResidential { get; init; } = true;
    public string? ContactName { get; init; }
    public string? CompanyName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }

    public RegisterParcelRecipientAddressDto() { }
}

public sealed record RegisterParcelDto
{
    public Guid ShipperAddressId { get; init; }
    public RegisterParcelRecipientAddressDto RecipientAddress { get; init; } = null!;
    public string? Description { get; init; }
    public ServiceType ServiceType { get; init; } = ServiceType.Standard;
    public decimal Weight { get; init; }
    public WeightUnit WeightUnit { get; init; } = WeightUnit.Kg;
    public decimal Length { get; init; }
    public decimal Width { get; init; }
    public decimal Height { get; init; }
    public DimensionUnit DimensionUnit { get; init; } = DimensionUnit.Cm;
    public decimal DeclaredValue { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime EstimatedDeliveryDate { get; init; }
    public string? ParcelType { get; init; }

    public RegisterParcelDto() { }
}
