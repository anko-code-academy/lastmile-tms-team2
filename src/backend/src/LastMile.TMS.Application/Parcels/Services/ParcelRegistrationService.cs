using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Mappings;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Services;

public sealed class ParcelRegistrationService(
    IAppDbContext db,
    IGeocodingService geocodingService,
    IZoneMatchingService zoneMatchingService,
    ICurrentUserService currentUser)
    : IParcelRegistrationService
{
    public async Task<ParcelDto> RegisterAsync(
        RegisterParcelDto dto,
        CancellationToken cancellationToken = default,
        Guid? parcelImportId = null,
        string? actorOverride = null)
    {
        var shipperExists = await db.Addresses
            .AnyAsync(a => a.Id == dto.ShipperAddressId, cancellationToken);
        if (!shipperExists)
        {
            throw new ArgumentException($"Shipper address with ID '{dto.ShipperAddressId}' was not found.");
        }

        var addressString = BuildAddressString(dto);
        var point = await geocodingService.GeocodeAsync(addressString, cancellationToken);

        if (point is null)
        {
            throw new InvalidOperationException(
                $"Could not determine zone: recipient address could not be geocoded. Address: {addressString}");
        }

        var zoneId = await zoneMatchingService.FindZoneIdAsync(point, cancellationToken);
        if (zoneId is null)
        {
            throw new InvalidOperationException(
                $"No active zone covers the recipient address location. Address: {addressString}");
        }

        var zone = await db.Zones
            .Include(z => z.Depot)
            .FirstOrDefaultAsync(z => z.Id == zoneId, cancellationToken);
        if (zone is null)
        {
            throw new InvalidOperationException($"Zone with ID '{zoneId}' was found but could not be loaded.");
        }

        var actor = actorOverride ?? currentUser.UserName ?? currentUser.UserId;

        var recipientAddress = dto.RecipientAddress.ToEntity();
        recipientAddress.CountryCode = recipientAddress.CountryCode.ToUpperInvariant();
        recipientAddress.GeoLocation = point;
        recipientAddress.CreatedBy = actor;

        var parcel = new Parcel
        {
            Id = Guid.NewGuid(),
            TrackingNumber = Parcel.GenerateTrackingNumber(),
            Description = dto.Description,
            ServiceType = dto.ServiceType,
            Status = ParcelStatus.Registered,
            ShipperAddressId = dto.ShipperAddressId,
            RecipientAddress = recipientAddress,
            Weight = dto.Weight,
            WeightUnit = dto.WeightUnit,
            Length = dto.Length,
            Width = dto.Width,
            Height = dto.Height,
            DimensionUnit = dto.DimensionUnit,
            DeclaredValue = dto.DeclaredValue,
            Currency = dto.Currency,
            EstimatedDeliveryDate = new DateTimeOffset(dto.EstimatedDeliveryDate, TimeSpan.Zero),
            ParcelType = dto.ParcelType,
            ZoneId = zoneId.Value,
            ParcelImportId = parcelImportId,
            CreatedBy = actor,
        };

        var locationLabel = zone.Depot?.Name ?? zone.Name;
        var now = DateTimeOffset.UtcNow;
        parcel.TrackingEvents.Add(
            ParcelTrackingEventFactory.CreateForParcelStatus(
                parcel.Id,
                ParcelStatus.Registered,
                now,
                locationLabel,
                "Parcel registered",
                actor));

        db.Parcels.Add(parcel);
        await db.SaveChangesAsync(cancellationToken);

        parcel.Zone = zone;
        return parcel.ToDto();
    }

    private static string BuildAddressString(RegisterParcelDto dto)
    {
        var address = dto.RecipientAddress;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.Street1)) parts.Add(address.Street1);
        if (!string.IsNullOrWhiteSpace(address.Street2)) parts.Add(address.Street2);
        if (!string.IsNullOrWhiteSpace(address.City)) parts.Add(address.City);
        if (!string.IsNullOrWhiteSpace(address.State)) parts.Add(address.State);
        if (!string.IsNullOrWhiteSpace(address.PostalCode)) parts.Add(address.PostalCode);
        if (!string.IsNullOrWhiteSpace(address.CountryCode)) parts.Add(address.CountryCode);
        return string.Join(", ", parts);
    }
}
