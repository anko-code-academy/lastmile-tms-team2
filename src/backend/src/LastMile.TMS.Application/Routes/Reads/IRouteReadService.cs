using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Application.Routes.Reads;

public interface IRouteReadService
{
    IQueryable<Route> GetRoutes();
    IQueryable<Route> GetRoutesForDriverUser(string? userId);
}
