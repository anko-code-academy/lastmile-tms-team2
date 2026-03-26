using LastMile.TMS.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class FrontendBaseUrlResolver(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontendOptions> frontendOptions)
{
    public string Resolve() =>
        TryResolveCurrentRequestBaseUrl() ?? GetConfiguredBaseUrl();

    private string? TryResolveCurrentRequestBaseUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return null;
        }

        var candidates = new[]
        {
            request.Headers.Origin.ToString(),
            request.Headers.Referer.ToString(),
            $"{request.Scheme}://{request.Host}"
        };

        var allowedOrigins = GetAllowedOrigins();

        foreach (var candidate in candidates)
        {
            var normalizedCandidate = NormalizeBaseUrl(candidate);
            if (normalizedCandidate is not null && allowedOrigins.Contains(normalizedCandidate))
            {
                return normalizedCandidate;
            }
        }

        return null;
    }

    private HashSet<string> GetAllowedOrigins()
    {
        var allowedOrigins = frontendOptions.Value.AllowedOrigins
            .Select(NormalizeBaseUrl)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        allowedOrigins.Add(GetConfiguredBaseUrl());
        return allowedOrigins;
    }

    private string GetConfiguredBaseUrl() =>
        NormalizeBaseUrl(frontendOptions.Value.BaseUrl)
        ?? throw new InvalidOperationException("Frontend:BaseUrl must be a valid absolute HTTP or HTTPS URL.");

    private static string? NormalizeBaseUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.GetLeftPart(UriPartial.Authority).TrimEnd('/')
            : null;
    }
}
