using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Application.Routes.Reads;

namespace LastMile.TMS.Api.GraphQL.Routes;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class RouteQuery
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    [UseSorting]
    [UseFiltering]
    public IQueryable<RouteDto> GetRoutes(
        [Service] IRouteReadService readService = null!) =>
        readService.GetRoutes();
}
