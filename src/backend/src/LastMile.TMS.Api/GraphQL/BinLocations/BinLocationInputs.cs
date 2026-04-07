namespace LastMile.TMS.Api.GraphQL.BinLocations;

public sealed class CreateStorageZoneInput
{
    public string Name { get; set; } = string.Empty;
    public Guid DepotId { get; set; }
}

public sealed class UpdateStorageZoneInput
{
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateStorageAisleInput
{
    public string Name { get; set; } = string.Empty;
    public Guid StorageZoneId { get; set; }
}

public sealed class UpdateStorageAisleInput
{
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateBinLocationInput
{
    public string Name { get; set; } = string.Empty;
    public Guid StorageAisleId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateBinLocationInput
{
    public string Name { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}
