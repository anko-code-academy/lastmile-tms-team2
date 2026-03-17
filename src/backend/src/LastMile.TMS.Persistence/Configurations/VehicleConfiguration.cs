using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RegistrationPlate)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.RegistrationPlate)
            .IsUnique();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.ParcelCapacity)
            .IsRequired();

        builder.Property(x => x.WeightCapacity)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasOne(x => x.Depot)
            .WithMany()
            .HasForeignKey(x => x.DepotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}