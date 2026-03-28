using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using LastMile.TMS.Application.Depots.Reads;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Depots;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class DepotQuery
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    [UseSorting(typeof(DepotDtoSortInputType))]
    [UseFiltering(typeof(DepotDtoFilterInputType))]
    public IQueryable<Depot> GetDepots(
        [Service] IDepotReadService readService = null!) =>
        readService.GetDepots();

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Depot> GetDepot(
        Guid id,
        [Service] IDepotReadService readService = null!) =>
        readService.GetDepotById(id);
}
