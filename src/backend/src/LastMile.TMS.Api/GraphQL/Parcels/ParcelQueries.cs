using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Reads;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Parcels;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ParcelQueries
{
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
}
