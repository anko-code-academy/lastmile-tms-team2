namespace LastMile.TMS.Infrastructure.Options;

public sealed class TestingOptions
{
    public bool EnableTestSupport { get; set; }

    public bool DisableExternalInfrastructure { get; set; }

    public string SupportKey { get; set; } = string.Empty;
}
