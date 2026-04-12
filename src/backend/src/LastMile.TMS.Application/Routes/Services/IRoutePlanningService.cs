using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Routes.Services;

public interface IRoutePlanningService
{
    Task<RoutePlanComputationResult> BuildPlanAsync(
        RoutePlanRequest request,
        CancellationToken cancellationToken = default);

    Task EnsureParcelRecipientGeocodedAsync(
        Parcel parcel,
        CancellationToken cancellationToken = default);

    Task ApplyMetricsToPersistedRouteAsync(
        Route route,
        CancellationToken cancellationToken = default);
}

public sealed record RoutePlanRequest
{
    public Guid ZoneId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public RouteAssignmentMode AssignmentMode { get; init; } = RouteAssignmentMode.ManualParcels;
    public RouteStopMode StopMode { get; init; } = RouteStopMode.Auto;
    public IReadOnlyList<Guid> ParcelIds { get; init; } = [];
    public IReadOnlyList<RouteStopDraftDto> Stops { get; init; } = [];

    public RoutePlanRequest() { }
}

public sealed record RoutePlanComputationResult
{
    public Guid ZoneId { get; init; }
    public string ZoneName { get; init; } = string.Empty;
    public Guid DepotId { get; init; }
    public string DepotName { get; init; } = string.Empty;
    public string DepotAddressLine { get; init; } = string.Empty;
    public double? DepotLongitude { get; init; }
    public double? DepotLatitude { get; init; }
    public IReadOnlyList<RoutePlanParcelCandidateDto> CandidateParcels { get; init; } = [];
    public IReadOnlyList<RoutePlannedStop> Stops { get; init; } = [];
    public IReadOnlyList<RoutePathPointDto> Path { get; init; } = [];
    public int PlannedDistanceMeters { get; init; }
    public int PlannedDurationSeconds { get; init; }
    public LineString? PlannedPath { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public RoutePlanPreviewDto ToPreviewDto() =>
        new()
        {
            ZoneId = ZoneId,
            ZoneName = ZoneName,
            DepotId = DepotId,
            DepotName = DepotName,
            DepotAddressLine = DepotAddressLine,
            DepotLongitude = DepotLongitude,
            DepotLatitude = DepotLatitude,
            CandidateParcels = CandidateParcels,
            Stops = Stops.Select(stop => stop.ToDto()).ToList(),
            Path = Path,
            EstimatedStopCount = Stops.Count,
            PlannedDistanceMeters = PlannedDistanceMeters,
            PlannedDurationSeconds = PlannedDurationSeconds,
            Warnings = Warnings,
        };
}

public sealed record RoutePlannedStop
{
    public string Id { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public string RecipientLabel { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public Point StopLocation { get; init; } = null!;
    public IReadOnlyList<RoutePlannedStopParcel> Parcels { get; init; } = [];

    public RouteStopDto ToDto() =>
        new()
        {
            Id = Id,
            Sequence = Sequence,
            RecipientLabel = RecipientLabel,
            AddressLine = AddressLine,
            Longitude = StopLocation.X,
            Latitude = StopLocation.Y,
            Parcels = Parcels.Select(parcel => parcel.ToDto()).ToList(),
        };
}

public sealed record RoutePlannedStopParcel
{
    public Guid ParcelId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string RecipientLabel { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public ParcelStatus Status { get; init; }

    public RouteStopParcelDto ToDto() =>
        new()
        {
            ParcelId = ParcelId,
            TrackingNumber = TrackingNumber,
            RecipientLabel = RecipientLabel,
            AddressLine = AddressLine,
            Status = Status,
        };
}
