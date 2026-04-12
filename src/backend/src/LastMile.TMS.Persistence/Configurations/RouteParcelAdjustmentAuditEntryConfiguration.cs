using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class RouteParcelAdjustmentAuditEntryConfiguration
    : IEntityTypeConfiguration<RouteParcelAdjustmentAuditEntry>
{
    public void Configure(EntityTypeBuilder<RouteParcelAdjustmentAuditEntry> builder)
    {
        builder.ToTable("RouteParcelAdjustmentAuditEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .IsRequired();

        builder.Property(x => x.TrackingNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ChangedAt)
            .IsRequired();

        builder.Property(x => x.ChangedBy)
            .HasMaxLength(256);

        builder.HasIndex(x => new { x.RouteId, x.ChangedAt });
    }
}
