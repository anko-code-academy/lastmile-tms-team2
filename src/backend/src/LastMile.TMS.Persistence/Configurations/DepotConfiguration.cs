using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class DepotConfiguration : IEntityTypeConfiguration<Depot>
{
    public void Configure(EntityTypeBuilder<Depot> builder)
    {
        builder.ToTable("Depots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne(x => x.Address)
            .WithMany()
            .HasForeignKey(x => x.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.OperatingHours)
            .WithOne(x => x.Depot)
            .HasForeignKey(x => x.DepotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Zones)
            .WithOne(x => x.Depot)
            .HasForeignKey(x => x.DepotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}