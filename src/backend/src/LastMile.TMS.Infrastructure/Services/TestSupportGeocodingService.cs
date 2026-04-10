using LastMile.TMS.Application.Parcels.Services;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Infrastructure.Services;

/// <summary>
/// Deterministic geocoding used when test support is enabled so end-to-end flows
/// do not depend on external HTTP geocoding availability.
/// </summary>
public sealed class TestSupportGeocodingService : IGeocodingService
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public Task<Point?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        var hash = Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(address ?? string.Empty));
        var longitude = 151.20 + (hash % 3000) / 10000d;
        var latitude = -33.80 + ((hash / 3000) % 3000) / 10000d;
        return Task.FromResult<Point?>(GeometryFactory.CreatePoint(new Coordinate(longitude, latitude)));
    }
}
