using System.Globalization;
using System.Xml;
using LastMile.TMS.Application.Depots.DTOs;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Api.GraphQL.Depots;

[Mapper]
public static partial class DepotInputMapper
{
    public static partial CreateDepotDto ToDto(this CreateDepotInput input);

    public static partial UpdateDepotDto ToDto(this UpdateDepotInput input);

    public static partial AddressDto ToDto(this AddressInput input);

    public static partial OperatingHoursDto ToDto(this OperatingHoursInput input);

    public static partial List<OperatingHoursDto> ToDtos(this IEnumerable<OperatingHoursInput> input);

    private static TimeOnly? MapToTimeOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (TimeOnly.TryParse(
                normalizedValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedTime))
        {
            return parsedTime;
        }

        if (TryParseIsoDuration(normalizedValue, out var duration))
        {
            return TimeOnly.FromTimeSpan(duration);
        }

        throw new FormatException($"String '{value}' was not recognized as a valid TimeOnly.");
    }

    private static bool TryParseIsoDuration(string value, out TimeSpan duration)
    {
        duration = default;

        try
        {
            duration = XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException)
        {
            return false;
        }

        if (duration < TimeSpan.Zero || duration >= TimeSpan.FromDays(1))
        {
            duration = default;
            return false;
        }

        return true;
    }
}
