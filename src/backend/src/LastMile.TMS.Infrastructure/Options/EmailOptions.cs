namespace LastMile.TMS.Infrastructure.Options;

public sealed class EmailOptions
{
    public string FromEmail { get; set; } = "noreply@lastmile.com";

    public string FromName { get; set; } = "Last Mile TMS";

    public string SendGridApiKey { get; set; } = string.Empty;

    public bool DisableDelivery { get; set; }
}
