using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class LoadParcelForRouteCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IParcelUpdateNotifier parcelUpdateNotifier)
    : IRequestHandler<LoadParcelForRouteCommand, LoadParcelForRouteResultDto>
{
    public async Task<LoadParcelForRouteResultDto> Handle(
        LoadParcelForRouteCommand request,
        CancellationToken cancellationToken)
    {
        var depotId = await InboundReceivingSupport.RequireCurrentDepotIdAsync(db, currentUser, cancellationToken);

        var route = await RouteLoadOutSupport.GetTrackedLoadOutRoutes(db, depotId)
            .Where(r => r.Id == request.RouteId)
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .Include(r => r.Parcels)
            .SingleOrDefaultAsync(cancellationToken);

        if (route is null)
        {
            return new LoadParcelForRouteResultDto
            {
                Outcome = RouteLoadOutScanOutcome.NotExpected,
                Message = "Route was not found for your depot.",
                TrackingNumber = request.Barcode.Trim(),
                Board = new RouteLoadOutBoardDto(),
            };
        }

        var barcode = request.Barcode.Trim();
        var parcel = await db.Parcels
            .Include(p => p.TrackingEvents)
            .SingleOrDefaultAsync(p => p.TrackingNumber == barcode, cancellationToken);

        if (parcel is null)
        {
            return await BuildResultAsync(
                RouteLoadOutScanOutcome.NotExpected,
                "Parcel is not assigned to this route.",
                barcode,
                null,
                route.Id,
                depotId,
                cancellationToken);
        }

        var isAssignedToRoute = route.Parcels.Any(p => p.Id == parcel.Id);
        if (isAssignedToRoute)
        {
            if (parcel.Status == ParcelStatus.Loaded)
            {
                return await BuildResultAsync(
                    RouteLoadOutScanOutcome.AlreadyLoaded,
                    "Parcel is already loaded for this route.",
                    parcel.TrackingNumber,
                    parcel,
                    route.Id,
                    depotId,
                    cancellationToken);
            }

            if (parcel.Status != ParcelStatus.Staged)
            {
                return await BuildResultAsync(
                    RouteLoadOutScanOutcome.InvalidStatus,
                    $"Parcel cannot be loaded from status {parcel.Status}.",
                    parcel.TrackingNumber,
                    parcel,
                    route.Id,
                    depotId,
                    cancellationToken);
            }

            parcel.TransitionTo(ParcelStatus.Loaded);

            var actor = InboundReceivingSupport.GetActor(currentUser);
            var now = DateTimeOffset.UtcNow;
            var trackingEvent = ParcelTrackingEventFactory.CreateForParcelStatus(
                parcel.Id,
                ParcelStatus.Loaded,
                now,
                $"Staging Area {route.StagingArea}",
                $"Loaded onto vehicle {route.Vehicle.RegistrationPlate} for route {route.Id}",
                actor);

            parcel.TrackingEvents.Add(trackingEvent);
            parcel.LastModifiedAt = now;
            parcel.LastModifiedBy = actor;

            await db.SaveChangesAsync(cancellationToken);
            await parcelUpdateNotifier.NotifyParcelUpdatedAsync(
                new ParcelUpdateNotification(parcel.TrackingNumber, parcel.Status.ToString(), parcel.LastModifiedAt),
                cancellationToken);

            return await BuildResultAsync(
                RouteLoadOutScanOutcome.Loaded,
                "Parcel loaded successfully.",
                parcel.TrackingNumber,
                parcel,
                route.Id,
                depotId,
                cancellationToken);
        }

        var conflictingRoute = await db.Routes
            .AsNoTracking()
            .Where(r =>
                r.Id != route.Id
                && (r.Status == RouteStatus.Planned || r.Status == RouteStatus.InProgress)
                && r.Parcels.Any(cp => cp.Id == parcel.Id))
            .Select(r => new { r.Id, r.StagingArea })
            .FirstOrDefaultAsync(cancellationToken);

        if (conflictingRoute is not null)
        {
            return await BuildResultAsync(
                RouteLoadOutScanOutcome.WrongRoute,
                "Parcel is assigned to a different active route.",
                parcel.TrackingNumber,
                parcel,
                route.Id,
                depotId,
                cancellationToken,
                conflictingRoute.Id,
                conflictingRoute.StagingArea);
        }

        return await BuildResultAsync(
            RouteLoadOutScanOutcome.NotExpected,
            "Parcel is not assigned to this route.",
            parcel.TrackingNumber,
            parcel,
            route.Id,
            depotId,
            cancellationToken);
    }

    private async Task<LoadParcelForRouteResultDto> BuildResultAsync(
        RouteLoadOutScanOutcome outcome,
        string message,
        string trackingNumber,
        Parcel? parcel,
        Guid routeId,
        Guid depotId,
        CancellationToken cancellationToken,
        Guid? conflictingRouteId = null,
        StagingArea? conflictingStagingArea = null)
    {
        var board = await RouteLoadOutSupport.LoadBoardAsync(db, routeId, depotId, cancellationToken)
            ?? new RouteLoadOutBoardDto();

        return new LoadParcelForRouteResultDto
        {
            Outcome = outcome,
            Message = message,
            TrackingNumber = trackingNumber,
            ParcelId = parcel?.Id,
            ConflictingRouteId = conflictingRouteId,
            ConflictingStagingArea = conflictingStagingArea,
            Board = board,
        };
    }
}
