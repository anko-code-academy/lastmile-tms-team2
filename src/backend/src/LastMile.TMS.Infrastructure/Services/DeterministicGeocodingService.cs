using LastMile.TMS.Application.Parcels.Services;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Infrastructure.Services;

/// <summary>
/// Deterministic geocoding for the in-memory e2e stack.
/// Returns a point inside the seeded test-support zone polygon.
/// </summary>
public sealed class DeterministicGeocodingService : IGeocodingService
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public Task<Point?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        var hash = Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(address ?? string.Empty));
        var longitude = 0.1 + (hash % 8000) / 10000d;
        var latitude = 0.1 + ((hash / 8000) % 8000) / 10000d;
        return Task.FromResult<Point?>(GeometryFactory.CreatePoint(new Coordinate(longitude, latitude)));
    }
}
