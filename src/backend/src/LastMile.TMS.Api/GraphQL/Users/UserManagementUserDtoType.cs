using HotChocolate;
using HotChocolate.Types;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Users.Common;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Api.GraphQL.Users;

public sealed class UserManagementUserDtoType : ObjectType<UserManagementUserDto>
{
    protected override void Configure(IObjectTypeDescriptor<UserManagementUserDto> descriptor)
    {
        descriptor.Field("role")
            .Type<StringType>()
            .Resolve(ctx =>
            {
                var userId = ctx.Parent<UserManagementUserDto>().Id;
                var dbContext = ctx.Service<IAppDbContext>();

                return ctx.GroupDataLoader<Guid, string?>(
                    async (ids, ct) =>
                    {
                        var roleRecords = await dbContext.UserRoles
                            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id,
                                (ur, r) => new { ur.UserId, RoleName = r.Name })
                            .Where(x => x.RoleName != null && ids.Contains(x.UserId))
                            .GroupBy(x => x.UserId)
                            .Select(g => new { UserId = g.Key, RoleName = g.First().RoleName! })
                            .ToListAsync(ct);

                        return roleRecords.ToLookup(
                            x => x.UserId,
                            x => (string?)x.RoleName);
                    })
                    .LoadAsync(userId);
            });
    }
}
