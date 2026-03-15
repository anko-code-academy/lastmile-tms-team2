using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Entities;

public class Depot: BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid AddressId { get; set; }
    public Address Address { get; set; } = null!;

    //public OperatingHours OperatingHours { get; set; } = null!;
    public ICollection<OperatingHours> OperatingHours { get; set; } = new List<OperatingHours>(); 

    public bool IsActive { get; set; } = true;

    public ICollection<Zone> Zones { get; set; } = new List<Zone>(); 
}