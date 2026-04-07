namespace LastMile.TMS.Application.BinLocations.DTOs;

public sealed record DepotStorageLayoutDto
{
    public Guid DepotId { get; init; }
    public string DepotName { get; init; } = string.Empty;
    public IReadOnlyList<StorageZoneResultDto> StorageZones { get; init; } = [];
}

public sealed record StorageZoneResultDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid DepotId { get; init; }
    public IReadOnlyList<StorageAisleResultDto> StorageAisles { get; init; } = [];
}

public sealed record StorageAisleResultDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid StorageZoneId { get; init; }
    public IReadOnlyList<BinLocationResultDto> BinLocations { get; init; } = [];
}

public sealed record BinLocationResultDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public Guid StorageAisleId { get; init; }
}
