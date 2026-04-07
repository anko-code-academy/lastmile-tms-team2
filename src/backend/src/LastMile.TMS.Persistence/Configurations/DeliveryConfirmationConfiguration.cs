using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class DeliveryConfirmationConfiguration : IEntityTypeConfiguration<DeliveryConfirmation>
{
    public void Configure(EntityTypeBuilder<DeliveryConfirmation> builder)
    {
        builder.ToTable("DeliveryConfirmations");

        builder.HasKey(dc => dc.Id);

        builder.Property(dc => dc.ReceivedBy)                      
            .HasMaxLength(200);

        builder.Property(dc => dc.DeliveryLocation)
            .HasMaxLength(500);

        builder.Property(dc => dc.SignatureImageKey)
            .HasMaxLength(512);

        builder.Property(dc => dc.PhotoKey)
            .HasMaxLength(512);

        builder.Property(dc => dc.SignatureImage)
            .HasColumnType("bytea");

        builder.Property(dc => dc.Photo)
            .HasColumnType("bytea");

        builder.HasOne(dc => dc.Parcel)
            .WithOne(p => p.DeliveryConfirmation)
            .HasForeignKey<DeliveryConfirmation>(dc => dc.ParcelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dc => dc.ParcelId)
            .IsUnique();
    }
}
