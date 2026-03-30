using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Application.Parcels.Mappings;

[Mapper]
public static partial class ParcelMappings
{
    [MapperIgnoreTarget(nameof(Address.GeoLocation))]
    [MapperIgnoreTarget(nameof(Address.ShipperParcels))]
    [MapperIgnoreTarget(nameof(Address.RecipientParcels))]
    [MapperIgnoreTarget(nameof(Address.CreatedAt))]
    [MapperIgnoreTarget(nameof(Address.CreatedBy))]
    [MapperIgnoreTarget(nameof(Address.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(Address.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Address.Id))]
    public static partial Address ToEntity(this RegisterParcelRecipientAddressDto dto);

    [MapperIgnoreSource(nameof(Parcel.ShipperAddressId))]
    [MapperIgnoreSource(nameof(Parcel.RecipientAddressId))]
    [MapperIgnoreSource(nameof(Parcel.ActualDeliveryDate))]
    [MapperIgnoreSource(nameof(Parcel.ShipperAddress))]
    [MapperIgnoreSource(nameof(Parcel.RecipientAddress))]
    [MapperIgnoreSource(nameof(Parcel.DeliveryConfirmation))]
    [MapperIgnoreSource(nameof(Parcel.ContentItems))]
    [MapperIgnoreSource(nameof(Parcel.TrackingEvents))]
    [MapperIgnoreSource(nameof(Parcel.Watchers))]
    [MapperIgnoreSource(nameof(Parcel.CreatedBy))]
    [MapperIgnoreSource(nameof(Parcel.LastModifiedBy))]
    [MapperIgnoreSource(nameof(Parcel.ZoneId))]
    [MapProperty("Zone.Id", nameof(ParcelDto.ZoneId))]
    [MapProperty("Zone.Name", nameof(ParcelDto.ZoneName))]
    [MapProperty("Zone.DepotId", nameof(ParcelDto.DepotId))]
    [MapProperty("Zone.Depot.Name", nameof(ParcelDto.DepotName))]
    [MapProperty(nameof(Parcel.ServiceType), nameof(ParcelDto.ServiceType))]
    [MapProperty(nameof(Parcel.Status), nameof(ParcelDto.Status))]
    [MapProperty(nameof(Parcel.WeightUnit), nameof(ParcelDto.WeightUnit))]
    [MapProperty(nameof(Parcel.DimensionUnit), nameof(ParcelDto.DimensionUnit))]
    [MapProperty(nameof(Parcel.TrackingNumber), nameof(ParcelDto.Barcode))]
    public static partial ParcelDto ToDto(this Parcel parcel);

    private static string MapToString(Enum value) => value.ToString();
}
