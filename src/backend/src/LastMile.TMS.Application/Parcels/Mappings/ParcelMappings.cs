using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Support;
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
    [MapperIgnoreSource(nameof(Parcel.ShipperAddress))]
    [MapperIgnoreSource(nameof(Parcel.RecipientAddress))]
    [MapperIgnoreSource(nameof(Parcel.DeliveryConfirmation))]
    [MapperIgnoreSource(nameof(Parcel.ContentItems))]
    [MapperIgnoreSource(nameof(Parcel.CancellationReason))]
    [MapperIgnoreSource(nameof(Parcel.ChangeHistory))]
    [MapperIgnoreSource(nameof(Parcel.TrackingEvents))]
    [MapperIgnoreSource(nameof(Parcel.Watchers))]
    [MapperIgnoreSource(nameof(Parcel.CreatedBy))]
    [MapperIgnoreSource(nameof(Parcel.LastModifiedBy))]
    [MapperIgnoreSource(nameof(Parcel.ZoneId))]
    [MapperIgnoreSource(nameof(Parcel.ParcelImportId))]
    [MapperIgnoreSource(nameof(Parcel.ParcelImport))]
    [MapProperty("Zone.Id", nameof(ParcelDto.ZoneId))]
    [MapProperty("Zone.Name", nameof(ParcelDto.ZoneName))]
    [MapProperty("Zone.DepotId", nameof(ParcelDto.DepotId))]
    [MapProperty("Zone.Depot.Name", nameof(ParcelDto.DepotName))]
    [MapProperty(nameof(Parcel.ServiceType), nameof(ParcelDto.ServiceType))]
    [MapProperty(nameof(Parcel.Status), nameof(ParcelDto.Status))]
    [MapProperty(nameof(Parcel.WeightUnit), nameof(ParcelDto.WeightUnit))]
    [MapProperty(nameof(Parcel.DimensionUnit), nameof(ParcelDto.DimensionUnit))]
    [MapProperty(nameof(Parcel.TrackingNumber), nameof(ParcelDto.Barcode))]
    [MapProperty(nameof(Parcel.ActualDeliveryDate), nameof(ParcelDto.ActualDeliveryDate))]
    public static partial ParcelDto ToDto(this Parcel parcel);

    [MapProperty(nameof(Address.ContactName), nameof(ParcelDetailAddressDto.ContactName))]
    [MapProperty(nameof(Address.CompanyName), nameof(ParcelDetailAddressDto.CompanyName))]
    [MapProperty(nameof(Address.Phone), nameof(ParcelDetailAddressDto.Phone))]
    [MapProperty(nameof(Address.Email), nameof(ParcelDetailAddressDto.Email))]
    [MapperIgnoreSource(nameof(Address.GeoLocation))]
    [MapperIgnoreSource(nameof(Address.ShipperParcels))]
    [MapperIgnoreSource(nameof(Address.RecipientParcels))]
    [MapperIgnoreSource(nameof(Address.CreatedAt))]
    [MapperIgnoreSource(nameof(Address.CreatedBy))]
    [MapperIgnoreSource(nameof(Address.LastModifiedAt))]
    [MapperIgnoreSource(nameof(Address.LastModifiedBy))]
    [MapperIgnoreSource(nameof(Address.Id))]
    public static partial ParcelDetailAddressDto ToDetailDto(this Address address);

    [MapperIgnoreSource(nameof(Parcel.ShipperAddressId))]
    [MapperIgnoreSource(nameof(Parcel.RecipientAddressId))]
    [MapperIgnoreSource(nameof(Parcel.ActualDeliveryDate))]
    [MapperIgnoreSource(nameof(Parcel.ShipperAddress))]
    [MapperIgnoreSource(nameof(Parcel.DeliveryConfirmation))]
    [MapperIgnoreSource(nameof(Parcel.ContentItems))]
    [MapperIgnoreSource(nameof(Parcel.TrackingEvents))]
    [MapperIgnoreSource(nameof(Parcel.Watchers))]
    [MapperIgnoreSource(nameof(Parcel.CreatedBy))]
    [MapperIgnoreSource(nameof(Parcel.LastModifiedBy))]
    [MapperIgnoreSource(nameof(Parcel.ZoneId))]
    [MapperIgnoreSource(nameof(Parcel.ChangeHistory))]
    public static ParcelDetailDto ToDetailDto(this Parcel parcel)
    {
        var history = parcel.ChangeHistory
            .OrderByDescending(entry => entry.ChangedAt)
            .Select(entry => new ParcelChangeHistoryDto
            {
                Action = ParcelChangeSupport.FormatEnum(entry.Action),
                FieldName = entry.FieldName,
                BeforeValue = entry.BeforeValue,
                AfterValue = entry.AfterValue,
                ChangedAt = entry.ChangedAt,
                ChangedBy = entry.ChangedBy,
            })
            .ToArray();

        return new ParcelDetailDto
        {
            Id = parcel.Id,
            TrackingNumber = parcel.TrackingNumber,
            Status = parcel.Status.ToString(),
            ShipperAddressId = parcel.ShipperAddressId,
            ServiceType = parcel.ServiceType.ToString(),
            Weight = parcel.Weight,
            WeightUnit = parcel.WeightUnit.ToString(),
            Length = parcel.Length,
            Width = parcel.Width,
            Height = parcel.Height,
            DimensionUnit = parcel.DimensionUnit.ToString(),
            DeclaredValue = parcel.DeclaredValue,
            Currency = parcel.Currency,
            Description = parcel.Description,
            ParcelType = parcel.ParcelType,
            CancellationReason = parcel.CancellationReason,
            EstimatedDeliveryDate = parcel.EstimatedDeliveryDate,
            DeliveryAttempts = parcel.DeliveryAttempts,
            ZoneId = parcel.Zone.Id,
            ZoneName = parcel.Zone.Name,
            DepotId = parcel.Zone.DepotId,
            DepotName = parcel.Zone.Depot?.Name,
            CreatedAt = parcel.CreatedAt,
            LastModifiedAt = parcel.LastModifiedAt,
            CanEdit = parcel.CanEditBeforeLoad(),
            CanCancel = parcel.CanCancelBeforeLoad(),
            RecipientAddress = parcel.RecipientAddress.ToDetailDto(),
            ChangeHistory = history,
        };
    }

    [MapperIgnoreSource(nameof(Parcel.Description))]
    [MapperIgnoreSource(nameof(Parcel.ServiceType))]
    [MapperIgnoreSource(nameof(Parcel.Status))]
    [MapperIgnoreSource(nameof(Parcel.CancellationReason))]
    [MapperIgnoreSource(nameof(Parcel.ShipperAddressId))]
    [MapperIgnoreSource(nameof(Parcel.RecipientAddressId))]
    [MapperIgnoreSource(nameof(Parcel.Weight))]
    [MapperIgnoreSource(nameof(Parcel.WeightUnit))]
    [MapperIgnoreSource(nameof(Parcel.Length))]
    [MapperIgnoreSource(nameof(Parcel.Width))]
    [MapperIgnoreSource(nameof(Parcel.Height))]
    [MapperIgnoreSource(nameof(Parcel.DimensionUnit))]
    [MapperIgnoreSource(nameof(Parcel.DeclaredValue))]
    [MapperIgnoreSource(nameof(Parcel.Currency))]
    [MapperIgnoreSource(nameof(Parcel.EstimatedDeliveryDate))]
    [MapperIgnoreSource(nameof(Parcel.ActualDeliveryDate))]
    [MapperIgnoreSource(nameof(Parcel.DeliveryAttempts))]
    [MapperIgnoreSource(nameof(Parcel.ZoneId))]
    [MapperIgnoreSource(nameof(Parcel.ShipperAddress))]
    [MapperIgnoreSource(nameof(Parcel.DeliveryConfirmation))]
    [MapperIgnoreSource(nameof(Parcel.ContentItems))]
    [MapperIgnoreSource(nameof(Parcel.ChangeHistory))]
    [MapperIgnoreSource(nameof(Parcel.TrackingEvents))]
    [MapperIgnoreSource(nameof(Parcel.Watchers))]
    [MapperIgnoreSource(nameof(Parcel.CreatedAt))]
    [MapperIgnoreSource(nameof(Parcel.CreatedBy))]
    [MapperIgnoreSource(nameof(Parcel.LastModifiedAt))]
    [MapperIgnoreSource(nameof(Parcel.LastModifiedBy))]
    [MapProperty("RecipientAddress.ContactName", nameof(ParcelLabelDataDto.RecipientName))]
    [MapProperty("RecipientAddress.CompanyName", nameof(ParcelLabelDataDto.CompanyName))]
    [MapProperty("RecipientAddress.Street1", nameof(ParcelLabelDataDto.Street1))]
    [MapProperty("RecipientAddress.Street2", nameof(ParcelLabelDataDto.Street2))]
    [MapProperty("RecipientAddress.City", nameof(ParcelLabelDataDto.City))]
    [MapProperty("RecipientAddress.State", nameof(ParcelLabelDataDto.State))]
    [MapProperty("RecipientAddress.PostalCode", nameof(ParcelLabelDataDto.PostalCode))]
    [MapProperty("RecipientAddress.CountryCode", nameof(ParcelLabelDataDto.CountryCode))]
    [MapProperty("Zone.Name", nameof(ParcelLabelDataDto.SortZone))]
    public static partial ParcelLabelDataDto ToLabelDataDto(this Parcel parcel);

    private static string MapToString(Enum value) => value.ToString();
}
