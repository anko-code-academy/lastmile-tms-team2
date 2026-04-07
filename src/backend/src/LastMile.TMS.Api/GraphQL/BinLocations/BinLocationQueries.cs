using HotChocolate;
using HotChocolate.Authorization;
using LastMile.TMS.Application.BinLocations.DTOs;
using LastMile.TMS.Application.BinLocations.Queries;
using MediatR;

namespace LastMile.TMS.Api.GraphQL.BinLocations;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class BinLocationQueries
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<DepotStorageLayoutDto?> DepotStorageLayout(
        Guid depotId,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new GetDepotStorageLayoutQuery(depotId), cancellationToken);
}
