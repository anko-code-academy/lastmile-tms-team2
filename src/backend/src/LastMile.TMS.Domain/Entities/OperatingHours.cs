using LastMile.TMS.Domain.Common;

public class OperatingHours: BaseAuditableEntity
{
    public Guid DepotId { get; set; }
    public Depot Depot { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? ClosedTime { get; set; }
    public bool IsClosed { get; set; }
}