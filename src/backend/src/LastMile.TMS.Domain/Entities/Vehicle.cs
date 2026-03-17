using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Entities;

public class Vehicle : BaseAuditableEntity
{
    public string RegistrationPlate { get; set; } = string.Empty;

    public VehicleType Type { get; set; }

    public int ParcelCapacity { get; set; }

    public decimal WeightCapacity { get; set; }

    public VehicleStatus Status { get; set; }

    public Guid DepotId { get; set; }

    public Depot Depot { get; set; } = null!;
}