using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Queries;
using LastMile.TMS.Application.Parcels.Reads;
using LastMile.TMS.Domain.Entities;
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

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    public IQueryable<Parcel> GetParcelsForRouteCreation(
        [Service] IParcelReadService readService = null!) =>
        readService.GetParcelsForRouteCreation();

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    [UseFiltering(typeof(ParcelFilterInputType))]
    [UseSorting(typeof(ParcelSortInputType))]
    public IQueryable<ParcelDto> GetRegisteredParcels(
        [Service] IParcelReadService readService = null!) =>
        readService.GetRegisteredParcels();

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
}
