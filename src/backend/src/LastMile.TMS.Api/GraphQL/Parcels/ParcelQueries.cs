using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
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

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    public IQueryable<Parcel> GetParcelsForRouteCreation(
        [Service] IParcelReadService readService = null!) =>
        readService.GetParcelsForRouteCreation();

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public IQueryable<ParcelDto> GetRegisteredParcels(
        string? search = null,
        ParcelStatus[]? status = null,
        Guid? zoneId = null,
        string? parcelType = null,
        DateTimeOffset? estimatedDeliveryDateFrom = null,
        DateTimeOffset? estimatedDeliveryDateTo = null,
        [Service] IParcelReadService readService = null!)
    {
        var filter = status is not null || zoneId is not null || !string.IsNullOrWhiteSpace(parcelType)
            || estimatedDeliveryDateFrom.HasValue || estimatedDeliveryDateTo.HasValue
            ? new ParcelFilter
            {
                Status = status,
                ZoneId = zoneId,
                ParcelType = parcelType,
                EstimatedDeliveryDateFrom = estimatedDeliveryDateFrom,
                EstimatedDeliveryDateTo = estimatedDeliveryDateTo,
            }
            : null;
        return readService.GetRegisteredParcels(search, filter);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher", "WarehouseOperator" })]
    public IQueryable<ParcelDto> GetPreLoadParcels(
        string? search = null,
        ParcelStatus[]? status = null,
        Guid? zoneId = null,
        string? parcelType = null,
        DateTimeOffset? estimatedDeliveryDateFrom = null,
        DateTimeOffset? estimatedDeliveryDateTo = null,
        [Service] IParcelReadService readService = null!)
    {
        var filter = status is not null || zoneId is not null || !string.IsNullOrWhiteSpace(parcelType)
            || estimatedDeliveryDateFrom.HasValue || estimatedDeliveryDateTo.HasValue
            ? new ParcelFilter
            {
                Status = status,
                ZoneId = zoneId,
                ParcelType = parcelType,
                EstimatedDeliveryDateFrom = estimatedDeliveryDateFrom,
                EstimatedDeliveryDateTo = estimatedDeliveryDateTo,
            }
            : null;
        return readService.GetPreLoadParcels(search, filter);
    }

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
