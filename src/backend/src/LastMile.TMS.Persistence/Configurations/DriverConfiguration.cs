using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Phone)
            .HasMaxLength(20);

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.LicenseNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.LicenseExpiryDate)
            .IsRequired();

        builder.Property(x => x.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired();

        // Driver → Zone (many-to-one)
        builder.HasOne(x => x.Zone)
            .WithMany(x => x.Drivers)
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        // Driver → Depot (many-to-one)
        builder.HasOne(x => x.Depot)
            .WithMany(x => x.Drivers)
            .HasForeignKey(x => x.DepotId)
            .OnDelete(DeleteBehavior.Restrict);

        // Driver → User (one-to-one)
        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Driver>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Driver → AvailabilitySchedule (one-to-many)
        builder.HasMany(x => x.AvailabilitySchedule)
            .WithOne(x => x.Driver)
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LicenseNumber)
            .IsUnique();

        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}
