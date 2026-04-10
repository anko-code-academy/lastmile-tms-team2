using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Application.Routes.Queries;
using LastMile.TMS.Application.Routes.Reads;
using MediatR;
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

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<RouteEntity> GetRoute(
        Guid id,
        [Service] IRouteReadService readService = null!) =>
        readService.GetRoutes().Where(route => route.Id == id);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public Task<RouteAssignmentCandidatesDto> GetRouteAssignmentCandidates(
        DateTimeOffset serviceDate,
        Guid zoneId,
        Guid? routeId,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(
            new GetRouteAssignmentCandidatesQuery(serviceDate, zoneId, routeId),
            cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public Task<RoutePlanPreviewDto> GetRoutePlanPreview(
        RoutePlanPreviewInput input,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(
            new GetRoutePlanPreviewQuery(input.ToDto()),
            cancellationToken);
}
