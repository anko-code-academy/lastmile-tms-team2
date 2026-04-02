using System.Net.Http.Json;
using System.Globalization;
using LastMile.TMS.Application.Parcels.Services;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Infrastructure.Services;

/// <summary>
/// Geocodes addresses using the OpenStreetMap Nominatim API (no API key required).
/// https://nominatim.org/release-docs/latest/api/Overview/
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private const string NominatimBaseUrl = "https://nominatim.openstreetmap.org/search";

    public NominatimGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LastMileTMS/1.0");
    }

    public async Task<Point?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var query = Uri.EscapeDataString(address);
        var url = $"{NominatimBaseUrl}?q={query}&format=json&limit=1";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadFromJsonAsync<List<NominatimResult>>(cancellationToken);
            if (json is null || json.Count == 0)
                return null;

            var result = json[0];
            if (!double.TryParse(result.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(result.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                return null;

            var factory = new GeometryFactory(new PrecisionModel(), 4326);
            var point = factory.CreatePoint(new Coordinate(lon, lat));
            point.SRID = 4326;
            return point;
        }
        catch
        {
            return null;
        }
    }

    private record NominatimResult(string Lat, string Lon, string? DisplayName);
}
