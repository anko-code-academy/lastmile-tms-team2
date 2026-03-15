using LastMile.TMS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Persistence;


public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Depot> Depots => Set<Depot>();
    public DbSet<OperatingHours> DepotOperatingHours => Set<OperatingHours>();
    public DbSet<Zone> Zones => Set<Zone>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
