using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Routes.DTOs;

public sealed record RouteParcelAdjustmentCandidateDto
{
    public Guid Id { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string RecipientLabel { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public double? Longitude { get; init; }
    public double? Latitude { get; init; }
    public ParcelStatus Status { get; init; }

    public RouteParcelAdjustmentCandidateDto() { }
}
