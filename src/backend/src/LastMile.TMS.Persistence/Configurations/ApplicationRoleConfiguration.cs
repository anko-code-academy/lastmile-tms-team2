using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.HasIndex(r => r.Name)
            .IsUnique();

        builder.HasMany(r => r.Permissions)
            .WithMany(p => p.Roles)
            .UsingEntity("RolePermissions");
    }
}
