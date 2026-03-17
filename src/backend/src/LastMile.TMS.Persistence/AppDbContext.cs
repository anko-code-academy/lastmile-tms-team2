using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Persistence;


public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IAppDbContext
{
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Depot> Depots => Set<Depot>();
    public DbSet<OperatingHours> DepotOperatingHours => Set<OperatingHours>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverAvailability> DriverAvailabilities => Set<DriverAvailability>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
