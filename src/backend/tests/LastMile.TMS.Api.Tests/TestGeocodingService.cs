using LastMile.TMS.Application.Parcels.Services;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Api.Tests;

/// <summary>
/// Deterministic geocoding service for integration tests.
/// Always returns a point that falls inside the seeded PostGIS zone polygon
/// (zone boundary: 151.0E-152.0E longitude, -34.0S to -33.0S latitude).
/// This removes the external geocoding network dependency from API tests while still exercising
/// the real PostGIS ST_Covers zone-matching logic.
/// </summary>
public sealed class TestGeocodingService : IGeocodingService
{
    // Center of the seeded 1x1-degree Sydney zone: 151.5E longitude, 33.5S latitude (SRID 4326)
    private static readonly Point DeterministicPoint =
        NtsGeometryServices.Instance
            .CreateGeometryFactory(srid: 4326)
            .CreatePoint(new Coordinate(151.5, -33.5));

    public Task<Point?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        // Return a deterministic point regardless of the input address string.
        // The zone matching service (PostGIS ST_Covers) is exercised in full;
        // only the external geocoding HTTP call is stubbed.
        return Task.FromResult<Point?>(DeterministicPoint);
    }
}
