using LastMile.TMS.Application.Parcels.Services;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Api.Tests;

/// <summary>
/// Deterministic geocoding service for integration tests.
/// Always returns a point that falls inside the seeded PostGIS zone polygon
/// (zone boundary: -87.6460 to -87.6180 longitude, 41.8745 to 41.8995 latitude).
/// This removes the external geocoding network dependency from API tests while still exercising
/// the real PostGIS ST_Covers zone-matching logic.
/// </summary>
public sealed class TestGeocodingService : IGeocodingService
{
    // Point inside the seeded downtown Chicago zone polygon (SRID 4326).
    private static readonly Point DeterministicPoint =
        NtsGeometryServices.Instance
            .CreateGeometryFactory(srid: 4326)
            .CreatePoint(new Coordinate(-87.6320, 41.8870));

    public Task<Point?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        // Return a deterministic point regardless of the input address string.
        // The zone matching service (PostGIS ST_Covers) is exercised in full;
        // only the external geocoding HTTP call is stubbed.
        return Task.FromResult<Point?>(DeterministicPoint);
    }
}
