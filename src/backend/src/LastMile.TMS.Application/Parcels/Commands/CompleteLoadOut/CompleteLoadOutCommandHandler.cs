using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class CompleteLoadOutCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CompleteLoadOutCommand, CompleteLoadOutResultDto>
{
    public async Task<CompleteLoadOutResultDto> Handle(
        CompleteLoadOutCommand request,
        CancellationToken cancellationToken)
    {
        var depotId = await InboundReceivingSupport.RequireCurrentDepotIdAsync(db, currentUser, cancellationToken);

        var route = await db.Routes
            .Where(r => r.Vehicle.DepotId == depotId && r.Id == request.RouteId)
            .Include(r => r.Parcels)
            .SingleOrDefaultAsync(cancellationToken);

        if (route is null)
        {
            return new CompleteLoadOutResultDto
            {
                Success = false,
                Message = "Route was not found for your depot.",
            };
        }

        if (route.Status != RouteStatus.Planned)
        {
            return new CompleteLoadOutResultDto
            {
                Success = false,
                Message = $"Route must be in Planned status to complete load-out. Current status: {route.Status}.",
                Board = await RouteLoadOutSupport.LoadBoardAsync(db, route.Id, depotId, cancellationToken)
                        ?? new RouteLoadOutBoardDto(),
            };
        }

        var loadedCount = route.Parcels.Count(p => p.Status == ParcelStatus.Loaded);
        var skippedCount = route.Parcels.Count(p => p.Status == ParcelStatus.Staged);
        var totalCount = route.Parcels.Count;

        if (skippedCount > 0 && !request.Force)
        {
            return new CompleteLoadOutResultDto
            {
                Success = false,
                Message = $"{skippedCount} of {totalCount} parcels have not been loaded. Force complete to proceed anyway.",
                LoadedCount = loadedCount,
                SkippedCount = skippedCount,
                TotalCount = totalCount,
                Board = await RouteLoadOutSupport.LoadBoardAsync(db, route.Id, depotId, cancellationToken)
                        ?? new RouteLoadOutBoardDto(),
            };
        }

        route.Status = RouteStatus.InProgress;
        route.LastModifiedAt = DateTimeOffset.UtcNow;
        route.LastModifiedBy = InboundReceivingSupport.GetActor(currentUser);

        var actor = InboundReceivingSupport.GetActor(currentUser);
        var now = DateTimeOffset.UtcNow;

        foreach (var parcel in route.Parcels.Where(p => p.Status == ParcelStatus.Staged))
        {
            parcel.TransitionTo(ParcelStatus.Exception);
            parcel.LastModifiedAt = now;
            parcel.LastModifiedBy = actor;

            var trackingEvent = ParcelTrackingEventFactory.CreateForParcelStatus(
                parcel.Id,
                ParcelStatus.Exception,
                now,
                $"Staging Area {route.StagingArea}",
                $"Force completed load-out: parcel was not loaded onto vehicle.",
                actor);

            parcel.TrackingEvents.Add(trackingEvent);
        }

        await db.SaveChangesAsync(cancellationToken);

        var board = await RouteLoadOutSupport.LoadBoardAsync(db, route.Id, depotId, cancellationToken)
                    ?? new RouteLoadOutBoardDto();

        return new CompleteLoadOutResultDto
        {
            Success = true,
            Message = "Load-out completed. Route is now in progress.",
            LoadedCount = loadedCount,
            SkippedCount = skippedCount,
            TotalCount = totalCount,
            Board = board,
        };
    }
}
