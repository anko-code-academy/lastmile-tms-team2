using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Entities;

public class ParcelImport : BaseAuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public ParcelImportFileFormat FileFormat { get; set; }
    public Guid ShipperAddressId { get; set; }
    public ParcelImportStatus Status { get; set; }
    public string? SourceFileKey { get; set; }
    public byte[]? SourceFile { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int ImportedRows { get; set; }
    public int RejectedRows { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureMessage { get; set; }

    public Address ShipperAddress { get; set; } = null!;
    public ICollection<ParcelImportRowFailure> RowFailures { get; set; } = new List<ParcelImportRowFailure>();
    public ICollection<Parcel> Parcels { get; set; } = new List<Parcel>();
}
