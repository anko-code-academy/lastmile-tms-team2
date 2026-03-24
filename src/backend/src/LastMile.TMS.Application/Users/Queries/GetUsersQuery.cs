using FluentValidation;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Users.Queries;

public sealed record GetUsersQuery(
    string? Search,
    PredefinedRole? Role,
    bool? IsActive,
    Guid? DepotId,
    Guid? ZoneId,
    int Skip = 0,
    int Take = 20) : IRequest<UserManagementUsersResultDto>;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
    }
}

public sealed class GetUsersQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetUsersQuery, UserManagementUsersResultDto>
{
    public async Task<UserManagementUsersResultDto> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = UserManagementReadModel.UserEntities(dbContext);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(x =>
                (x.FirstName + " " + x.LastName).ToUpper().Contains(search) ||
                x.Email!.ToUpper().Contains(search) ||
                (x.PhoneNumber ?? string.Empty).ToUpper().Contains(search));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        if (request.DepotId.HasValue)
        {
            query = query.Where(x => x.DepotId == request.DepotId.Value);
        }

        if (request.ZoneId.HasValue)
        {
            query = query.Where(x => x.ZoneId == request.ZoneId.Value);
        }

        if (request.Role.HasValue)
        {
            var roleName = UserManagementRoleHelper.ToIdentityRoleName(request.Role.Value);
            var matchingUserIds = UserManagementReadModel.UserIdsInRole(dbContext, roleName);

            query = query.Where(x => matchingUserIds.Contains(x.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await UserManagementReadModel.ProjectUsers(
            query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Skip(request.Skip)
                .Take(request.Take))
            .ToListAsync(cancellationToken);

        var userIds = users.Select(x => x.Id).ToList();
        var roles = await UserManagementReadModel.GetUserRolesAsync(
            dbContext,
            userIds,
            cancellationToken);

        var roleLookup = roles
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.Select(role => role.RoleName).FirstOrDefault());

        var items = users
            .Select(user => UserManagementReadModel.ToDto(
                user,
                roleLookup.TryGetValue(user.Id, out var roleName) ? roleName : null))
            .ToList();

        return new UserManagementUsersResultDto(totalCount, items);
    }
}
