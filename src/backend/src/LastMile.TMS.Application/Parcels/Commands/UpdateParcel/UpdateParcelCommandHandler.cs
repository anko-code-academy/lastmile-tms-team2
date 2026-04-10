using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Mappings;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class UpdateParcelCommandHandler(
    IAppDbContext dbContext,
    IGeocodingService geocodingService,
    IZoneMatchingService zoneMatchingService,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateParcelCommand, ParcelDetailDto?>
{
    public async Task<ParcelDetailDto?> Handle(UpdateParcelCommand request, CancellationToken cancellationToken)
    {
        var parcel = await dbContext.Parcels
            .Include(p => p.ShipperAddress)
            .Include(p => p.RecipientAddress)
            .Include(p => p.ChangeHistory)
            .Include(p => p.TrackingEvents)
            .Include(p => p.DeliveryConfirmation)
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .FirstOrDefaultAsync(p => p.Id == request.Dto.Id, cancellationToken);

        if (parcel is null)
        {
            return null;
        }

        if (!parcel.CanEditBeforeLoad())
        {
            throw new InvalidOperationException(
                $"Parcel {parcel.TrackingNumber} cannot be edited while in status {parcel.Status}.");
        }

        var input = Normalize(request.Dto);

        var shipperExists = await dbContext.Addresses
            .AnyAsync(address => address.Id == input.ShipperAddressId, cancellationToken);
        if (!shipperExists)
        {
            throw new ArgumentException($"Shipper address with ID '{input.ShipperAddressId}' was not found.");
        }

        var depotNames = await dbContext.Depots
            .AsNoTracking()
            .Where(depot => depot.AddressId == parcel.ShipperAddressId || depot.AddressId == input.ShipperAddressId)
            .ToDictionaryAsync(depot => depot.AddressId, depot => depot.Name, cancellationToken);

        var historyEntries = new List<ParcelChangeHistoryEntry>();
        var actor = currentUser.UserName ?? currentUser.UserId;
        var now = DateTimeOffset.UtcNow;

        var currentZone = parcel.Zone;
        var nextZone = currentZone;
        NetTopologySuite.Geometries.Point? point = null;

        if (ParcelChangeSupport.HasGeocodedAddressChanges(parcel, input))
        {
            var hasNonCancelledRouteAssignment = await dbContext.Routes
                .AsNoTracking()
                .AnyAsync(
                    route => route.Status != RouteStatus.Cancelled
                        && route.Parcels.Any(assignedParcel => assignedParcel.Id == parcel.Id),
                    cancellationToken);

            if (hasNonCancelledRouteAssignment)
            {
                throw new InvalidOperationException(
                    $"Parcel {parcel.TrackingNumber} is already assigned to a route and its delivery address cannot be changed until the route is cancelled or recomputed.");
            }

            var addressString = ParcelChangeSupport.BuildAddressString(input.RecipientAddress);
            point = await geocodingService.GeocodeAsync(addressString, cancellationToken);

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

            nextZone = await dbContext.Zones
                .Include(zone => zone.Depot)
                .FirstOrDefaultAsync(zone => zone.Id == zoneId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Zone with ID '{zoneId}' was found but could not be loaded.");
        }

        TrackChange(
            historyEntries,
            parcel.Id,
            ParcelChangeAction.Updated,
            now,
            actor,
            "Sender depot",
            depotNames.GetValueOrDefault(parcel.ShipperAddressId) ?? parcel.ShipperAddressId.ToString(),
            depotNames.GetValueOrDefault(input.ShipperAddressId) ?? input.ShipperAddressId.ToString());

        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient street 1",
            ParcelChangeSupport.NormalizeRequired(parcel.RecipientAddress.Street1),
            ParcelChangeSupport.NormalizeRequired(input.RecipientAddress.Street1));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient street 2",
            ParcelChangeSupport.NormalizeOptional(parcel.RecipientAddress.Street2),
            ParcelChangeSupport.NormalizeOptional(input.RecipientAddress.Street2));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient city",
            ParcelChangeSupport.NormalizeRequired(parcel.RecipientAddress.City),
            ParcelChangeSupport.NormalizeRequired(input.RecipientAddress.City));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient state",
            ParcelChangeSupport.NormalizeRequired(parcel.RecipientAddress.State),
            ParcelChangeSupport.NormalizeRequired(input.RecipientAddress.State));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient postal code",
            ParcelChangeSupport.NormalizeRequired(parcel.RecipientAddress.PostalCode),
            ParcelChangeSupport.NormalizeRequired(input.RecipientAddress.PostalCode));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient country code",
            ParcelChangeSupport.NormalizeCountryCode(parcel.RecipientAddress.CountryCode),
            ParcelChangeSupport.NormalizeCountryCode(input.RecipientAddress.CountryCode));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Residential",
            ParcelChangeSupport.FormatBool(parcel.RecipientAddress.IsResidential),
            ParcelChangeSupport.FormatBool(input.RecipientAddress.IsResidential));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient contact name",
            ParcelChangeSupport.NormalizeOptional(parcel.RecipientAddress.ContactName),
            ParcelChangeSupport.NormalizeOptional(input.RecipientAddress.ContactName));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient company name",
            ParcelChangeSupport.NormalizeOptional(parcel.RecipientAddress.CompanyName),
            ParcelChangeSupport.NormalizeOptional(input.RecipientAddress.CompanyName));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient phone",
            ParcelChangeSupport.NormalizeOptional(parcel.RecipientAddress.Phone),
            ParcelChangeSupport.NormalizeOptional(input.RecipientAddress.Phone));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Recipient email",
            ParcelChangeSupport.NormalizeOptional(parcel.RecipientAddress.Email),
            ParcelChangeSupport.NormalizeOptional(input.RecipientAddress.Email));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Description",
            ParcelChangeSupport.NormalizeOptional(parcel.Description),
            ParcelChangeSupport.NormalizeOptional(input.Description));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Service type",
            ParcelChangeSupport.FormatEnum(parcel.ServiceType),
            ParcelChangeSupport.FormatEnum(input.ServiceType));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Weight",
            ParcelChangeSupport.FormatDecimal(parcel.Weight),
            ParcelChangeSupport.FormatDecimal(input.Weight));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Weight unit",
            ParcelChangeSupport.FormatEnum(parcel.WeightUnit),
            ParcelChangeSupport.FormatEnum(input.WeightUnit));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Length",
            ParcelChangeSupport.FormatDecimal(parcel.Length),
            ParcelChangeSupport.FormatDecimal(input.Length));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Width",
            ParcelChangeSupport.FormatDecimal(parcel.Width),
            ParcelChangeSupport.FormatDecimal(input.Width));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Height",
            ParcelChangeSupport.FormatDecimal(parcel.Height),
            ParcelChangeSupport.FormatDecimal(input.Height));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Dimension unit",
            ParcelChangeSupport.FormatEnum(parcel.DimensionUnit),
            ParcelChangeSupport.FormatEnum(input.DimensionUnit));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Declared value",
            ParcelChangeSupport.FormatDecimal(parcel.DeclaredValue),
            ParcelChangeSupport.FormatDecimal(input.DeclaredValue));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Currency",
            ParcelChangeSupport.NormalizeCurrency(parcel.Currency),
            ParcelChangeSupport.NormalizeCurrency(input.Currency));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Estimated delivery date",
            ParcelChangeSupport.FormatDate(parcel.EstimatedDeliveryDate),
            ParcelChangeSupport.FormatDate(new DateTimeOffset(input.EstimatedDeliveryDate, TimeSpan.Zero)));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Parcel type",
            ParcelChangeSupport.NormalizeOptional(parcel.ParcelType),
            ParcelChangeSupport.NormalizeOptional(input.ParcelType));
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Sort zone",
            currentZone.Name,
            nextZone.Name);
        TrackChange(historyEntries, parcel.Id, ParcelChangeAction.Updated, now, actor, "Depot",
            currentZone.Depot?.Name,
            nextZone.Depot?.Name);

        if (historyEntries.Count == 0)
        {
            return parcel.ToDetailDto();
        }

        parcel.ShipperAddressId = input.ShipperAddressId;
        parcel.Description = input.Description;
        parcel.ServiceType = input.ServiceType;
        parcel.Weight = input.Weight;
        parcel.WeightUnit = input.WeightUnit;
        parcel.Length = input.Length;
        parcel.Width = input.Width;
        parcel.Height = input.Height;
        parcel.DimensionUnit = input.DimensionUnit;
        parcel.DeclaredValue = input.DeclaredValue;
        parcel.Currency = input.Currency;
        parcel.EstimatedDeliveryDate = new DateTimeOffset(input.EstimatedDeliveryDate, TimeSpan.Zero);
        parcel.ParcelType = input.ParcelType;
        parcel.ZoneId = nextZone.Id;
        parcel.Zone = nextZone;
        parcel.LastModifiedAt = now;
        parcel.LastModifiedBy = actor;

        parcel.RecipientAddress.Street1 = input.RecipientAddress.Street1;
        parcel.RecipientAddress.Street2 = input.RecipientAddress.Street2;
        parcel.RecipientAddress.City = input.RecipientAddress.City;
        parcel.RecipientAddress.State = input.RecipientAddress.State;
        parcel.RecipientAddress.PostalCode = input.RecipientAddress.PostalCode;
        parcel.RecipientAddress.CountryCode = input.RecipientAddress.CountryCode;
        parcel.RecipientAddress.IsResidential = input.RecipientAddress.IsResidential;
        parcel.RecipientAddress.ContactName = input.RecipientAddress.ContactName;
        parcel.RecipientAddress.CompanyName = input.RecipientAddress.CompanyName;
        parcel.RecipientAddress.Phone = input.RecipientAddress.Phone;
        parcel.RecipientAddress.Email = input.RecipientAddress.Email;
        if (point is not null)
        {
            parcel.RecipientAddress.GeoLocation = point;
        }
        parcel.RecipientAddress.LastModifiedAt = now;
        parcel.RecipientAddress.LastModifiedBy = actor;

        foreach (var historyEntry in historyEntries)
        {
            parcel.ChangeHistory.Add(historyEntry);
            dbContext.ParcelChangeHistoryEntries.Add(historyEntry);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return parcel.ToDetailDto();
    }

    private static UpdateParcelDto Normalize(UpdateParcelDto dto)
    {
        var address = dto.RecipientAddress;

        return dto with
        {
            Description = ParcelChangeSupport.NormalizeOptional(dto.Description),
            ParcelType = ParcelChangeSupport.NormalizeOptional(dto.ParcelType),
            Currency = ParcelChangeSupport.NormalizeCurrency(dto.Currency),
            RecipientAddress = address with
            {
                Street1 = ParcelChangeSupport.NormalizeRequired(address.Street1),
                Street2 = ParcelChangeSupport.NormalizeOptional(address.Street2),
                City = ParcelChangeSupport.NormalizeRequired(address.City),
                State = ParcelChangeSupport.NormalizeRequired(address.State),
                PostalCode = ParcelChangeSupport.NormalizeRequired(address.PostalCode),
                CountryCode = ParcelChangeSupport.NormalizeCountryCode(address.CountryCode),
                ContactName = ParcelChangeSupport.NormalizeOptional(address.ContactName),
                CompanyName = ParcelChangeSupport.NormalizeOptional(address.CompanyName),
                Phone = ParcelChangeSupport.NormalizeOptional(address.Phone),
                Email = ParcelChangeSupport.NormalizeOptional(address.Email),
            }
        };
    }

    private static void TrackChange(
        ICollection<ParcelChangeHistoryEntry> historyEntries,
        Guid parcelId,
        ParcelChangeAction action,
        DateTimeOffset changedAt,
        string? changedBy,
        string fieldName,
        string? beforeValue,
        string? afterValue)
    {
        var normalizedBefore = string.IsNullOrWhiteSpace(beforeValue) ? null : beforeValue;
        var normalizedAfter = string.IsNullOrWhiteSpace(afterValue) ? null : afterValue;

        if (normalizedBefore == normalizedAfter)
        {
            return;
        }

        historyEntries.Add(new ParcelChangeHistoryEntry
        {
            ParcelId = parcelId,
            Action = action,
            FieldName = fieldName,
            BeforeValue = normalizedBefore,
            AfterValue = normalizedAfter,
            ChangedAt = changedAt,
            ChangedBy = changedBy,
        });
    }
}
