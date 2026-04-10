using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class CompleteRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IRequestHandler<CompleteRouteCommand, Route?>
{
    public async Task<Route?> Handle(CompleteRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .Include(candidate => candidate.Vehicle)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (route is null)
        {
            return null;
        }

        if (route.Status != RouteStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress routes can be completed.");
        }

        if (request.Dto.EndMileage < route.StartMileage)
        {
            throw new InvalidOperationException("End mileage cannot be lower than the route start mileage.");
        }

        route.Status = RouteStatus.Completed;
        route.EndMileage = request.Dto.EndMileage;
        route.EndDate = DateTimeOffset.UtcNow;
        route.LastModifiedAt = route.EndDate;
        route.LastModifiedBy = currentUser.UserName ?? currentUser.UserId;

        var vehicleHasOtherActiveRoutes = await dbContext.Routes
            .AsNoTracking()
            .AnyAsync(
                candidate => candidate.Id != route.Id
                    && candidate.VehicleId == route.VehicleId
                    && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(candidate.Status),
                cancellationToken);

        if (!vehicleHasOtherActiveRoutes)
        {
            route.Vehicle.Status = VehicleStatus.Available;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return route;
    }
}
