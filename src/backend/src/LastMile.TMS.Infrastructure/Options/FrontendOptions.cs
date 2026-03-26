namespace LastMile.TMS.Infrastructure.Options;

public sealed class FrontendOptions
{
    public string BaseUrl { get; set; } = "http://localhost";

    public string[] AllowedOrigins { get; set; } = ["http://localhost", "http://localhost:3000"];
}
