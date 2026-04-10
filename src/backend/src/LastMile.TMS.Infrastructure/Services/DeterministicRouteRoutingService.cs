using LastMile.TMS.Application.Routes.Services;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class DeterministicRouteRoutingService : IRouteRoutingService
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public Task<RouteMatrixResult> GetMatrixAsync(
        IReadOnlyList<Point> coordinates,
        CancellationToken cancellationToken = default)
    {
        var durations = new List<IReadOnlyList<double?>>(coordinates.Count);
        var distances = new List<IReadOnlyList<double?>>(coordinates.Count);

        for (var i = 0; i < coordinates.Count; i++)
        {
            var durationRow = new List<double?>(coordinates.Count);
            var distanceRow = new List<double?>(coordinates.Count);

            for (var j = 0; j < coordinates.Count; j++)
            {
                var distance = HaversineMeters(coordinates[i], coordinates[j]);
                distanceRow.Add(distance);
                durationRow.Add(distance / 13.89d);
            }

            durations.Add(durationRow);
            distances.Add(distanceRow);
        }

        return Task.FromResult(new RouteMatrixResult(durations, distances));
    }

    public Task<RouteDirectionsResult> GetDirectionsAsync(
        IReadOnlyList<Point> coordinates,
        CancellationToken cancellationToken = default)
    {
        var distance = 0d;
        for (var i = 0; i < coordinates.Count - 1; i++)
        {
            distance += HaversineMeters(coordinates[i], coordinates[i + 1]);
        }

        var path = coordinates
            .Select(point => new RouteCoordinateResult(point.X, point.Y))
            .ToList();

        return Task.FromResult(
            new RouteDirectionsResult(
                (int)Math.Round(distance),
                (int)Math.Round(distance / 13.89d),
                path));
    }

    private static double HaversineMeters(Point origin, Point destination)
    {
        const double EarthRadius = 6_371_000d;
        var dLat = DegreesToRadians(destination.Y - origin.Y);
        var dLon = DegreesToRadians(destination.X - origin.X);
        var lat1 = DegreesToRadians(origin.Y);
        var lat2 = DegreesToRadians(destination.Y);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2)
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadius * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
