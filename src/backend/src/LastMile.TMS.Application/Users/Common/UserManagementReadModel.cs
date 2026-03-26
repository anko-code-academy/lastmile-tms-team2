using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Users.Common;

internal sealed record UserManagementUserRecord(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    bool IsActive,
    bool IsSystemAdmin,
    Guid? DepotId,
    string? DepotName,
    Guid? ZoneId,
    string? ZoneName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);

internal sealed record UserManagementRoleRecord(
    Guid UserId,
    string RoleName);

internal static class UserManagementReadModel
{
    public static DbContext AsDbContext(IAppDbContext dbContext) =>
        dbContext as DbContext
        ?? throw new InvalidOperationException("The application DbContext must inherit from Entity Framework DbContext.");

    public static IQueryable<ApplicationUser> UserEntities(IAppDbContext dbContext) =>
        dbContext.Users.AsNoTracking();

    public static IQueryable<UserManagementRoleRecord> UserRoles(IAppDbContext dbContext)
    {
        var entityFrameworkContext = AsDbContext(dbContext);
        var userRoles = entityFrameworkContext.Set<IdentityUserRole<Guid>>().AsNoTracking();

        return from userRole in userRoles
               join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
               where role.Name != null
               select new UserManagementRoleRecord(userRole.UserId, role.Name!);
    }

    public static IQueryable<Guid> UserIdsInRole(IAppDbContext dbContext, string roleName)
    {
        var entityFrameworkContext = AsDbContext(dbContext);
        var userRoles = entityFrameworkContext.Set<IdentityUserRole<Guid>>().AsNoTracking();

        return from userRole in userRoles
               join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
               where role.Name == roleName
               select userRole.UserId;
    }

    public static Task<List<UserManagementRoleRecord>> GetUserRolesAsync(
        IAppDbContext dbContext,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return Task.FromResult(new List<UserManagementRoleRecord>());
        }

        var entityFrameworkContext = AsDbContext(dbContext);
        var userRoles = entityFrameworkContext.Set<IdentityUserRole<Guid>>().AsNoTracking();

        return (from userRole in userRoles
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where role.Name != null && userIds.Contains(userRole.UserId)
                select new UserManagementRoleRecord(userRole.UserId, role.Name!))
            .ToListAsync(cancellationToken);
    }

    public static Task<string?> GetUserRoleNameAsync(
        IAppDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var entityFrameworkContext = AsDbContext(dbContext);
        var userRoles = entityFrameworkContext.Set<IdentityUserRole<Guid>>().AsNoTracking();

        return (from userRole in userRoles
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == userId && role.Name != null
                select role.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static IQueryable<UserManagementUserRecord> ProjectUsers(
        IQueryable<ApplicationUser> users) =>
        users
            .Select(user => new UserManagementUserRecord(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email!,
                user.PhoneNumber,
                user.IsActive,
                user.IsSystemAdmin,
                user.DepotId,
                user.Depot != null ? user.Depot.Name : null,
                user.ZoneId,
                user.Zone != null ? user.Zone.Name : null,
                user.CreatedAt,
                user.LastModifiedAt));

    public static async Task<UserManagementUserDto> GetUserAsync(
        IAppDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await ProjectUsers(
            UserEntities(dbContext)
                .Where(x => x.Id == userId))
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var roleName = await GetUserRoleNameAsync(dbContext, userId, cancellationToken);

        return ToDto(user, roleName);
    }

    public static UserManagementUserDto ToDto(
        UserManagementUserRecord user,
        string? roleName) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Email,
            user.Phone,
            UserManagementRoleHelper.Parse(roleName),
            user.IsActive,
            user.IsSystemAdmin,
            user.DepotId,
            user.DepotName,
            user.ZoneId,
            user.ZoneName,
            user.CreatedAt,
            user.LastModifiedAt);
}
