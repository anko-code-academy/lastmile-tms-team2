using LastMile.TMS.Domain.Common;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Domain.Entities;

public class RouteStop : BaseAuditableEntity
{
    public Guid RouteId { get; set; }
    public Route Route { get; set; } = null!;

    public int Sequence { get; set; }

    public string RecipientLabel { get; set; } = string.Empty;
    public string Street1 { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;

    public Point StopLocation { get; set; } = null!;

    public ICollection<Parcel> Parcels { get; set; } = new List<Parcel>();
}
