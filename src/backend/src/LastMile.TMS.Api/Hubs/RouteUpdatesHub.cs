using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Abstractions;

namespace LastMile.TMS.Api.Hubs;

[Authorize(Roles = "Driver")]
public sealed class RouteUpdatesHub : Hub
{
    public static string GetGroupName(Guid driverUserId) => $"driver-route:{driverUserId:D}";

    public Task SubscribeToMyRoutes()
    {
        var subject = Context.User?.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var driverUserId))
        {
            throw new InvalidOperationException("Authenticated driver identifier is missing.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(driverUserId));
    }

    public Task UnsubscribeFromMyRoutes()
    {
        var subject = Context.User?.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (!Guid.TryParse(subject, out var driverUserId))
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(driverUserId));
    }
}
