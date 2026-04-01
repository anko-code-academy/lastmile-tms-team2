using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using LastMile.TMS.Application.Routes.Reads;
using RouteEntity = LastMile.TMS.Domain.Entities.Route;

namespace LastMile.TMS.Api.GraphQL.Routes;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class RouteQueries
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    [UseFiltering(typeof(RouteFilterInputType))]
    [UseSorting(typeof(RouteSortInputType))]
    public IQueryable<RouteEntity> GetRoutes(
        [Service] IRouteReadService readService = null!) =>
        readService.GetRoutes();
}
