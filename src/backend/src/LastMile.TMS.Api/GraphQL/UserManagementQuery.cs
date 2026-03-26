using HotChocolate;
using HotChocolate.Authorization;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Application.Users.Queries;
using LastMile.TMS.Domain.Enums;
using MediatR;

namespace LastMile.TMS.Api.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize(Roles = new[] { nameof(PredefinedRole.Admin) })]
public sealed class UserManagementQuery
{
    public Task<UserManagementUsersResultDto> Users(
        [Service] ISender sender,
        string? search,
        PredefinedRole? role,
        bool? isActive,
        Guid? depotId,
        Guid? zoneId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default) =>
        sender.Send(
            new GetUsersQuery(search, role, isActive, depotId, zoneId, skip, take),
            cancellationToken);

    public Task<UserManagementUserDto> User(
        Guid id,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new GetUserByIdQuery(id), cancellationToken);

    public Task<UserManagementLookupsDto> UserManagementLookups(
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new GetUserManagementLookupsQuery(), cancellationToken);
}
