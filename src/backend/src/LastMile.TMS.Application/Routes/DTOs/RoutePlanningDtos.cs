using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Routes.DTOs;

public sealed record RoutePlanPreviewInputDto
{
    public Guid ZoneId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public RouteAssignmentMode AssignmentMode { get; init; } = RouteAssignmentMode.ManualParcels;
    public RouteStopMode StopMode { get; init; } = RouteStopMode.Auto;
    public List<Guid> ParcelIds { get; init; } = [];
    public List<RouteStopDraftDto> Stops { get; init; } = [];

    public RoutePlanPreviewInputDto() { }
}

public sealed record RoutePlanPreviewDto
{
    public Guid ZoneId { get; init; }
    public string ZoneName { get; init; } = string.Empty;
    public Guid DepotId { get; init; }
    public string DepotName { get; init; } = string.Empty;
    public string DepotAddressLine { get; init; } = string.Empty;
    public double? DepotLongitude { get; init; }
    public double? DepotLatitude { get; init; }
    public IReadOnlyList<RoutePlanParcelCandidateDto> CandidateParcels { get; init; } = [];
    public IReadOnlyList<RouteStopDto> Stops { get; init; } = [];
    public IReadOnlyList<RoutePathPointDto> Path { get; init; } = [];
    public int EstimatedStopCount { get; init; }
    public int PlannedDistanceMeters { get; init; }
    public int PlannedDurationSeconds { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public RoutePlanPreviewDto() { }
}

public sealed record RoutePlanParcelCandidateDto
{
    public Guid Id { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public decimal Weight { get; init; }
    public WeightUnit WeightUnit { get; init; }
    public Guid ZoneId { get; init; }
    public string ZoneName { get; init; } = string.Empty;
    public string RecipientLabel { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public double? Longitude { get; init; }
    public double? Latitude { get; init; }
    public bool IsSelected { get; init; }

    public RoutePlanParcelCandidateDto() { }
}

public sealed record RouteStopDto
{
    public string Id { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public string RecipientLabel { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public double Longitude { get; init; }
    public double Latitude { get; init; }
    public IReadOnlyList<RouteStopParcelDto> Parcels { get; init; } = [];

    public RouteStopDto() { }
}

public sealed record RouteStopParcelDto
{
    public Guid ParcelId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string RecipientLabel { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public ParcelStatus Status { get; init; }

    public RouteStopParcelDto() { }
}

public sealed record RoutePathPointDto
{
    public double Longitude { get; init; }
    public double Latitude { get; init; }

    public RoutePathPointDto() { }
}
