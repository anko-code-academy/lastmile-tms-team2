using HotChocolate;
using HotChocolate.Authorization;
using LastMile.TMS.Application.Depots.Commands;
using LastMile.TMS.Domain.Entities;
using MediatR;

namespace LastMile.TMS.Api.GraphQL.Depots;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class DepotMutation
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public async Task<Depot> CreateDepot(
        CreateDepotInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(
            new CreateDepotCommand(
                input.Name,
                input.Address.ToDto(),
                input.OperatingHours?.ToDtos(),
                input.IsActive),
            cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public async Task<Depot?> UpdateDepot(
        Guid id,
        UpdateDepotInput input,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(
            new UpdateDepotCommand(
                id,
                input.Name,
                input.Address?.ToDto(),
                input.OperatingHours?.ToDtos(),
                input.IsActive),
            cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public async Task<bool> DeleteDepot(
        Guid id,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new DeleteDepotCommand(id), cancellationToken);
    }
}
