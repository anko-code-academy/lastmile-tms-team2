using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<Permission> Permissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
