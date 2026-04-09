using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Entities;

public class Route : BaseAuditableEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = null!;

    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    public int StartMileage { get; set; }
    public int EndMileage { get; set; }

    public StagingArea StagingArea { get; set; }

    public RouteStatus Status { get; set; }

    public ICollection<Parcel> Parcels { get; set; } = new List<Parcel>();
    public ICollection<RouteAssignmentAuditEntry> AssignmentAuditTrail { get; set; } =
        new List<RouteAssignmentAuditEntry>();

    /// <summary>
    /// Calculated total mileage for this route
    /// </summary>
    public int TotalMileage => EndMileage > 0 ? EndMileage - StartMileage : 0;

    /// <summary>
    /// Number of parcels delivered on this route
    /// </summary>
    public int ParcelsDelivered => Parcels?.Count(p => p.Status == ParcelStatus.Delivered) ?? 0;

    /// <summary>
    /// Number of parcels assigned to this route
    /// </summary>
    public int ParcelCount => Parcels?.Count ?? 0;

    /// <summary>
    /// Half-open intervals [start, end): a null end means unbounded forward.
    /// Used for scheduling conflict checks; keep in sync with queries that use the same rules.
    /// </summary>
    public static bool TimeRangesOverlap(
        DateTimeOffset startA, DateTimeOffset? endA,
        DateTimeOffset startB, DateTimeOffset? endB)
    {
        var endAExclusive = endA ?? DateTimeOffset.MaxValue;
        var endBExclusive = endB ?? DateTimeOffset.MaxValue;
        return startA < endBExclusive && startB < endAExclusive;
    }
}
