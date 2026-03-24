using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Users.Queries;

public sealed record GetUserManagementLookupsQuery : IRequest<UserManagementLookupsDto>;

public sealed class GetUserManagementLookupsQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetUserManagementLookupsQuery, UserManagementLookupsDto>
{
    public async Task<UserManagementLookupsDto> Handle(
        GetUserManagementLookupsQuery request,
        CancellationToken cancellationToken)
    {
        var depots = await dbContext.Depots
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new UserManagementDepotOptionDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        var zones = await dbContext.Zones
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new UserManagementZoneOptionDto(x.Id, x.DepotId, x.Name))
            .ToListAsync(cancellationToken);

        var roles = Enum
            .GetValues<PredefinedRole>()
            .Select(role => new UserManagementRoleOptionDto(role, UserManagementRoleHelper.ToDisplayName(role)))
            .ToList();

        return new UserManagementLookupsDto(roles, depots, zones);
    }
}
