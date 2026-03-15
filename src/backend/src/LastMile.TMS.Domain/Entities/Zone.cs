using NetTopologySuite.Geometries;
using LastMile.TMS.Domain.Common;

namespace LastMile.TMS.Domain.Entities;

public class Zone: BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public Polygon Boundary { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public Guid DepotId { get; set; }

    public Depot Depot { get; set; } = null!;

    public void UpdateBoundary(Polygon boundary)
    {
        Boundary = boundary;
    }
}