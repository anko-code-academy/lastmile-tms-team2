using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Routes.DTOs;

public sealed record RouteAssignmentCandidatesDto
{
    public IReadOnlyList<AssignableVehicleDto> Vehicles { get; init; } = [];
    public IReadOnlyList<AssignableDriverDto> Drivers { get; init; } = [];

    public RouteAssignmentCandidatesDto() { }
}

public sealed record AssignableVehicleDto
{
    public Guid Id { get; init; }
    public string RegistrationPlate { get; init; } = string.Empty;
    public Guid DepotId { get; init; }
    public string? DepotName { get; init; }
    public int ParcelCapacity { get; init; }
    public decimal WeightCapacity { get; init; }
    public VehicleStatus Status { get; init; }
    public bool IsCurrentAssignment { get; init; }

    public AssignableVehicleDto() { }
}

public sealed record AssignableDriverDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public Guid DepotId { get; init; }
    public Guid ZoneId { get; init; }
    public DriverStatus Status { get; init; }
    public bool IsCurrentAssignment { get; init; }
    public IReadOnlyList<DriverWorkloadRouteDto> WorkloadRoutes { get; init; } = [];

    public AssignableDriverDto() { }
}

public sealed record DriverWorkloadRouteDto
{
    public Guid RouteId { get; init; }
    public Guid VehicleId { get; init; }
    public string VehiclePlate { get; init; } = string.Empty;
    public DateTimeOffset StartDate { get; init; }
    public RouteStatus Status { get; init; }

    public DriverWorkloadRouteDto() { }
}
