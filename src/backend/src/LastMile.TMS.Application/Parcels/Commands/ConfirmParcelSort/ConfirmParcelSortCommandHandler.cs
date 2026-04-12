using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Mappings;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class ConfirmParcelSortCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    IParcelUpdateNotifier parcelUpdateNotifier)
    : IRequestHandler<ConfirmParcelSortCommand, ParcelDto>
{
    public async Task<ParcelDto> Handle(ConfirmParcelSortCommand request, CancellationToken cancellationToken)
    {
        var parcel = await dbContext.Parcels
            .Include(p => p.TrackingEvents)
            .Include(p => p.Zone)
                .ThenInclude(z => z!.Depot)
            .Include(p => p.RecipientAddress)
            .FirstOrDefaultAsync(p => p.Id == request.ParcelId, cancellationToken);

        if (parcel is null)
        {
            throw new InvalidOperationException($"Parcel with ID '{request.ParcelId}' was not found.");
        }

        if (parcel.Status != ParcelStatus.ReceivedAtDepot)
        {
            throw new InvalidOperationException(
                $"Parcel must be in '{ParcelStatus.ReceivedAtDepot}' status to confirm sort. Current status: {parcel.Status}.");
        }

        var bin = await dbContext.BinLocations
            .Include(b => b.StorageAisle)
                .ThenInclude(a => a!.StorageZone)
            .FirstOrDefaultAsync(b => b.Id == request.BinLocationId, cancellationToken);

        if (bin is null)
        {
            throw new InvalidOperationException($"Bin location with ID '{request.BinLocationId}' was not found.");
        }

        if (!bin.IsActive)
        {
            throw new InvalidOperationException(
                "Mis-sort: the scanned bin is inactive. Choose an active bin linked to this delivery zone.");
        }

        if (bin.DeliveryZoneId is null)
        {
            throw new InvalidOperationException(
                "Mis-sort: this bin is not linked to a delivery zone. Use a sort bin assigned to the parcel zone.");
        }

        if (bin.DeliveryZoneId != parcel.ZoneId)
        {
            throw new InvalidOperationException(
                "Mis-sort: this bin is for a different delivery zone than the parcel. Place the parcel in a bin linked to the correct zone.");
        }

        var binDepotId = bin.StorageAisle.StorageZone.DepotId;
        if (binDepotId != parcel.Zone.DepotId)
        {
            throw new InvalidOperationException(
                "Mis-sort: this bin belongs to a different depot than the parcel. Use a bin in the same depot as the parcel zone.");
        }

        parcel.TransitionTo(ParcelStatus.Sorted);

        var actor = currentUser.UserName ?? currentUser.UserId ?? "System";
        var now = DateTimeOffset.UtcNow;
        var storagePath =
            $"{bin.StorageAisle.StorageZone.Name} / {bin.StorageAisle.Name} / {bin.Name}";
        var locationLabel = parcel.Zone.Depot?.Name ?? parcel.Zone.Name;
        var description = $"Parcel sorted into {storagePath}.";

        var trackingEvent = ParcelTrackingEventFactory.CreateForParcelStatus(
            parcel.Id,
            ParcelStatus.Sorted,
            now,
            locationLabel,
            description,
            actor);

        parcel.TrackingEvents.Add(trackingEvent);
        parcel.LastModifiedAt = now;
        parcel.LastModifiedBy = actor;

        await dbContext.SaveChangesAsync(cancellationToken);
        await parcelUpdateNotifier.NotifyParcelUpdatedAsync(
            new ParcelUpdateNotification(parcel.TrackingNumber, parcel.Status.ToString(), parcel.LastModifiedAt),
            cancellationToken);

        return parcel.ToDto();
    }
}
