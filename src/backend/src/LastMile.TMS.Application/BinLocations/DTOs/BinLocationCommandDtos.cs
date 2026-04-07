namespace LastMile.TMS.Application.BinLocations.DTOs;

public sealed record CreateStorageZoneDto
{
    public string Name { get; init; } = string.Empty;
    public Guid DepotId { get; init; }
}

public sealed record UpdateStorageZoneDto
{
    public string Name { get; init; } = string.Empty;
    public Guid DepotId { get; init; }
}

public sealed record CreateStorageAisleDto
{
    public string Name { get; init; } = string.Empty;
    public Guid StorageZoneId { get; init; }
}

public sealed record UpdateStorageAisleDto
{
    public string Name { get; init; } = string.Empty;
    public Guid StorageZoneId { get; init; }
}

public sealed record CreateBinLocationDto
{
    public string Name { get; init; } = string.Empty;
    public Guid StorageAisleId { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record UpdateBinLocationDto
{
    public string Name { get; init; } = string.Empty;
    public Guid StorageAisleId { get; init; }
    public bool IsActive { get; init; }
}
