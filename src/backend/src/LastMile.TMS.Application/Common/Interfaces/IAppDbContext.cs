using LastMile.TMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<IdentityUserRole<Guid>> UserRoles { get; }
    DbSet<Depot> Depots { get; }
    DbSet<Zone> Zones { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Route> Routes { get; }
    DbSet<Address> Addresses { get; }
    DbSet<OperatingHours> DepotOperatingHours { get; }
    DbSet<DriverAvailability> DriverAvailabilities { get; }
    DbSet<Parcel> Parcels { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
