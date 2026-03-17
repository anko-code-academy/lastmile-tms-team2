using LastMile.TMS.Domain.Common;

namespace LastMile.TMS.Domain.Entities;

public class DriverAvailability : BaseAuditableEntity
{
    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly? ShiftStart { get; set; }
    public TimeOnly? ShiftEnd { get; set; }
    public bool IsAvailable { get; set; }
}
