using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Common;
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
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverAvailability> DriverAvailabilities => Set<DriverAvailability>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Parcel> Parcels => Set<Parcel>();
    public DbSet<DeliveryConfirmation> DeliveryConfirmations => Set<DeliveryConfirmation>();
    public DbSet<ParcelChangeHistoryEntry> ParcelChangeHistoryEntries => Set<ParcelChangeHistoryEntry>();
    public DbSet<ParcelImport> ParcelImports => Set<ParcelImport>();
    public DbSet<ParcelImportRowFailure> ParcelImportRowFailures => Set<ParcelImportRowFailure>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
