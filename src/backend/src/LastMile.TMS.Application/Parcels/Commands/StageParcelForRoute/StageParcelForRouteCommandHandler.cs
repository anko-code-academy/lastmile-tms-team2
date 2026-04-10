using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class StageParcelForRouteCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IParcelUpdateNotifier parcelUpdateNotifier)
    : IRequestHandler<StageParcelForRouteCommand, StageParcelForRouteResultDto>
{
    public async Task<StageParcelForRouteResultDto> Handle(
        StageParcelForRouteCommand request,
        CancellationToken cancellationToken)
    {
        var depotId = await InboundReceivingSupport.RequireCurrentDepotIdAsync(db, currentUser, cancellationToken);
        var route = await RouteStagingSupport.GetTrackedActiveDepotRoutes(db, depotId)
            .Where(candidate => candidate.Id == request.RouteId)
            .Include(candidate => candidate.Vehicle)
            .Include(candidate => candidate.Driver)
            .Include(candidate => candidate.Parcels)
            .SingleOrDefaultAsync(cancellationToken);

        if (route is null)
        {
            return new StageParcelForRouteResultDto
            {
                Outcome = RouteStagingScanOutcome.NotExpected,
                Message = "Route was not found for your depot.",
                TrackingNumber = request.Barcode.Trim(),
                Board = new RouteStagingBoardDto()
            };
        }

        var barcode = request.Barcode.Trim();
        var parcel = await db.Parcels
            .Include(candidate => candidate.TrackingEvents)
            .SingleOrDefaultAsync(candidate => candidate.TrackingNumber == barcode, cancellationToken);

        if (parcel is null)
        {
            return await BuildResultAsync(
                RouteStagingScanOutcome.NotExpected,
                "Parcel is not assigned to this route.",
                barcode,
                null,
                route.Id,
                depotId,
                cancellationToken);
        }

        var isAssignedToSelectedRoute = route.Parcels.Any(candidate => candidate.Id == parcel.Id);
        if (isAssignedToSelectedRoute)
        {
            if (parcel.Status == ParcelStatus.Staged)
            {
                return await BuildResultAsync(
                    RouteStagingScanOutcome.AlreadyStaged,
                    "Parcel is already staged for this route.",
                    parcel.TrackingNumber,
                    parcel,
                    route.Id,
                    depotId,
                    cancellationToken);
            }

            if (parcel.Status != ParcelStatus.Sorted)
            {
                return await BuildResultAsync(
                    RouteStagingScanOutcome.InvalidStatus,
                    $"Parcel cannot be staged from status {parcel.Status}.",
                    parcel.TrackingNumber,
                    parcel,
                    route.Id,
                    depotId,
                    cancellationToken);
            }

            parcel.TransitionTo(ParcelStatus.Staged);

            var actor = InboundReceivingSupport.GetActor(currentUser);
            var now = DateTimeOffset.UtcNow;
            var trackingEvent = ParcelTrackingEventFactory.CreateForParcelStatus(
                parcel.Id,
                ParcelStatus.Staged,
                now,
                $"Staging Area {route.StagingArea}",
                $"Staged in area {route.StagingArea} for route {route.Id}",
                actor);

            parcel.TrackingEvents.Add(trackingEvent);
            parcel.LastModifiedAt = now;
            parcel.LastModifiedBy = actor;

            await db.SaveChangesAsync(cancellationToken);
            await parcelUpdateNotifier.NotifyParcelUpdatedAsync(
                new ParcelUpdateNotification(parcel.TrackingNumber, parcel.Status.ToString(), parcel.LastModifiedAt),
                cancellationToken);

            return await BuildResultAsync(
                RouteStagingScanOutcome.Staged,
                "Parcel staged successfully.",
                parcel.TrackingNumber,
                parcel,
                route.Id,
                depotId,
                cancellationToken);
        }

        var conflictingRoute = await db.Routes
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id != route.Id
                && (candidate.Status == RouteStatus.Draft
                    || candidate.Status == RouteStatus.Dispatched
                    || candidate.Status == RouteStatus.InProgress)
                && candidate.Parcels.Any(conflictingParcel => conflictingParcel.Id == parcel.Id))
            .Select(candidate => new
            {
                candidate.Id,
                candidate.StagingArea,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (conflictingRoute is not null)
        {
            return await BuildResultAsync(
                RouteStagingScanOutcome.WrongRoute,
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
            RouteStagingScanOutcome.NotExpected,
            "Parcel is not assigned to this route.",
            parcel.TrackingNumber,
            parcel,
            route.Id,
            depotId,
            cancellationToken);
    }

    private async Task<StageParcelForRouteResultDto> BuildResultAsync(
        RouteStagingScanOutcome outcome,
        string message,
        string trackingNumber,
        Parcel? parcel,
        Guid routeId,
        Guid depotId,
        CancellationToken cancellationToken,
        Guid? conflictingRouteId = null,
        StagingArea? conflictingStagingArea = null)
    {
        var board = await RouteStagingSupport.LoadBoardAsync(db, routeId, depotId, cancellationToken)
            ?? new RouteStagingBoardDto();

        return new StageParcelForRouteResultDto
        {
            Outcome = outcome,
            Message = message,
            TrackingNumber = trackingNumber,
            ParcelId = parcel?.Id,
            ConflictingRouteId = conflictingRouteId,
            ConflictingStagingArea = conflictingStagingArea,
            Board = board
        };
    }
}
