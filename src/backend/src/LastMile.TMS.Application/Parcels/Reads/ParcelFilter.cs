using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Parcels.Reads;

public sealed class ParcelFilter
{
    public ParcelStatus[]? Status { get; set; }
    public Guid? ZoneId { get; set; }
    public string? ParcelType { get; set; }
    public DateTimeOffset? EstimatedDeliveryDateFrom { get; set; }
    public DateTimeOffset? EstimatedDeliveryDateTo { get; set; }
}
