using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Reads;

namespace LastMile.TMS.Api.GraphQL.Parcels;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ParcelQuery
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    public IQueryable<ParcelOptionDto> GetParcelsForRouteCreation(
        [Service] IParcelReadService readService = null!) =>
        readService.GetParcelsForRouteCreation();
}
