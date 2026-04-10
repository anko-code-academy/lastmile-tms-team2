using System.Text.Json;
using System.Text.RegularExpressions;
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
    private const string DefaultGeocodingBaseUrl = "https://api.mapbox.com/search/geocode/v6";
    private const string PreferredFeatureTypes = "address,street,secondary_address";
    private static readonly Regex MultiWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex AddressNumberRegex = new(@"(?<!\d)(\d+[A-Za-z\-]*)\b", RegexOptions.Compiled);
    private static readonly Regex LeadingUnitSegmentRegex = new(
        @"^(apt|apartment|suite|ste|unit|level|lvl|floor|fl|room|rm|#)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InlineUnitRegex = new(
        @"\b(?:apt|apartment|suite|ste|unit|level|lvl|floor|fl|room|rm)\b.*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly IReadOnlyList<(Regex Regex, string Replacement)> StreetSuffixNormalizers =
    [
        (new Regex(@"\bRd\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Road"),
        (new Regex(@"\bSt\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Street"),
        (new Regex(@"\bAve\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Avenue"),
        (new Regex(@"\bBlvd\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Boulevard"),
        (new Regex(@"\bDr\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Drive"),
        (new Regex(@"\bLn\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Lane"),
        (new Regex(@"\bCt\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Court"),
        (new Regex(@"\bPl\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Place"),
        (new Regex(@"\bTer\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Terrace"),
        (new Regex(@"\bPde\b\.?", RegexOptions.Compiled | RegexOptions.IgnoreCase), "Parade"),
    ];
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

        foreach (var query in BuildQueryVariants(address))
        {
            var feature = await FindBestFeatureAsync(query, cancellationToken);
            if (feature is null)
            {
                continue;
            }

            if (TryGetRoutablePoint(feature.Value, out var routablePoint))
            {
                return routablePoint;
            }

            if (TryGetGeometryPoint(feature.Value, out var geometryPoint))
            {
                return geometryPoint;
            }
        }

        return null;
    }

    private async Task<JsonElement?> FindBestFeatureAsync(string query, CancellationToken cancellationToken)
    {
        foreach (var requestUri in BuildForwardGeocodingUris(query))
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("features", out var features)
                || features.ValueKind != JsonValueKind.Array
                || features.GetArrayLength() == 0)
            {
                continue;
            }

            JsonElement? bestFeature = null;
            var bestScore = int.MinValue;

            foreach (var feature in features.EnumerateArray())
            {
                var score = ScoreFeature(query, feature);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestFeature = feature.Clone();
            }

            if (bestFeature is not null)
            {
                return bestFeature;
            }
        }

        return null;
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

    private static bool TryGetGeometryPoint(JsonElement feature, out Point? point)
    {
        point = null;
        if (!feature.TryGetProperty("geometry", out var geometry)
            || !geometry.TryGetProperty("coordinates", out var coordinates)
            || coordinates.ValueKind != JsonValueKind.Array
            || coordinates.GetArrayLength() < 2)
        {
            return false;
        }

        point = CreatePoint(coordinates[0].GetDouble(), coordinates[1].GetDouble());
        return true;
    }

    private IEnumerable<Uri> BuildForwardGeocodingUris(string query)
    {
        yield return BuildForwardGeocodingUri(query, PreferredFeatureTypes);
        yield return BuildForwardGeocodingUri(query, featureTypes: null);
    }

    private Uri BuildForwardGeocodingUri(string query, string? featureTypes)
    {
        var builder = new UriBuilder($"{ResolveGeocodingBaseUrl()}/forward");
        var queryParameters = new List<string>
        {
            $"q={Uri.EscapeDataString(query)}",
            "limit=10",
            "autocomplete=false",
            "permanent=true",
            "language=en",
            $"access_token={Uri.EscapeDataString(_options.AccessToken)}",
        };

        if (!string.IsNullOrWhiteSpace(featureTypes))
        {
            queryParameters.Add($"types={Uri.EscapeDataString(featureTypes)}");
        }

        builder.Query = string.Join("&", queryParameters);

        return builder.Uri;
    }

    private string ResolveGeocodingBaseUrl()
    {
        const string geocodingPath = "/search/geocode/v6";

        var configuredBaseUrl = string.IsNullOrWhiteSpace(_options.GeocodingBaseUrl)
            ? DefaultGeocodingBaseUrl
            : _options.GeocodingBaseUrl.Trim();

        configuredBaseUrl = configuredBaseUrl.TrimEnd('/');

        var geocodingPathIndex = configuredBaseUrl.IndexOf(geocodingPath, StringComparison.OrdinalIgnoreCase);
        if (geocodingPathIndex >= 0)
        {
            return configuredBaseUrl[..(geocodingPathIndex + geocodingPath.Length)];
        }

        return $"{configuredBaseUrl}{geocodingPath}";
    }

    private static IReadOnlyList<string> BuildQueryVariants(string address)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var variants = new List<string>();

        void AddVariant(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            var normalized = NormalizeWhitespace(candidate);
            if (normalized.Length == 0 || !seen.Add(normalized))
            {
                return;
            }

            variants.Add(normalized);
        }

        var normalizedAddress = NormalizeWhitespace(address);
        AddVariant(normalizedAddress);

        var withoutUnit = StripUnitDetails(normalizedAddress);
        AddVariant(withoutUnit);

        var expandedAddress = ExpandStreetSuffixes(normalizedAddress);
        AddVariant(expandedAddress);
        AddVariant(StripUnitDetails(expandedAddress));

        return variants;
    }

    private static int ScoreFeature(string query, JsonElement feature)
    {
        if (!TryGetProperty(feature, "properties", out var properties)
            || (!TryGetRoutablePoint(feature, out _) && !TryGetGeometryPoint(feature, out _)))
        {
            return int.MinValue;
        }

        var score = 0;
        score += GetString(properties, "feature_type")?.ToLowerInvariant() switch
        {
            "address" => 600,
            "secondary_address" => 560,
            "street" => 250,
            "block" => 150,
            _ => 0,
        };

        score += GetString(properties, "coordinates", "accuracy")?.ToLowerInvariant() switch
        {
            "rooftop" => 160,
            "parcel" => 140,
            "interpolated" => 120,
            "street" => 70,
            "proximate" => 40,
            _ => 0,
        };

        if (TryGetRoutablePoint(feature, out _))
        {
            score += 90;
        }

        var queryAddressNumber = ExtractAddressNumber(query);
        var candidateAddressNumber =
            GetString(properties, "context", "address", "address_number")
            ?? GetString(properties, "context", "address_number", "name");

        if (!string.IsNullOrWhiteSpace(queryAddressNumber))
        {
            score += string.Equals(queryAddressNumber, candidateAddressNumber, StringComparison.OrdinalIgnoreCase)
                ? 160
                : string.IsNullOrWhiteSpace(candidateAddressNumber)
                    ? -40
                    : -120;
        }

        var queryStreet = ExtractStreetName(query);
        var candidateStreet =
            GetString(properties, "context", "address", "street_name")
            ?? GetString(properties, "context", "street", "name")
            ?? GetString(properties, "address")
            ?? GetString(properties, "name");

        if (!string.IsNullOrWhiteSpace(queryStreet) && !string.IsNullOrWhiteSpace(candidateStreet))
        {
            var normalizedQueryStreet = NormalizeForComparison(queryStreet);
            var normalizedCandidateStreet = NormalizeForComparison(candidateStreet);

            if (normalizedQueryStreet == normalizedCandidateStreet)
            {
                score += 100;
            }
            else if (normalizedCandidateStreet.Contains(normalizedQueryStreet, StringComparison.Ordinal)
                || normalizedQueryStreet.Contains(normalizedCandidateStreet, StringComparison.Ordinal))
            {
                score += 40;
            }
        }

        if (TryGetProperty(properties, "match_code", out var matchCode))
        {
            score += ScoreMatchCodeComponent(matchCode, "address_number", matched: 120, plausible: 80, inferred: 30, unmatched: -120);
            score += ScoreMatchCodeComponent(matchCode, "street", matched: 100, plausible: 70, inferred: 20, unmatched: -80);
            score += ScoreMatchCodeComponent(matchCode, "postcode", matched: 50, plausible: 25, inferred: 10, unmatched: -30);
            score += ScoreMatchCodeComponent(matchCode, "place", matched: 35, plausible: 15, inferred: 5, unmatched: -20);
            score += ScoreMatchCodeComponent(matchCode, "region", matched: 30, plausible: 10, inferred: 5, unmatched: -15);
            score += ScoreMatchCodeComponent(matchCode, "country", matched: 20, plausible: 10, inferred: 5, unmatched: -10);

            score += GetString(matchCode, "confidence")?.ToLowerInvariant() switch
            {
                "exact" => 120,
                "high" => 90,
                "medium" => 50,
                "low" => 20,
                _ => 0,
            };
        }

        var normalizedQuery = NormalizeForComparison(query);
        var normalizedFullAddress = NormalizeForComparison(GetString(properties, "full_address") ?? string.Empty);
        if (normalizedFullAddress.Length > 0 && normalizedFullAddress.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            score += 25;
        }

        return score;
    }

    private static int ScoreMatchCodeComponent(
        JsonElement matchCode,
        string propertyName,
        int matched,
        int plausible,
        int inferred,
        int unmatched)
    {
        return GetString(matchCode, propertyName)?.ToLowerInvariant() switch
        {
            "matched" => matched,
            "plausible" => plausible,
            "inferred" => inferred,
            "unmatched" => unmatched,
            _ => 0,
        };
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        property = default;
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out property);
    }

    private static bool TryGetNestedProperty(JsonElement element, string[] path, out JsonElement property)
    {
        property = element;
        foreach (var segment in path)
        {
            if (!TryGetProperty(property, segment, out property))
            {
                property = default;
                return false;
            }
        }

        return true;
    }

    private static string? GetString(JsonElement element, params string[] path)
    {
        if (!TryGetNestedProperty(element, path, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string NormalizeWhitespace(string value) =>
        MultiWhitespaceRegex.Replace(value, " ").Trim().Trim(',');

    private static string StripUnitDetails(string address)
    {
        var segments = address
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (segments.Count == 0)
        {
            return string.Empty;
        }

        segments[0] = NormalizeWhitespace(InlineUnitRegex.Replace(segments[0], string.Empty));

        var filteredSegments = segments
            .Where((segment, index) => index == 0 || !LeadingUnitSegmentRegex.IsMatch(segment))
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToList();

        return string.Join(", ", filteredSegments);
    }

    private static string ExpandStreetSuffixes(string address)
    {
        var expanded = address;
        foreach (var (regex, replacement) in StreetSuffixNormalizers)
        {
            expanded = regex.Replace(expanded, replacement);
        }

        return NormalizeWhitespace(expanded);
    }

    private static string NormalizeForComparison(string value)
    {
        var expanded = ExpandStreetSuffixes(value).ToUpperInvariant();
        var normalizedCharacters = expanded
            .Select(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) ? character : ' ')
            .ToArray();

        return NormalizeWhitespace(new string(normalizedCharacters));
    }

    private static string? ExtractAddressNumber(string address)
    {
        var firstSegment = address.Split(',', 2, StringSplitOptions.TrimEntries)[0];
        var match = AddressNumberRegex.Match(firstSegment);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string ExtractStreetName(string address)
    {
        var firstSegment = address.Split(',', 2, StringSplitOptions.TrimEntries)[0];
        var withoutInlineUnit = InlineUnitRegex.Replace(firstSegment, string.Empty);
        var withoutAddressNumber = AddressNumberRegex.Replace(withoutInlineUnit, string.Empty, 1);
        return NormalizeWhitespace(withoutAddressNumber);
    }

    private static Point CreatePoint(double longitude, double latitude)
    {
        var point = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        point.SRID = 4326;
        return point;
    }
}
