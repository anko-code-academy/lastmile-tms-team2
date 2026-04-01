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
}
