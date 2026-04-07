using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Api.GraphQL.BinLocations;

[Mapper]
public static partial class BinLocationInputMapper
{
    public static partial LastMile.TMS.Application.BinLocations.DTOs.CreateStorageZoneDto ToDto(this CreateStorageZoneInput input);

    public static partial LastMile.TMS.Application.BinLocations.DTOs.UpdateStorageZoneDto ToDto(this UpdateStorageZoneInput input);

    public static partial LastMile.TMS.Application.BinLocations.DTOs.CreateStorageAisleDto ToDto(this CreateStorageAisleInput input);

    public static partial LastMile.TMS.Application.BinLocations.DTOs.UpdateStorageAisleDto ToDto(this UpdateStorageAisleInput input);

    public static partial LastMile.TMS.Application.BinLocations.DTOs.CreateBinLocationDto ToDto(this CreateBinLocationInput input);

    public static partial LastMile.TMS.Application.BinLocations.DTOs.UpdateBinLocationDto ToDto(this UpdateBinLocationInput input);
}
