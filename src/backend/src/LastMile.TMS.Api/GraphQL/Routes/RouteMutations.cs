using HotChocolate;
using HotChocolate.Authorization;
using LastMile.TMS.Application.Routes.Commands;
using MediatR;
using RouteEntity = LastMile.TMS.Domain.Entities.Route;

namespace LastMile.TMS.Api.GraphQL.Routes;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class RouteMutations
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public Task<RouteEntity> CreateRoute(
        CreateRouteInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new CreateRouteCommand(input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public Task<RouteEntity?> UpdateRouteAssignment(
        Guid id,
        UpdateRouteAssignmentInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new UpdateRouteAssignmentCommand(id, input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public Task<RouteEntity?> CancelRoute(
        Guid id,
        CancelRouteInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new CancelRouteCommand(id, input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public Task<RouteEntity?> DispatchRoute(
        Guid id,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new DispatchRouteCommand(id), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public Task<RouteEntity?> StartRoute(
        Guid id,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new StartRouteCommand(id), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public Task<RouteEntity?> CompleteRoute(
        Guid id,
        CompleteRouteInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new CompleteRouteCommand(id, input.ToDto()), cancellationToken);
}
