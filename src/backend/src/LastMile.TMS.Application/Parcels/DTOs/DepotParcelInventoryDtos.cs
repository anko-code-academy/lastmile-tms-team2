namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed class DepotParcelInventoryDashboardDto
{
    public Guid DepotId { get; init; }

    public string DepotName { get; init; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyList<DepotParcelInventoryStatusCountDto> StatusCounts { get; init; } =
        Array.Empty<DepotParcelInventoryStatusCountDto>();

    public IReadOnlyList<DepotParcelInventoryZoneCountDto> ZoneCounts { get; init; } =
        Array.Empty<DepotParcelInventoryZoneCountDto>();

    public DepotParcelAgingAlertDto AgingAlert { get; init; } = new();
}

public sealed class DepotParcelInventoryStatusCountDto
{
    public string Status { get; init; } = string.Empty;

    public int Count { get; init; }
}

public sealed class DepotParcelInventoryZoneCountDto
{
    public Guid ZoneId { get; init; }

    public string ZoneName { get; init; } = string.Empty;

    public int Count { get; init; }
}

public sealed class DepotParcelAgingAlertDto
{
    public int ThresholdMinutes { get; init; }

    public int Count { get; init; }
}

public sealed class DepotParcelInventoryParcelConnectionDto
{
    public int TotalCount { get; init; }

    public DepotParcelInventoryPageInfoDto PageInfo { get; init; } = new();

    public IReadOnlyList<DepotParcelInventoryParcelDto> Nodes { get; init; } =
        Array.Empty<DepotParcelInventoryParcelDto>();
}

public sealed class DepotParcelInventoryPageInfoDto
{
    public bool HasNextPage { get; init; }

    public bool HasPreviousPage { get; init; }

    public string? StartCursor { get; init; }

    public string? EndCursor { get; init; }
}

public sealed class DepotParcelInventoryParcelDto
{
    public Guid Id { get; init; }

    public string TrackingNumber { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public Guid ZoneId { get; init; }

    public string ZoneName { get; init; } = string.Empty;

    public int AgeMinutes { get; init; }

    public DateTimeOffset LastUpdatedAt { get; init; }
}
