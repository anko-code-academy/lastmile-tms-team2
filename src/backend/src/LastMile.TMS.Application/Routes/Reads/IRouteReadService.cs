using LastMile.TMS.Application.Routes.DTOs;

namespace LastMile.TMS.Application.Routes.Reads;

public interface IRouteReadService
{
    IQueryable<RouteDto> GetRoutes();
}
