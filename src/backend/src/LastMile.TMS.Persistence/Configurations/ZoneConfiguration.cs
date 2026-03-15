using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable("Zones");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.Boundary)
            .IsRequired()
            .HasColumnType("geometry (polygon, 4326)");

        builder.HasOne(x => x.Depot)
            .WithMany(x => x.Zones)
            .HasForeignKey(x => x.DepotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Boundary)
            .HasMethod("GIST");
    }
}