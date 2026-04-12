using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Queries;
using LastMile.TMS.Application.Parcels.Reads;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;

namespace LastMile.TMS.Api.GraphQL.Parcels;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ParcelQueries
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelDetailDto?> GetParcel(
        Guid id,
        [Service] IParcelReadService readService = null!,
        CancellationToken cancellationToken = default) =>
        readService.GetParcelByIdAsync(id, cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelDetailDto?> GetParcelByTrackingNumber(
        string trackingNumber,
        [Service] IParcelReadService readService = null!,
        CancellationToken cancellationToken = default) =>
        readService.GetParcelByTrackingNumberAsync(trackingNumber, cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    public IQueryable<Parcel> GetParcelsForRouteCreation(
        Guid vehicleId,
        Guid driverId,
        [Service] IParcelReadService readService = null!) =>
        readService.GetParcelsForRouteCreation(vehicleId, driverId);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    [UseProjection]
    [UseFiltering(typeof(ParcelFilterInputType))]
    [UseSorting(typeof(ParcelSortInputType))]
    public IQueryable<Parcel> GetRegisteredParcels(
        string? search,
        [Service] IParcelReadService readService = null!)
        => ApplyParcelSearch(readService.GetRegisteredParcels(), search);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    [UseProjection]
    [UseFiltering(typeof(ParcelFilterInputType))]
    [UseSorting(typeof(ParcelSortInputType))]
    public IQueryable<Parcel> GetPreLoadParcels(
        string? search,
        [Service] IParcelReadService readService = null!)
        => ApplyParcelSearch(readService.GetPreLoadParcels(), search);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20, MaxPageSize = 100)]
    [UseProjection]
    [UseFiltering(typeof(ParcelFilterInputType))]
    [UseSorting(typeof(ParcelSortInputType))]
    public IQueryable<Parcel> GetPreLoadParcelsConnection(
        string? search,
        [Service] IParcelReadService readService = null!)
        => ApplyParcelSearch(readService.GetPreLoadParcels(), search);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<IReadOnlyList<ParcelImportHistoryDto>> GetParcelImports(
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetParcelImportsQuery(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelImportDetailDto?> GetParcelImport(
        Guid id,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetParcelImportQuery(id), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<ParcelSortInstructionDto?> GetParcelSortInstruction(
        string trackingNumber,
        Guid? depotId,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetParcelSortInstructionQuery(trackingNumber, depotId), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<IReadOnlyList<TrackingEventDto>> GetParcelTrackingEvents(
        Guid parcelId,
        [Service] IParcelReadService readService,
        CancellationToken cancellationToken) =>
        readService.GetTrackingEventsAsync(parcelId, cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<IReadOnlyList<InboundManifestDto>> GetOpenInboundManifests(
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetOpenInboundManifestsQuery(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<InboundReceivingSessionDto?> GetInboundReceivingSession(
        Guid sessionId,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetInboundReceivingSessionQuery(sessionId), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<IReadOnlyList<StagingRouteDto>> GetStagingRoutes(
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetStagingRoutesQuery(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<RouteStagingBoardDto?> GetRouteStagingBoard(
        Guid routeId,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetRouteStagingBoardQuery(routeId), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<IReadOnlyList<LoadOutRouteDto>> GetLoadOutRoutes(
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetLoadOutRoutesQuery(), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public Task<RouteLoadOutBoardDto?> GetRouteLoadOutBoard(
        Guid routeId,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetRouteLoadOutBoardQuery(routeId), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "WarehouseOperator" })]
    public Task<DepotParcelInventoryDashboardDto?> GetDepotParcelInventory(
        int agingThresholdMinutes,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetDepotParcelInventoryQuery(agingThresholdMinutes), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "WarehouseOperator" })]
    public Task<DepotParcelInventoryParcelConnectionDto> GetDepotParcelInventoryParcels(
        int agingThresholdMinutes,
        ParcelStatus? status,
        Guid? zoneId,
        bool agingOnly,
        int first,
        string? after,
        [Service] ISender mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(
            new GetDepotParcelInventoryParcelsQuery(
                agingThresholdMinutes,
                status,
                zoneId,
                agingOnly,
                first,
                after),
            cancellationToken);

    private static IQueryable<Parcel> ApplyParcelSearch(IQueryable<Parcel> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var pattern = search.Trim().ToUpperInvariant();
        return query.Where(p =>
            p.TrackingNumber.ToUpper().Contains(pattern) ||
            (p.RecipientAddress.ContactName ?? string.Empty).ToUpper().Contains(pattern) ||
            (p.RecipientAddress.CompanyName ?? string.Empty).ToUpper().Contains(pattern) ||
            (p.RecipientAddress.Street1 ?? string.Empty).ToUpper().Contains(pattern) ||
            (p.RecipientAddress.City ?? string.Empty).ToUpper().Contains(pattern) ||
            (p.RecipientAddress.PostalCode ?? string.Empty).ToUpper().Contains(pattern));
    }
}
