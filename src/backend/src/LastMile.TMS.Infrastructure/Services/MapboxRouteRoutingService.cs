using System.Text.Json;
using LastMile.TMS.Application.Routes.Services;
using LastMile.TMS.Infrastructure.Options;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class MapboxRouteRoutingService(
    HttpClient httpClient,
    IOptions<MapboxOptions> options) : IRouteRoutingService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly MapboxOptions _options = options.Value;

    public async Task<RouteMatrixResult> GetMatrixAsync(
        IReadOnlyList<Point> coordinates,
        CancellationToken cancellationToken = default)
    {
        if (coordinates.Count < 2)
        {
            return new RouteMatrixResult([], []);
        }

        var url = BuildNavigationUrl(
            "directions-matrix/v1/mapbox/driving",
            coordinates,
            "annotations=duration,distance");

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Mapbox Matrix request failed: {payload}");
        }

        using var document = JsonDocument.Parse(payload);
        var code = document.RootElement.GetProperty("code").GetString();
        if (!string.Equals(code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Mapbox Matrix returned '{code}'.");
        }

        return new RouteMatrixResult(
            ReadMatrix(document.RootElement, "durations"),
            ReadMatrix(document.RootElement, "distances"));
    }

    public async Task<RouteDirectionsResult> GetDirectionsAsync(
        IReadOnlyList<Point> coordinates,
        CancellationToken cancellationToken = default)
    {
        if (coordinates.Count < 2)
        {
            return new RouteDirectionsResult(0, 0, []);
        }

        var approaches = string.Join(";", Enumerable.Repeat("curb", coordinates.Count));
        var url = BuildNavigationUrl(
            "directions/v5/mapbox/driving",
            coordinates,
            $"geometries=geojson&overview=full&steps=false&approaches={approaches}");

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Mapbox Directions request failed: {payload}");
        }

        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("routes", out var routes)
            || routes.ValueKind != JsonValueKind.Array
            || routes.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Mapbox Directions returned no routes.");
        }

        var route = routes[0];
        var path = new List<RouteCoordinateResult>();
        if (route.TryGetProperty("geometry", out var geometry)
            && geometry.TryGetProperty("coordinates", out var geometryCoordinates)
            && geometryCoordinates.ValueKind == JsonValueKind.Array)
        {
            foreach (var coordinate in geometryCoordinates.EnumerateArray())
            {
                if (coordinate.ValueKind == JsonValueKind.Array && coordinate.GetArrayLength() >= 2)
                {
                    path.Add(new RouteCoordinateResult(coordinate[0].GetDouble(), coordinate[1].GetDouble()));
                }
            }
        }

        return new RouteDirectionsResult(
            (int)Math.Round(route.GetProperty("distance").GetDouble()),
            (int)Math.Round(route.GetProperty("duration").GetDouble()),
            path);
    }

    private Uri BuildNavigationUrl(string route, IReadOnlyList<Point> coordinates, string query)
    {
        var coordinateText = string.Join(
            ";",
            coordinates.Select(point => $"{point.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{point.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

        var builder = new UriBuilder($"{_options.NavigationBaseUrl.TrimEnd('/')}/{route}/{coordinateText}");
        builder.Query = $"{query}&access_token={Uri.EscapeDataString(_options.AccessToken)}";
        return builder.Uri;
    }

    private static IReadOnlyList<IReadOnlyList<double?>> ReadMatrix(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property
            .EnumerateArray()
            .Select(row =>
                (IReadOnlyList<double?>)row
                    .EnumerateArray()
                    .Select(cell => cell.ValueKind == JsonValueKind.Null ? (double?)null : cell.GetDouble())
                    .ToList())
            .ToList();
    }
}
