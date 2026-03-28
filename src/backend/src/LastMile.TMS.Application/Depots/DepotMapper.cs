using LastMile.TMS.Application.Depots.DTOs;
using LastMile.TMS.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Application.Depots;

[Mapper]
public static partial class DepotMapper
{
    [MapperIgnoreTarget(nameof(Address.GeoLocation))]
    [MapperIgnoreTarget(nameof(Address.ShipperParcels))]
    [MapperIgnoreTarget(nameof(Address.RecipientParcels))]
    [MapperIgnoreTarget(nameof(Address.CreatedAt))]
    [MapperIgnoreTarget(nameof(Address.CreatedBy))]
    [MapperIgnoreTarget(nameof(Address.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(Address.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Address.Id))]
    public static partial Address ToEntity(this AddressDto dto);

    [MapperIgnoreTarget(nameof(OperatingHours.DepotId))]
    [MapperIgnoreTarget(nameof(OperatingHours.Depot))]
    [MapperIgnoreTarget(nameof(OperatingHours.CreatedAt))]
    [MapperIgnoreTarget(nameof(OperatingHours.CreatedBy))]
    [MapperIgnoreTarget(nameof(OperatingHours.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(OperatingHours.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(OperatingHours.Id))]
    public static partial OperatingHours ToEntity(this OperatingHoursDto dto);
}
