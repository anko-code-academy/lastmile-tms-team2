using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(u => u.Depot)
            .WithMany()
            .HasForeignKey(u => u.DepotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Zone)
            .WithMany()
            .HasForeignKey(u => u.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(u => u.Email)
            .IsUnique();
    }
}
