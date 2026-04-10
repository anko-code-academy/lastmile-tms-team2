namespace LastMile.TMS.Infrastructure.Options;

public sealed class NominatimOptions
{
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
    public string UserAgent { get; set; } = "LastMile.TMS/1.0";
    public string Language { get; set; } = "en";
    public string Email { get; set; } = string.Empty;
}
