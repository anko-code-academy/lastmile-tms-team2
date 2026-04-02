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
    private static readonly Point DeterministicPoint =
        NtsGeometryServices.Instance
            .CreateGeometryFactory(srid: 4326)
            .CreatePoint(new Coordinate(0.5, 0.5));

    public Task<Point?> GeocodeAsync(string address, CancellationToken cancellationToken = default) =>
        Task.FromResult<Point?>(DeterministicPoint);
}
