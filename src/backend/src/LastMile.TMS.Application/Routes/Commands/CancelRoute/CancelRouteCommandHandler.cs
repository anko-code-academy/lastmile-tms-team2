using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class CancelRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    IParcelUpdateNotifier parcelUpdateNotifier) : IRequestHandler<CancelRouteCommand, Route?>
{
    public async Task<Route?> Handle(CancelRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .Include(candidate => candidate.Vehicle)
            .ThenInclude(vehicle => vehicle.Depot)
            .Include(candidate => candidate.Parcels)
            .ThenInclude(parcel => parcel.TrackingEvents)
            .Include(candidate => candidate.Parcels)
            .ThenInclude(parcel => parcel.ChangeHistory)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (route is null)
        {
            return null;
        }

        if (route.Status != RouteStatus.Draft && route.Status != RouteStatus.Dispatched)
        {
            throw new InvalidOperationException("Only draft or dispatched routes can be cancelled before route start.");
        }

        var reason = request.Dto.Reason.Trim();
        if (reason.Length == 0)
        {
            throw new InvalidOperationException("Cancellation reason is required.");
        }

        if (reason.Length > 1000)
        {
            throw new InvalidOperationException("Cancellation reason must not exceed 1000 characters.");
        }

        var actor = currentUser.UserName ?? currentUser.UserId;
        var now = DateTimeOffset.UtcNow;

        route.Status = RouteStatus.Cancelled;
        route.CancellationReason = reason;
        route.LastModifiedAt = now;
        route.LastModifiedBy = actor;

        var revertedParcels = route.Parcels
            .Where(parcel => parcel.Status is ParcelStatus.Staged or ParcelStatus.Loaded or ParcelStatus.OutForDelivery)
            .ToList();

        foreach (var parcel in revertedParcels)
        {
            var previousStatus = parcel.Status;
            parcel.ReturnToSortedFromCancelledRoute();
            parcel.LastModifiedAt = now;
            parcel.LastModifiedBy = actor;

            var historyEntry = new ParcelChangeHistoryEntry
            {
                ParcelId = parcel.Id,
                Action = ParcelChangeAction.Updated,
                FieldName = "Status",
                BeforeValue = ParcelChangeSupport.FormatEnum(previousStatus),
                AfterValue = ParcelChangeSupport.FormatEnum(ParcelStatus.Sorted),
                ChangedAt = now,
                ChangedBy = actor,
            };

            parcel.ChangeHistory.Add(historyEntry);
            dbContext.ParcelChangeHistoryEntries.Add(historyEntry);

            var revertDescription =
                $"Returned to sorted after route {route.Id} was cancelled: {reason}";
            if (revertDescription.Length > 1000)
            {
                revertDescription = revertDescription[..1000];
            }

            parcel.TrackingEvents.Add(
                ParcelTrackingEventFactory.CreateForParcelStatus(
                    parcel.Id,
                    ParcelStatus.Sorted,
                    now,
                    route.Vehicle.Depot?.Name ?? $"Staging Area {route.StagingArea}",
                    revertDescription,
                    actor));
        }

        var vehicleHasOtherActiveRoutes = await dbContext.Routes
            .AsNoTracking()
            .AnyAsync(
                candidate => candidate.Id != route.Id
                    && candidate.VehicleId == route.VehicleId
                    && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(candidate.Status),
                cancellationToken);

        if (!vehicleHasOtherActiveRoutes)
        {
            route.Vehicle.Status = VehicleStatus.Available;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var parcel in revertedParcels)
        {
            await parcelUpdateNotifier.NotifyParcelUpdatedAsync(
                new ParcelUpdateNotification(parcel.TrackingNumber, parcel.Status.ToString(), parcel.LastModifiedAt),
                cancellationToken);
        }

        return route;
    }
}
