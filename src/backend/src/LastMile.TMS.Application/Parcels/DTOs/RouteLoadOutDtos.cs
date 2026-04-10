using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Parcels.DTOs;

public enum RouteLoadOutScanOutcome
{
    Loaded,
    AlreadyLoaded,
    WrongRoute,
    NotExpected,
    InvalidStatus
}

public sealed record LoadOutRouteDto
{
    public Guid Id { get; init; }
    public Guid VehicleId { get; init; }
    public string VehiclePlate { get; init; } = string.Empty;
    public Guid DriverId { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public RouteStatus Status { get; init; }
    public StagingArea StagingArea { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public int ExpectedParcelCount { get; init; }
    public int LoadedParcelCount { get; init; }
    public int RemainingParcelCount { get; init; }

    public LoadOutRouteDto() { }
}

public sealed record RouteLoadOutExpectedParcelDto
{
    public Guid ParcelId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsLoaded { get; init; }

    public RouteLoadOutExpectedParcelDto() { }
}

public sealed record RouteLoadOutBoardDto
{
    public Guid Id { get; init; }
    public Guid VehicleId { get; init; }
    public string VehiclePlate { get; init; } = string.Empty;
    public Guid DriverId { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public RouteStatus Status { get; init; }
    public StagingArea StagingArea { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public int ExpectedParcelCount { get; init; }
    public int LoadedParcelCount { get; init; }
    public int RemainingParcelCount { get; init; }
    public IReadOnlyList<RouteLoadOutExpectedParcelDto> ExpectedParcels { get; init; } = [];

    public RouteLoadOutBoardDto() { }
}

public sealed record LoadParcelForRouteResultDto
{
    public RouteLoadOutScanOutcome Outcome { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? TrackingNumber { get; init; }
    public Guid? ParcelId { get; init; }
    public Guid? ConflictingRouteId { get; init; }
    public StagingArea? ConflictingStagingArea { get; init; }
    public RouteLoadOutBoardDto Board { get; init; } = new();

    public LoadParcelForRouteResultDto() { }
}

public sealed record CompleteLoadOutResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int LoadedCount { get; init; }
    public int SkippedCount { get; init; }
    public int TotalCount { get; init; }
    public RouteLoadOutBoardDto Board { get; init; } = new();

    public CompleteLoadOutResultDto() { }
}
