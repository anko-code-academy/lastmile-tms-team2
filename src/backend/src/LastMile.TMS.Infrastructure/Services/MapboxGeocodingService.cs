using System.Text.Json;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Infrastructure.Options;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class MapboxGeocodingService(
    HttpClient httpClient,
    IOptions<MapboxOptions> options) : IGeocodingService
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    private readonly HttpClient _httpClient = httpClient;
    private readonly MapboxOptions _options = options.Value;

    public async Task<Point?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var url = new UriBuilder($"{_options.GeocodingBaseUrl.TrimEnd('/')}/search/geocode/v6/forward");
        url.Query =
            $"q={Uri.EscapeDataString(address)}&types=address&limit=1&autocomplete=false&permanent=true&access_token={Uri.EscapeDataString(_options.AccessToken)}";

        using var response = await _httpClient.GetAsync(url.Uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("features", out var features)
            || features.ValueKind != JsonValueKind.Array
            || features.GetArrayLength() == 0)
        {
            return null;
        }

        var feature = features[0];
        if (TryGetRoutablePoint(feature, out var routablePoint))
        {
            return routablePoint;
        }

        if (!feature.TryGetProperty("geometry", out var geometry)
            || !geometry.TryGetProperty("coordinates", out var coordinates)
            || coordinates.GetArrayLength() < 2)
        {
            return null;
        }

        return CreatePoint(coordinates[0].GetDouble(), coordinates[1].GetDouble());
    }

    private static bool TryGetRoutablePoint(JsonElement feature, out Point? point)
    {
        point = null;
        if (!feature.TryGetProperty("properties", out var properties)
            || !properties.TryGetProperty("coordinates", out var coordinates)
            || !coordinates.TryGetProperty("routable_points", out var routablePoints)
            || routablePoints.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var routablePoint in routablePoints.EnumerateArray())
        {
            if (!routablePoint.TryGetProperty("name", out var name)
                || !string.Equals(name.GetString(), "default", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!routablePoint.TryGetProperty("longitude", out var longitude)
                || !routablePoint.TryGetProperty("latitude", out var latitude))
            {
                continue;
            }

            point = CreatePoint(longitude.GetDouble(), latitude.GetDouble());
            return true;
        }

        return false;
    }

    private static Point CreatePoint(double longitude, double latitude)
    {
        var point = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        point.SRID = 4326;
        return point;
    }
}
