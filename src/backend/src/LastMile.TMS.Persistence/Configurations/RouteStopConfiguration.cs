using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("RouteStops");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sequence)
            .IsRequired();

        builder.Property(x => x.RecipientLabel)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(x => x.Street1)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Street2)
            .HasMaxLength(200);

        builder.Property(x => x.City)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.State)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.PostalCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CountryCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.StopLocation)
            .HasColumnType("geometry(Point,4326)")
            .IsRequired();

        builder.HasIndex(x => new { x.RouteId, x.Sequence })
            .IsUnique();

        builder.HasMany(x => x.Parcels)
            .WithMany()
            .UsingEntity("RouteStopParcels");
    }
}
