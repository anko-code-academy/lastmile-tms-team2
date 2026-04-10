using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.StagingArea)
            .IsRequired();

        builder.Property(x => x.StartMileage)
            .IsRequired();

        builder.Property(x => x.PlannedDistanceMeters)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.PlannedDurationSeconds)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.PlannedPath)
            .HasColumnType("geometry(LineString,4326)");

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Zone)
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Parcels)
            .WithMany()
            .UsingEntity("RouteParcels");

        builder.HasMany(x => x.Stops)
            .WithOne(x => x.Route)
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AssignmentAuditTrail)
            .WithOne(x => x.Route)
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
