using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Application.Routes.Services;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class RemoveParcelFromDispatchedRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    IParcelUpdateNotifier parcelUpdateNotifier,
    IRouteUpdateNotifier routeUpdateNotifier,
    IRoutePlanningService routePlanningService)
    : IRequestHandler<RemoveParcelFromDispatchedRouteCommand, Route?>
{
    public async Task<Route?> Handle(
        RemoveParcelFromDispatchedRouteCommand request,
        CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .Include(candidate => candidate.Zone)
            .ThenInclude(zone => zone.Depot)
            .ThenInclude(depot => depot.Address)
            .Include(candidate => candidate.Driver)
            .Include(candidate => candidate.Parcels)
            .ThenInclude(parcel => parcel.RecipientAddress)
            .Include(candidate => candidate.Parcels)
            .ThenInclude(parcel => parcel.ChangeHistory)
            .Include(candidate => candidate.Parcels)
            .ThenInclude(parcel => parcel.TrackingEvents)
            .Include(candidate => candidate.Stops)
            .ThenInclude(stop => stop.Parcels)
            .ThenInclude(parcel => parcel.RecipientAddress)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (route is null)
        {
            return null;
        }

        if (route.Status != RouteStatus.Dispatched)
        {
            throw new InvalidOperationException("Only dispatched routes can be adjusted.");
        }

        var reason = RouteParcelAdjustmentSupport.NormalizeRequiredReason(request.Dto.Reason);
        var parcel = route.Parcels.FirstOrDefault(candidate => candidate.Id == request.Dto.ParcelId)
            ?? throw new InvalidOperationException("The parcel is not assigned to this route.");

        if (route.Parcels.Count == 1)
        {
            throw new InvalidOperationException("The final parcel cannot be removed from a dispatched route. Cancel the route instead.");
        }

        var stop = route.Stops
            .OrderBy(candidate => candidate.Sequence)
            .FirstOrDefault(candidate => candidate.Parcels.Any(stopParcel => stopParcel.Id == parcel.Id))
            ?? throw new InvalidOperationException("The parcel is not assigned to any persisted stop on this route.");

        stop.Parcels.Remove(parcel);
        route.Parcels.Remove(parcel);

        var now = DateTimeOffset.UtcNow;
        var actor = currentUser.UserName ?? currentUser.UserId ?? "System";

        int? affectedStopSequence = null;
        if (stop.Parcels.Count == 0)
        {
            route.Stops.Remove(stop);
            dbContext.RouteStops.Remove(stop);
        }
        else
        {
            stop.LastModifiedAt = now;
            stop.LastModifiedBy = actor;
            RouteParcelAdjustmentSupport.RefreshStop(stop);
            affectedStopSequence = stop.Sequence;
        }

        RouteParcelAdjustmentSupport.ResequenceStops(route);
        if (affectedStopSequence.HasValue)
        {
            affectedStopSequence = route.Stops
                .Where(candidate => candidate.Id == stop.Id)
                .Select(candidate => (int?)candidate.Sequence)
                .SingleOrDefault();
        }

        await routePlanningService.ApplyMetricsToPersistedRouteAsync(route, cancellationToken);

        RouteParcelLifecycleSupport.ReturnToStagedFromDispatchedRouteAdjustment(
            dbContext,
            parcel,
            now,
            actor,
            route.Zone.Depot.Name,
            $"Parcel removed from dispatched route {route.Id}. Reason: {reason}");

        var auditEntry = new RouteParcelAdjustmentAuditEntry
        {
            RouteId = route.Id,
            ParcelId = parcel.Id,
            Action = RouteParcelAdjustmentAction.Removed,
            TrackingNumber = parcel.TrackingNumber,
            Reason = reason,
            AffectedStopSequence = affectedStopSequence,
            ChangedAt = now,
            ChangedBy = actor,
        };

        route.ParcelAdjustmentAuditTrail.Add(auditEntry);
        dbContext.RouteParcelAdjustmentAuditEntries.Add(auditEntry);
        route.LastModifiedAt = now;
        route.LastModifiedBy = actor;

        await dbContext.SaveChangesAsync(cancellationToken);

        await parcelUpdateNotifier.NotifyParcelUpdatedAsync(
            new ParcelUpdateNotification(parcel.TrackingNumber, parcel.Status.ToString(), parcel.LastModifiedAt),
            cancellationToken);

        if (route.Driver.UserId != Guid.Empty)
        {
            await routeUpdateNotifier.NotifyRouteUpdatedAsync(
                new RouteUpdateNotification(
                    route.Driver.UserId,
                    route.Id,
                    RouteParcelAdjustmentAction.Removed.ToString(),
                    parcel.TrackingNumber,
                    reason,
                    now),
                cancellationToken);
        }

        return route;
    }
}
