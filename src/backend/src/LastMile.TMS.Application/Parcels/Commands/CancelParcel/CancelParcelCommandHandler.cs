using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Mappings;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class CancelParcelCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IRequestHandler<CancelParcelCommand, ParcelDetailDto?>
{
    public async Task<ParcelDetailDto?> Handle(CancelParcelCommand request, CancellationToken cancellationToken)
    {
        var parcel = await dbContext.Parcels
            .Include(p => p.RecipientAddress)
            .Include(p => p.ChangeHistory)
            .Include(p => p.TrackingEvents)
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .FirstOrDefaultAsync(p => p.Id == request.ParcelId, cancellationToken);

        if (parcel is null)
        {
            return null;
        }

        if (!parcel.CanCancelBeforeLoad())
        {
            throw new InvalidOperationException(
                $"Parcel {parcel.TrackingNumber} cannot be cancelled while in status {parcel.Status}.");
        }

        var reason = request.Reason.Trim();
        if (reason.Length == 0)
        {
            throw new InvalidOperationException("Cancel reason is required.");
        }

        var actor = currentUser.UserName ?? currentUser.UserId;
        var now = DateTimeOffset.UtcNow;
        var previousStatus = parcel.Status;
        var previousReason = parcel.CancellationReason;

        parcel.TransitionTo(ParcelStatus.Cancelled);
        parcel.CancellationReason = reason;
        parcel.LastModifiedAt = now;
        parcel.LastModifiedBy = actor;
        parcel.RecipientAddress.LastModifiedAt = now;
        parcel.RecipientAddress.LastModifiedBy = actor;

        var statusEntry = new ParcelChangeHistoryEntry
        {
            ParcelId = parcel.Id,
            Action = ParcelChangeAction.Cancelled,
            FieldName = "Status",
            BeforeValue = ParcelChangeSupport.FormatEnum(previousStatus),
            AfterValue = "Cancelled",
            ChangedAt = now,
            ChangedBy = actor,
        };

        var reasonEntry = new ParcelChangeHistoryEntry
        {
            ParcelId = parcel.Id,
            Action = ParcelChangeAction.Cancelled,
            FieldName = "Cancellation reason",
            BeforeValue = string.IsNullOrWhiteSpace(previousReason) ? null : previousReason,
            AfterValue = reason,
            ChangedAt = now,
            ChangedBy = actor,
        };

        parcel.ChangeHistory.Add(statusEntry);
        parcel.ChangeHistory.Add(reasonEntry);
        dbContext.ParcelChangeHistoryEntries.Add(statusEntry);
        dbContext.ParcelChangeHistoryEntries.Add(reasonEntry);

        var locationLabel = parcel.Zone.Depot?.Name ?? parcel.Zone.Name;
        var cancelDescription = $"Parcel cancelled: {reason}";
        if (cancelDescription.Length > 1000)
        {
            cancelDescription = cancelDescription[..1000];
        }

        parcel.TrackingEvents.Add(
            ParcelTrackingEventFactory.CreateForParcelStatus(
                parcel.Id,
                ParcelStatus.Cancelled,
                now,
                locationLabel,
                cancelDescription,
                actor));

        await dbContext.SaveChangesAsync(cancellationToken);

        return parcel.ToDetailDto();
    }
}
