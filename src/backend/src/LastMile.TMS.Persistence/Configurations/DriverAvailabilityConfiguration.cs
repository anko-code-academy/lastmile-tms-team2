using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class DriverAvailabilityConfiguration : IEntityTypeConfiguration<DriverAvailability>
{
    public void Configure(EntityTypeBuilder<DriverAvailability> builder)
    {
        builder.ToTable("DriverAvailabilities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .IsRequired();

        builder.Property(x => x.IsAvailable)
            .IsRequired();

        builder.Property(x => x.ShiftStart);
        builder.Property(x => x.ShiftEnd);

        builder.HasIndex(x => new { x.DriverId, x.DayOfWeek })
            .IsUnique();
    }
}
