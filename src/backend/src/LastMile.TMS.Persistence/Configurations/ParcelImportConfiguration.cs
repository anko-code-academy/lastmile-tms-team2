using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LastMile.TMS.Persistence.Configurations;

public class ParcelImportConfiguration : IEntityTypeConfiguration<ParcelImport>
{
    public void Configure(EntityTypeBuilder<ParcelImport> builder)
    {
        builder.ToTable("ParcelImports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(x => x.FileFormat)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.SourceFileKey)
            .HasMaxLength(512);

        builder.Property(x => x.SourceFile);

        builder.Property(x => x.FailureMessage)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.ShipperAddress)
            .WithMany()
            .HasForeignKey(x => x.ShipperAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.RowFailures)
            .WithOne(x => x.ParcelImport)
            .HasForeignKey(x => x.ParcelImportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
