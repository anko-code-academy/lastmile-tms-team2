using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Reads;

public sealed class RouteReadService(IAppDbContext dbContext) : IRouteReadService
{
    public IQueryable<Route> GetRoutes() =>
        dbContext.Routes
            .AsNoTracking();

    public IQueryable<Route> GetRoutesForDriverUser(string? userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return dbContext.Routes
                .AsNoTracking()
                .Where(_ => false);
        }

        return dbContext.Routes
            .AsNoTracking()
            .Where(route => dbContext.Drivers.Any(driver =>
                driver.UserId == parsedUserId
                && driver.Id == route.DriverId));
    }
}
