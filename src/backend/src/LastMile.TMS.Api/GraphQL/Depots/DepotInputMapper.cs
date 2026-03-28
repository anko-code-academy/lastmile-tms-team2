using LastMile.TMS.Application.Depots.DTOs;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Api.GraphQL.Depots;

[Mapper]
public static partial class DepotInputMapper
{
    public static partial AddressDto ToDto(this AddressInput input);

    public static partial OperatingHoursDto ToDto(this OperatingHoursInput input);

    public static partial List<OperatingHoursDto> ToDtos(this IEnumerable<OperatingHoursInput> input);

    private static TimeOnly? MapToTimeOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TimeOnly.Parse(value);
    }
}
