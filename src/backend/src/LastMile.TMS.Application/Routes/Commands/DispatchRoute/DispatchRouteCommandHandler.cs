using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class DispatchRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IRequestHandler<DispatchRouteCommand, Route?>
{
    public async Task<Route?> Handle(DispatchRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (route is null)
        {
            return null;
        }

        if (route.Status != RouteStatus.Draft)
        {
            throw new InvalidOperationException("Only draft routes can be dispatched.");
        }

        route.Status = RouteStatus.Dispatched;
        route.LastModifiedAt = DateTimeOffset.UtcNow;
        route.LastModifiedBy = currentUser.UserName ?? currentUser.UserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return route;
    }
}
