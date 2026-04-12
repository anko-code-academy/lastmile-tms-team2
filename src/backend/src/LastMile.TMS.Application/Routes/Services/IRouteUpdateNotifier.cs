namespace LastMile.TMS.Application.Routes.Services;

public interface IRouteUpdateNotifier
{
    Task NotifyRouteUpdatedAsync(
        RouteUpdateNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record RouteUpdateNotification(
    Guid DriverUserId,
    Guid RouteId,
    string Action,
    string TrackingNumber,
    string Reason,
    DateTimeOffset ChangedAt);
