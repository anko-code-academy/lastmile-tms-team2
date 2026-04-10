using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Application.Depots.Commands;

internal static class DepotAddressGeocodingSupport
{
    public static async Task ApplyGeoLocationAsync(
        Address address,
        IGeocodingService geocodingService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(geocodingService);

        var query = BuildAddressQuery(address);
        address.GeoLocation = string.IsNullOrWhiteSpace(query)
            ? null
            : await geocodingService.GeocodeAsync(query, cancellationToken);
    }

    public static string BuildAddressQuery(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return string.Join(", ", GetAddressParts(address));
    }

    private static IEnumerable<string> GetAddressParts(Address address)
    {
        foreach (var part in new[]
                 {
                     address.Street1,
                     address.Street2,
                     address.City,
                     address.State,
                     address.PostalCode,
                     address.CountryCode,
                 })
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                yield return part.Trim();
            }
        }
    }
}
