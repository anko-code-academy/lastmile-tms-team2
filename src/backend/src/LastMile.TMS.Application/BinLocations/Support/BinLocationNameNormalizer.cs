namespace LastMile.TMS.Application.BinLocations.Support;

internal static class BinLocationNameNormalizer
{
    public static string Normalize(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Trim();
    }

    public static string NormalizeForUniqueness(string name) =>
        Normalize(name).ToUpperInvariant();
}
