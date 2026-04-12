using LastMile.TMS.Application.Routes.Services;
using Microsoft.AspNetCore.SignalR;

namespace LastMile.TMS.Api.Hubs;

public sealed class SignalRRouteUpdateNotifier(IHubContext<RouteUpdatesHub> hubContext)
    : IRouteUpdateNotifier
{
    public Task NotifyRouteUpdatedAsync(
        RouteUpdateNotification notification,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(RouteUpdatesHub.GetGroupName(notification.DriverUserId))
            .SendAsync("RouteUpdated", notification, cancellationToken);
}
