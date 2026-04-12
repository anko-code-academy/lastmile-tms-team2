using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Routes.DTOs;

public sealed record RouteStopDraftDto
{
    public int Sequence { get; init; }
    public List<Guid> ParcelIds { get; init; } = [];

    public RouteStopDraftDto() { }
}

public sealed record CreateRouteDto
{
    public Guid ZoneId { get; init; }
    public Guid VehicleId { get; init; }
    public Guid DriverId { get; init; }
    public StagingArea StagingArea { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public int StartMileage { get; init; }
    public RouteAssignmentMode AssignmentMode { get; init; } = RouteAssignmentMode.ManualParcels;
    public RouteStopMode StopMode { get; init; } = RouteStopMode.Auto;
    public List<Guid> ParcelIds { get; init; } = [];
    public List<RouteStopDraftDto> Stops { get; init; } = [];

    public CreateRouteDto() { }
}

public sealed record UpdateRouteAssignmentDto
{
    public Guid VehicleId { get; init; }
    public Guid DriverId { get; init; }

    public UpdateRouteAssignmentDto() { }
}

public sealed record CancelRouteDto
{
    public string Reason { get; init; } = string.Empty;

    public CancelRouteDto() { }
}

public sealed record AdjustRouteParcelDto
{
    public Guid ParcelId { get; init; }
    public string Reason { get; init; } = string.Empty;

    public AdjustRouteParcelDto() { }
}

public sealed record CompleteRouteDto
{
    public int EndMileage { get; init; }

    public CompleteRouteDto() { }
}
