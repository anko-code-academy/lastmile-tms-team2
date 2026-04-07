using LastMile.TMS.Application.BinLocations.DTOs;
using LastMile.TMS.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Application.BinLocations.Mappings;

[Mapper]
public static partial class BinLocationMappings
{
    [MapperIgnoreTarget(nameof(StorageZone.NormalizedName))]
    [MapperIgnoreTarget(nameof(StorageZone.Depot))]
    [MapperIgnoreTarget(nameof(StorageZone.StorageAisles))]
    [MapperIgnoreTarget(nameof(StorageZone.CreatedAt))]
    [MapperIgnoreTarget(nameof(StorageZone.CreatedBy))]
    [MapperIgnoreTarget(nameof(StorageZone.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(StorageZone.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(StorageZone.Id))]
    public static partial StorageZone ToEntity(this CreateStorageZoneDto dto);

    [MapperIgnoreTarget(nameof(StorageZone.NormalizedName))]
    [MapperIgnoreTarget(nameof(StorageZone.Depot))]
    [MapperIgnoreTarget(nameof(StorageZone.StorageAisles))]
    [MapperIgnoreTarget(nameof(StorageZone.CreatedAt))]
    [MapperIgnoreTarget(nameof(StorageZone.CreatedBy))]
    [MapperIgnoreTarget(nameof(StorageZone.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(StorageZone.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(StorageZone.Id))]
    public static partial void UpdateEntity(this UpdateStorageZoneDto dto, [MappingTarget] StorageZone entity);

    [MapperIgnoreTarget(nameof(StorageAisle.NormalizedName))]
    [MapperIgnoreTarget(nameof(StorageAisle.StorageZone))]
    [MapperIgnoreTarget(nameof(StorageAisle.BinLocations))]
    [MapperIgnoreTarget(nameof(StorageAisle.CreatedAt))]
    [MapperIgnoreTarget(nameof(StorageAisle.CreatedBy))]
    [MapperIgnoreTarget(nameof(StorageAisle.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(StorageAisle.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(StorageAisle.Id))]
    public static partial StorageAisle ToEntity(this CreateStorageAisleDto dto);

    [MapperIgnoreTarget(nameof(StorageAisle.NormalizedName))]
    [MapperIgnoreTarget(nameof(StorageAisle.StorageZone))]
    [MapperIgnoreTarget(nameof(StorageAisle.BinLocations))]
    [MapperIgnoreTarget(nameof(StorageAisle.CreatedAt))]
    [MapperIgnoreTarget(nameof(StorageAisle.CreatedBy))]
    [MapperIgnoreTarget(nameof(StorageAisle.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(StorageAisle.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(StorageAisle.Id))]
    public static partial void UpdateEntity(this UpdateStorageAisleDto dto, [MappingTarget] StorageAisle entity);

    [MapperIgnoreTarget(nameof(BinLocation.NormalizedName))]
    [MapperIgnoreTarget(nameof(BinLocation.StorageAisle))]
    [MapperIgnoreTarget(nameof(BinLocation.CreatedAt))]
    [MapperIgnoreTarget(nameof(BinLocation.CreatedBy))]
    [MapperIgnoreTarget(nameof(BinLocation.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(BinLocation.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(BinLocation.Id))]
    public static partial BinLocation ToEntity(this CreateBinLocationDto dto);

    [MapperIgnoreTarget(nameof(BinLocation.NormalizedName))]
    [MapperIgnoreTarget(nameof(BinLocation.StorageAisle))]
    [MapperIgnoreTarget(nameof(BinLocation.CreatedAt))]
    [MapperIgnoreTarget(nameof(BinLocation.CreatedBy))]
    [MapperIgnoreTarget(nameof(BinLocation.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(BinLocation.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(BinLocation.Id))]
    public static partial void UpdateEntity(this UpdateBinLocationDto dto, [MappingTarget] BinLocation entity);

    public static partial StorageZoneResultDto ToResultDto(this StorageZone entity);

    public static partial StorageAisleResultDto ToResultDto(this StorageAisle entity);

    public static partial BinLocationResultDto ToResultDto(this BinLocation entity);
}
