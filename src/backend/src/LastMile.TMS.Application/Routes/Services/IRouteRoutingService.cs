using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Routes.Services;

public interface IRouteRoutingService
{
    Task<RouteMatrixResult> GetMatrixAsync(
        IReadOnlyList<Point> coordinates,
        CancellationToken cancellationToken = default);

    Task<RouteDirectionsResult> GetDirectionsAsync(
        IReadOnlyList<Point> coordinates,
        CancellationToken cancellationToken = default);
}

public sealed record RouteMatrixResult(
    IReadOnlyList<IReadOnlyList<double?>> Durations,
    IReadOnlyList<IReadOnlyList<double?>> Distances);

public sealed record RouteDirectionsResult(
    int DistanceMeters,
    int DurationSeconds,
    IReadOnlyList<RouteCoordinateResult> Path);

public sealed record RouteCoordinateResult(double Longitude, double Latitude);
