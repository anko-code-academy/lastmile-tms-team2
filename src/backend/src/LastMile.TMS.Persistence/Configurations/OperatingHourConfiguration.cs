using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class DepotOperatingHourConfiguration : IEntityTypeConfiguration<OperatingHours>
{
    public void Configure(EntityTypeBuilder<OperatingHours> builder)
    {
        builder.ToTable("DepotOperatingHours");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .IsRequired();

        builder.Property(x => x.IsClosed)
            .IsRequired();

        builder.Property(x => x.OpenTime);
        builder.Property(x => x.ClosedTime);

        builder.HasIndex(x => new { x.DepotId, x.DayOfWeek })
            .IsUnique();
    }
}