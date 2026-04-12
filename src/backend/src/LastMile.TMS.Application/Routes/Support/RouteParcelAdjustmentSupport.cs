using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Routes.Support;

internal static class RouteParcelAdjustmentSupport
{
    private const double CoordinateTolerance = 0.000001;

    public static string NormalizeRequiredReason(string? reason)
    {
        var normalized = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Adjustment reason is required.");
        }

        return normalized;
    }

    public static RouteStop? FindMatchingStop(Route route, Parcel parcel)
    {
        var recipientAddress = parcel.RecipientAddress;
        var recipientPoint = recipientAddress.GeoLocation;

        return route.Stops
            .OrderBy(stop => stop.Sequence)
            .FirstOrDefault(stop =>
                (recipientPoint is not null && PointsMatch(stop.StopLocation, recipientPoint))
                || AddressesMatch(stop, recipientAddress));
    }

    public static RouteStop CreateStop(Route route, Parcel parcel, DateTimeOffset timestamp, string actor)
    {
        var address = parcel.RecipientAddress;
        var point = address.GeoLocation
            ?? throw new InvalidOperationException("The parcel recipient address is missing geocoded coordinates.");

        return new RouteStop
        {
            Route = route,
            RouteId = route.Id,
            Sequence = route.Stops.Count == 0 ? 1 : route.Stops.Max(stop => stop.Sequence) + 1,
            RecipientLabel = BuildStopRecipientLabel([parcel]),
            Street1 = ParcelChangeSupport.NormalizeRequired(address.Street1),
            Street2 = ParcelChangeSupport.NormalizeOptional(address.Street2),
            City = ParcelChangeSupport.NormalizeRequired(address.City),
            State = ParcelChangeSupport.NormalizeRequired(address.State),
            PostalCode = ParcelChangeSupport.NormalizeRequired(address.PostalCode),
            CountryCode = ParcelChangeSupport.NormalizeRequired(address.CountryCode),
            StopLocation = point.Copy() as Point ?? point,
            CreatedAt = timestamp,
            CreatedBy = actor,
            Parcels = [parcel],
        };
    }

    public static void RefreshStop(RouteStop stop)
    {
        if (stop.Parcels.Count == 0)
        {
            return;
        }

        var anchorParcel = stop.Parcels
            .OrderBy(parcel => parcel.TrackingNumber)
            .First();
        var address = anchorParcel.RecipientAddress;
        var point = address.GeoLocation
            ?? throw new InvalidOperationException("The parcel recipient address is missing geocoded coordinates.");

        stop.RecipientLabel = BuildStopRecipientLabel(stop.Parcels);
        stop.Street1 = ParcelChangeSupport.NormalizeRequired(address.Street1);
        stop.Street2 = ParcelChangeSupport.NormalizeOptional(address.Street2);
        stop.City = ParcelChangeSupport.NormalizeRequired(address.City);
        stop.State = ParcelChangeSupport.NormalizeRequired(address.State);
        stop.PostalCode = ParcelChangeSupport.NormalizeRequired(address.PostalCode);
        stop.CountryCode = ParcelChangeSupport.NormalizeRequired(address.CountryCode);
        stop.StopLocation = point.Copy() as Point ?? point;
    }

    public static void ResequenceStops(Route route)
    {
        var nextSequence = 1;
        foreach (var stop in route.Stops.OrderBy(stop => stop.Sequence))
        {
            stop.Sequence = nextSequence++;
        }
    }

    public static string BuildAddressLine(Address address)
    {
        var parts = new[]
        {
            ParcelChangeSupport.NormalizeRequired(address.Street1),
            ParcelChangeSupport.NormalizeOptional(address.Street2),
            ParcelChangeSupport.NormalizeRequired(address.City),
            ParcelChangeSupport.NormalizeRequired(address.State),
            ParcelChangeSupport.NormalizeRequired(address.PostalCode),
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public static string BuildRecipientLabel(Parcel parcel)
    {
        var address = parcel.RecipientAddress;
        return ParcelChangeSupport.NormalizeOptional(address.ContactName)
            ?? ParcelChangeSupport.NormalizeOptional(address.CompanyName)
            ?? parcel.TrackingNumber;
    }

    private static string BuildStopRecipientLabel(IEnumerable<Parcel> parcels)
    {
        var orderedParcels = parcels
            .OrderBy(parcel => parcel.TrackingNumber)
            .ToList();
        var anchorParcel = orderedParcels[0];
        return orderedParcels.Count == 1
            ? BuildRecipientLabel(anchorParcel)
            : $"{BuildRecipientLabel(anchorParcel)} +{orderedParcels.Count - 1}";
    }

    private static bool AddressesMatch(RouteStop stop, Address address) =>
        string.Equals(
            ParcelChangeSupport.NormalizeRequired(stop.Street1),
            ParcelChangeSupport.NormalizeRequired(address.Street1),
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            ParcelChangeSupport.NormalizeOptional(stop.Street2),
            ParcelChangeSupport.NormalizeOptional(address.Street2),
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            ParcelChangeSupport.NormalizeRequired(stop.City),
            ParcelChangeSupport.NormalizeRequired(address.City),
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            ParcelChangeSupport.NormalizeRequired(stop.State),
            ParcelChangeSupport.NormalizeRequired(address.State),
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            ParcelChangeSupport.NormalizeRequired(stop.PostalCode),
            ParcelChangeSupport.NormalizeRequired(address.PostalCode),
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            ParcelChangeSupport.NormalizeRequired(stop.CountryCode),
            ParcelChangeSupport.NormalizeRequired(address.CountryCode),
            StringComparison.OrdinalIgnoreCase);

    private static bool PointsMatch(Point left, Point right) =>
        Math.Abs(left.X - right.X) <= CoordinateTolerance
        && Math.Abs(left.Y - right.Y) <= CoordinateTolerance;
}
