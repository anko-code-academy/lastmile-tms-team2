using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class StartRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IRequestHandler<StartRouteCommand, Route?>
{
    public async Task<Route?> Handle(StartRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (route is null)
        {
            return null;
        }

        if (route.Status != RouteStatus.Dispatched)
        {
            throw new InvalidOperationException("Only dispatched routes can be started.");
        }

        var now = DateTimeOffset.UtcNow;
        var actor = currentUser.UserName ?? currentUser.UserId ?? "System";

        route.Status = RouteStatus.InProgress;
        route.LastModifiedAt = now;
        route.LastModifiedBy = actor;

        await dbContext.SaveChangesAsync(cancellationToken);

        return route;
    }
}
