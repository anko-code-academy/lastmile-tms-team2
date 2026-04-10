namespace LastMile.TMS.Infrastructure.Options;

public sealed class MapboxOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string GeocodingBaseUrl { get; set; } = "https://api.mapbox.com";
    public string NavigationBaseUrl { get; set; } = "https://api.mapbox.com";
}
