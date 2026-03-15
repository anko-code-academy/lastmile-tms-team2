using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class ParcelConfiguration : IEntityTypeConfiguration<Parcel>
{
    public void Configure(EntityTypeBuilder<Parcel> builder)
    {
        builder.ToTable("Parcels");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TrackingNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Description)            
            .HasMaxLength(1000);

        builder.Property(p => p.ServiceType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.Weight)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.WeightUnit)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.Length)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.Width)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.Height)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.DimensionUnit)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.DeclaredValue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(p => p.EstimatedDeliveryDate)
            .IsRequired();

        builder.Property(p => p.DeliveryAttempts)
            .HasDefaultValue(0);

        builder.Property(p => p.ParcelType)
            .HasMaxLength(50);

        builder.HasIndex(p => p.TrackingNumber)
            .IsUnique();

        builder.HasIndex(p => p.Status);

        builder.HasIndex(p => p.EstimatedDeliveryDate);

        builder.HasOne(p => p.ShipperAddress)
            .WithMany(a => a.ShipperParcels)
            .HasForeignKey(p => p.ShipperAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RecipientAddress)
            .WithMany(a => a.RecipientParcels)
            .HasForeignKey(p => p.RecipientAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Zone)
            .WithMany(z => z.Parcels)
            .HasForeignKey(p => p.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
