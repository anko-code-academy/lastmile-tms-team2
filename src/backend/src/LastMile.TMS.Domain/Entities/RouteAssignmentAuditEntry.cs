using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Entities;

public class RouteAssignmentAuditEntry : BaseEntity
{
    public Guid RouteId { get; set; }
    public RouteAssignmentAuditAction Action { get; set; }

    public Guid? PreviousDriverId { get; set; }
    public string? PreviousDriverName { get; set; }
    public Guid NewDriverId { get; set; }
    public string NewDriverName { get; set; } = string.Empty;

    public Guid? PreviousVehicleId { get; set; }
    public string? PreviousVehiclePlate { get; set; }
    public Guid NewVehicleId { get; set; }
    public string NewVehiclePlate { get; set; } = string.Empty;

    public DateTimeOffset ChangedAt { get; set; }
    public string? ChangedBy { get; set; }

    public Route Route { get; set; } = null!;
}
