using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public sealed class RouteAssignmentAuditEntryConfiguration
    : IEntityTypeConfiguration<RouteAssignmentAuditEntry>
{
    public void Configure(EntityTypeBuilder<RouteAssignmentAuditEntry> builder)
    {
        builder.ToTable("RouteAssignmentAuditEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .IsRequired();

        builder.Property(x => x.NewDriverName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.PreviousDriverName)
            .HasMaxLength(200);

        builder.Property(x => x.NewVehiclePlate)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PreviousVehiclePlate)
            .HasMaxLength(50);

        builder.Property(x => x.ChangedAt)
            .IsRequired();

        builder.Property(x => x.ChangedBy)
            .HasMaxLength(200);

        builder.HasIndex(x => new { x.RouteId, x.ChangedAt });
    }
}
