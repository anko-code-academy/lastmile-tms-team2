using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Entities;

public class RouteParcelAdjustmentAuditEntry : BaseEntity
{
    public Guid RouteId { get; set; }
    public Guid ParcelId { get; set; }
    public RouteParcelAdjustmentAction Action { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int? AffectedStopSequence { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? ChangedBy { get; set; }

    public Route Route { get; set; } = null!;
}
