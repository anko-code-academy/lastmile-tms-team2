using LastMile.TMS.Application.Depots.DTOs;
using LastMile.TMS.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Application.Depots.Mappings;

[Mapper]
public static partial class DepotMappings
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

    [MapperIgnoreTarget(nameof(Address.GeoLocation))]
    [MapperIgnoreTarget(nameof(Address.ShipperParcels))]
    [MapperIgnoreTarget(nameof(Address.RecipientParcels))]
    [MapperIgnoreTarget(nameof(Address.CreatedAt))]
    [MapperIgnoreTarget(nameof(Address.CreatedBy))]
    [MapperIgnoreTarget(nameof(Address.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(Address.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Address.Id))]
    public static partial void UpdateEntity(this AddressDto dto, [MappingTarget] Address address);

    [MapperIgnoreTarget(nameof(OperatingHours.DepotId))]
    [MapperIgnoreTarget(nameof(OperatingHours.Depot))]
    [MapperIgnoreTarget(nameof(OperatingHours.CreatedAt))]
    [MapperIgnoreTarget(nameof(OperatingHours.CreatedBy))]
    [MapperIgnoreTarget(nameof(OperatingHours.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(OperatingHours.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(OperatingHours.Id))]
    public static partial OperatingHours ToEntity(this OperatingHoursDto dto);

    [MapperIgnoreTarget(nameof(OperatingHours.DepotId))]
    [MapperIgnoreTarget(nameof(OperatingHours.Depot))]
    [MapperIgnoreTarget(nameof(OperatingHours.CreatedAt))]
    [MapperIgnoreTarget(nameof(OperatingHours.CreatedBy))]
    [MapperIgnoreTarget(nameof(OperatingHours.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(OperatingHours.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(OperatingHours.Id))]
    public static partial void UpdateEntity(this OperatingHoursDto dto, [MappingTarget] OperatingHours entity);

    [MapperIgnoreTarget(nameof(Depot.AddressId))]
    [MapperIgnoreTarget(nameof(Depot.Address))]
    [MapperIgnoreTarget(nameof(Depot.OperatingHours))]
    [MapperIgnoreTarget(nameof(Depot.Zones))]
    [MapperIgnoreTarget(nameof(Depot.Drivers))]
    [MapperIgnoreTarget(nameof(Depot.CreatedAt))]
    [MapperIgnoreTarget(nameof(Depot.CreatedBy))]
    [MapperIgnoreTarget(nameof(Depot.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(Depot.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Depot.Id))]
    [MapperIgnoreSource(nameof(CreateDepotDto.Address))]
    [MapperIgnoreSource(nameof(CreateDepotDto.OperatingHours))]
    public static partial Depot ToEntity(this CreateDepotDto dto);

    [MapperIgnoreTarget(nameof(Depot.AddressId))]
    [MapperIgnoreTarget(nameof(Depot.Address))]
    [MapperIgnoreTarget(nameof(Depot.OperatingHours))]
    [MapperIgnoreTarget(nameof(Depot.Zones))]
    [MapperIgnoreTarget(nameof(Depot.Drivers))]
    [MapperIgnoreTarget(nameof(Depot.CreatedAt))]
    [MapperIgnoreTarget(nameof(Depot.CreatedBy))]
    [MapperIgnoreTarget(nameof(Depot.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(Depot.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Depot.Id))]
    [MapperIgnoreSource(nameof(UpdateDepotDto.Address))]
    [MapperIgnoreSource(nameof(UpdateDepotDto.OperatingHours))]
    public static partial void UpdateEntity(this UpdateDepotDto dto, [MappingTarget] Depot depot);
}
