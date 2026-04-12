using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Api.GraphQL.Routes;

public sealed class RouteStopDraftInput
{
    public int Sequence { get; set; }
    public List<Guid> ParcelIds { get; set; } = [];
}

public sealed class RoutePlanPreviewInput
{
    public Guid ZoneId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public RouteAssignmentMode AssignmentMode { get; set; } = RouteAssignmentMode.ManualParcels;
    public RouteStopMode StopMode { get; set; } = RouteStopMode.Auto;
    public List<Guid> ParcelIds { get; set; } = [];
    public List<RouteStopDraftInput> Stops { get; set; } = [];
}

public sealed class CreateRouteInput
{
    public Guid ZoneId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DriverId { get; set; }
    public StagingArea StagingArea { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public int StartMileage { get; set; }
    public RouteAssignmentMode AssignmentMode { get; set; } = RouteAssignmentMode.ManualParcels;
    public RouteStopMode StopMode { get; set; } = RouteStopMode.Auto;
    public List<Guid> ParcelIds { get; set; } = [];
    public List<RouteStopDraftInput> Stops { get; set; } = [];
}

public sealed class UpdateRouteAssignmentInput
{
    public Guid VehicleId { get; set; }
    public Guid DriverId { get; set; }
}

public sealed class CancelRouteInput
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdjustRouteParcelInput
{
    public Guid ParcelId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class CompleteRouteInput
{
    public int EndMileage { get; set; }
}
