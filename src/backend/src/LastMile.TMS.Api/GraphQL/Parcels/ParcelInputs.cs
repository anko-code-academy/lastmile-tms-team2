using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Api.GraphQL.Parcels;

public sealed class RegisterParcelRecipientAddressInput
{
    public string Street1 { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public bool IsResidential { get; set; } = true;
    public string? ContactName { get; set; }
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public sealed class RegisterParcelInput
{
    public Guid ShipperAddressId { get; set; }
    public RegisterParcelRecipientAddressInput RecipientAddress { get; set; } = null!;
    public string? Description { get; set; }
    public ServiceType ServiceType { get; set; } = ServiceType.Standard;
    public decimal Weight { get; set; }
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public DimensionUnit DimensionUnit { get; set; } = DimensionUnit.Cm;
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset EstimatedDeliveryDate { get; set; }
    public string? ParcelType { get; set; }
}

public sealed class UpdateParcelInput
{
    public Guid Id { get; set; }
    public Guid ShipperAddressId { get; set; }
    public RegisterParcelRecipientAddressInput RecipientAddress { get; set; } = null!;
    public string? Description { get; set; }
    public ServiceType ServiceType { get; set; } = ServiceType.Standard;
    public decimal Weight { get; set; }
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public DimensionUnit DimensionUnit { get; set; } = DimensionUnit.Cm;
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset EstimatedDeliveryDate { get; set; }
    public string? ParcelType { get; set; }
}

public sealed class CancelParcelInput
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class TransitionParcelStatusInput
{
    public Guid ParcelId { get; set; }
    public ParcelStatus NewStatus { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
}
