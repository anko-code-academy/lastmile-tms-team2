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
    public DbSet<StorageZone> StorageZones => Set<StorageZone>();
    public DbSet<StorageAisle> StorageAisles => Set<StorageAisle>();
    public DbSet<BinLocation> BinLocations => Set<BinLocation>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverAvailability> DriverAvailabilities => Set<DriverAvailability>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<RouteAssignmentAuditEntry> RouteAssignmentAuditEntries => Set<RouteAssignmentAuditEntry>();
    public DbSet<Parcel> Parcels => Set<Parcel>();
    public DbSet<DeliveryConfirmation> DeliveryConfirmations => Set<DeliveryConfirmation>();
    public DbSet<ParcelChangeHistoryEntry> ParcelChangeHistoryEntries => Set<ParcelChangeHistoryEntry>();
    public DbSet<ParcelImport> ParcelImports => Set<ParcelImport>();
    public DbSet<ParcelImportRowFailure> ParcelImportRowFailures => Set<ParcelImportRowFailure>();
    public DbSet<InboundManifest> InboundManifests => Set<InboundManifest>();
    public DbSet<InboundManifestLine> InboundManifestLines => Set<InboundManifestLine>();
    public DbSet<InboundReceivingSession> InboundReceivingSessions => Set<InboundReceivingSession>();
    public DbSet<InboundReceivingScan> InboundReceivingScans => Set<InboundReceivingScan>();
    public DbSet<InboundReceivingException> InboundReceivingExceptions => Set<InboundReceivingException>();

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
