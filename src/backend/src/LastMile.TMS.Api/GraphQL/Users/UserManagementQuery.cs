using HotChocolate;
using HotChocolate.Authorization;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Application.Users.Queries;
using LastMile.TMS.Application.Users.Reads;
using LastMile.TMS.Domain.Enums;
using MediatR;

namespace LastMile.TMS.Api.GraphQL.Users;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize(Roles = new[] { nameof(PredefinedRole.Admin) })]
public sealed class UserManagementQuery
{
    public IQueryable<UserManagementUserDto> Users(
        string? search = null,
        bool? isActive = null,
        Guid? depotId = null,
        Guid? zoneId = null,
        [Service] IUserReadService readService = null!) =>
        readService.GetUsers(search, isActive, depotId, zoneId);

    public IQueryable<UserManagementUserDto> User(
        Guid id,
        [Service] IUserReadService readService = null!) =>
        readService.GetUsers().Where(u => u.Id == id);

    public Task<UserManagementLookupsDto> UserManagementLookups(
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new GetUserManagementLookupsQuery(), cancellationToken);
}
