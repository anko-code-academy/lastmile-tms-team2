using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Infrastructure.Options;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class NominatimGeocodingService(
    HttpClient httpClient,
    IOptions<NominatimOptions> options) : IGeocodingService
{
    private const string DefaultBaseUrl = "https://nominatim.openstreetmap.org";
    private static readonly Regex MultiWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex AddressNumberRegex = new(@"(?<!\d)(\d+[A-Za-z\-]*)\b", RegexOptions.Compiled);
    private static readonly Regex LeadingUnitSegmentRegex = new(
        @"^(apt|apartment|suite|ste|unit|level|lvl|floor|fl|room|rm|#)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InlineUnitRegex = new(
        @"\b(?:apt|apartment|suite|ste|unit|level|lvl|floor|fl|room|rm)\b.*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CountryCodeRegex = new(@"^[A-Za-z]{2}$", RegexOptions.Compiled);
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
    private readonly NominatimOptions _options = options.Value;

    public async Task<Point?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        foreach (var query in BuildQueryVariants(address))
        {
            var result = await FindBestResultAsync(query, cancellationToken);
            if (result is null)
            {
                continue;
            }

            if (TryGetPoint(result.Value, out var point))
            {
                return point;
            }
        }

        return null;
    }

    private async Task<JsonElement?> FindBestResultAsync(string query, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildSearchUri(query));
        request.Headers.TryAddWithoutValidation("User-Agent", ResolveUserAgent());

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array
            || document.RootElement.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement? bestResult = null;
        var bestScore = double.NegativeInfinity;

        foreach (var result in document.RootElement.EnumerateArray())
        {
            var score = ScoreResult(query, result);
            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestResult = result.Clone();
        }

        return bestResult;
    }

    private Uri BuildSearchUri(string query)
    {
        var builder = new UriBuilder($"{ResolveBaseUrl()}/search");
        var queryParameters = new List<string>
        {
            $"q={Uri.EscapeDataString(query)}",
            "format=jsonv2",
            "limit=10",
            "addressdetails=1",
            "dedupe=1",
            "layer=address",
            $"accept-language={Uri.EscapeDataString(ResolveLanguage())}",
        };

        if (TryExtractCountryCode(query, out var countryCode))
        {
            queryParameters.Add($"countrycodes={Uri.EscapeDataString(countryCode)}");
        }

        if (!string.IsNullOrWhiteSpace(_options.Email))
        {
            queryParameters.Add($"email={Uri.EscapeDataString(_options.Email.Trim())}");
        }

        builder.Query = string.Join("&", queryParameters);
        return builder.Uri;
    }

    private string ResolveBaseUrl()
    {
        const string searchPath = "/search";

        var configuredBaseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? DefaultBaseUrl
            : _options.BaseUrl.Trim();

        configuredBaseUrl = configuredBaseUrl.TrimEnd('/');

        return configuredBaseUrl.EndsWith(searchPath, StringComparison.OrdinalIgnoreCase)
            ? configuredBaseUrl[..^searchPath.Length]
            : configuredBaseUrl;
    }

    private string ResolveLanguage() =>
        string.IsNullOrWhiteSpace(_options.Language)
            ? "en"
            : _options.Language.Trim();

    private string ResolveUserAgent() =>
        string.IsNullOrWhiteSpace(_options.UserAgent)
            ? "LastMile.TMS/1.0"
            : _options.UserAgent.Trim();

    private static double ScoreResult(string query, JsonElement result)
    {
        if (!TryGetPoint(result, out _))
        {
            return double.NegativeInfinity;
        }

        var score = 0d;

        score += (GetString(result, "addresstype") ?? GetString(result, "type"))?.ToLowerInvariant() switch
        {
            "house" => 260,
            "house_number" => 260,
            "building" => 220,
            "residential" => 160,
            "road" => 140,
            "street" => 140,
            "living_street" => 140,
            "postcode" => -60,
            "city" => -140,
            "town" => -140,
            "village" => -140,
            "administrative" => -160,
            _ => 0,
        };

        score += GetString(result, "category")?.ToLowerInvariant() switch
        {
            "building" => 80,
            "highway" => 40,
            "place" => -40,
            _ => 0,
        };

        if (GetDouble(result, "place_rank") is { } placeRank)
        {
            score += Math.Max(0, 120 - Math.Abs(placeRank - 30) * 8);
        }

        if (GetDouble(result, "importance") is { } importance)
        {
            score += importance * 100;
        }

        var queryAddressNumber = ExtractAddressNumber(query);
        var candidateAddressNumber =
            GetString(result, "address", "house_number")
            ?? ExtractAddressNumber(GetString(result, "display_name") ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(queryAddressNumber))
        {
            score += string.Equals(queryAddressNumber, candidateAddressNumber, StringComparison.OrdinalIgnoreCase)
                ? 200
                : string.IsNullOrWhiteSpace(candidateAddressNumber)
                    ? -40
                    : -160;
        }

        var queryStreet = ExtractStreetName(query);
        var candidateStreet = GetStreetName(result);
        if (!string.IsNullOrWhiteSpace(queryStreet) && !string.IsNullOrWhiteSpace(candidateStreet))
        {
            var normalizedQueryStreet = NormalizeForComparison(queryStreet);
            var normalizedCandidateStreet = NormalizeForComparison(candidateStreet);

            if (normalizedQueryStreet == normalizedCandidateStreet)
            {
                score += 140;
            }
            else if (normalizedCandidateStreet.Contains(normalizedQueryStreet, StringComparison.Ordinal)
                || normalizedQueryStreet.Contains(normalizedCandidateStreet, StringComparison.Ordinal))
            {
                score += 60;
            }
            else
            {
                score -= 40;
            }
        }

        if (TryExtractCountryCode(query, out var queryCountryCode))
        {
            score += string.Equals(
                queryCountryCode,
                GetString(result, "address", "country_code"),
                StringComparison.OrdinalIgnoreCase)
                ? 40
                : -20;
        }

        var normalizedQuery = NormalizeForComparison(query);
        var normalizedDisplayName = NormalizeForComparison(GetString(result, "display_name") ?? string.Empty);
        if (normalizedDisplayName.Length > 0
            && normalizedDisplayName.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            score += 30;
        }

        return score;
    }

    private static string? GetStreetName(JsonElement result) =>
        GetString(result, "address", "road")
        ?? GetString(result, "address", "pedestrian")
        ?? GetString(result, "address", "footway")
        ?? GetString(result, "address", "street")
        ?? GetString(result, "address", "residential")
        ?? GetString(result, "name");

    private static bool TryGetPoint(JsonElement result, out Point? point)
    {
        point = null;

        var longitude = GetDouble(result, "lon");
        var latitude = GetDouble(result, "lat");
        if (longitude is null || latitude is null)
        {
            return false;
        }

        point = CreatePoint(longitude.Value, latitude.Value);
        return true;
    }

    private static bool TryExtractCountryCode(string query, out string countryCode)
    {
        countryCode = string.Empty;
        var lastSegment = query
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(lastSegment) || !CountryCodeRegex.IsMatch(lastSegment))
        {
            return false;
        }

        countryCode = lastSegment.ToLowerInvariant();
        return true;
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

    private static double? GetDouble(JsonElement element, params string[] path)
    {
        if (!TryGetNestedProperty(element, path, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var numericValue))
        {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String
            && double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
        {
            return numericValue;
        }

        return null;
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
