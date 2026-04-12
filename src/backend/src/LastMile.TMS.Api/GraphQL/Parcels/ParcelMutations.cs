using HotChocolate;
using HotChocolate.Authorization;
using LastMile.TMS.Application.Parcels.Commands;
using LastMile.TMS.Application.Parcels.DTOs;
using MediatR;

namespace LastMile.TMS.Api.GraphQL.Parcels;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class ParcelMutations
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelDto> RegisterParcel(
        RegisterParcelInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new RegisterParcelCommand(input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelDetailDto?> UpdateParcel(
        UpdateParcelInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new UpdateParcelCommand(input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelDetailDto?> CancelParcel(
        CancelParcelInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new CancelParcelCommand(input.Id, input.Reason), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelDto> TransitionParcelStatus(
        TransitionParcelStatusInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(input.ToDto(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelDto> ConfirmParcelSort(
        ConfirmParcelSortInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(input.ToDto(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<InboundReceivingSessionDto> StartInboundReceivingSession(
        StartInboundReceivingSessionInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(input.ToDto(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<InboundParcelScanResultDto> ScanInboundParcel(
        ScanInboundParcelInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(input.ToDto(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<InboundReceivingSessionDto> ConfirmInboundReceivingSession(
        ConfirmInboundReceivingSessionInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(input.ToDto(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<StageParcelForRouteResultDto> StageParcelForRoute(
        StageParcelForRouteInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(input.ToDto(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<LoadParcelForRouteResultDto> LoadParcelForRoute(
        LoadParcelForRouteInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(input.ToDto(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<CompleteLoadOutResultDto> CompleteLoadOut(
        CompleteLoadOutInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default) =>
        mediator.Send(input.ToDto(), cancellationToken);
}
