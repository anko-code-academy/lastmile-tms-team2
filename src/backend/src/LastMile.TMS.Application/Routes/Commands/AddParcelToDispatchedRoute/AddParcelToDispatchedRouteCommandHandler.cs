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

public sealed class AddParcelToDispatchedRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    IParcelUpdateNotifier parcelUpdateNotifier,
    IRouteUpdateNotifier routeUpdateNotifier,
    IRoutePlanningService routePlanningService) : IRequestHandler<AddParcelToDispatchedRouteCommand, Route?>
{
    public async Task<Route?> Handle(
        AddParcelToDispatchedRouteCommand request,
        CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .Include(candidate => candidate.Zone)
            .ThenInclude(zone => zone.Depot)
            .ThenInclude(depot => depot.Address)
            .Include(candidate => candidate.Driver)
            .Include(candidate => candidate.Vehicle)
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

        var parcel = await dbContext.Parcels
            .Include(candidate => candidate.Zone)
            .ThenInclude(zone => zone.Depot)
            .Include(candidate => candidate.RecipientAddress)
            .Include(candidate => candidate.ChangeHistory)
            .Include(candidate => candidate.TrackingEvents)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Dto.ParcelId, cancellationToken)
            ?? throw new InvalidOperationException("Parcel not found.");

        var reason = RouteParcelAdjustmentSupport.NormalizeRequiredReason(request.Dto.Reason);
        if (parcel.Status != ParcelStatus.Staged)
        {
            throw new InvalidOperationException("Only staged parcels can be added to a dispatched route.");
        }

        if (parcel.ZoneId != route.ZoneId || parcel.Zone.DepotId != route.Zone.DepotId)
        {
            throw new InvalidOperationException("The parcel must belong to the same zone and depot as the route.");
        }

        var isAssignedToActiveRoute = await dbContext.Routes
            .Where(candidate =>
                candidate.Id != route.Id
                && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(candidate.Status))
            .AnyAsync(
                candidate => candidate.Parcels.Any(assignedParcel => assignedParcel.Id == parcel.Id),
                cancellationToken);

        if (isAssignedToActiveRoute)
        {
            throw new InvalidOperationException("The parcel is already assigned to another active route.");
        }

        await routePlanningService.EnsureParcelRecipientGeocodedAsync(parcel, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var actor = currentUser.UserName ?? currentUser.UserId ?? "System";
        var matchingStop = RouteParcelAdjustmentSupport.FindMatchingStop(route, parcel);
        RouteStop targetStop;

        route.Parcels.Add(parcel);
        if (matchingStop is null)
        {
            targetStop = RouteParcelAdjustmentSupport.CreateStop(route, parcel, now, actor);
            route.Stops.Add(targetStop);
            dbContext.RouteStops.Add(targetStop);
        }
        else
        {
            targetStop = matchingStop;
            targetStop.Parcels.Add(parcel);
            targetStop.LastModifiedAt = now;
            targetStop.LastModifiedBy = actor;
            RouteParcelAdjustmentSupport.RefreshStop(targetStop);
        }

        RouteParcelAdjustmentSupport.ResequenceStops(route);
        await routePlanningService.ApplyMetricsToPersistedRouteAsync(route, cancellationToken);

        var vehicleLocation = RouteParcelLifecycleSupport.GetVehicleLocation(route.Vehicle.RegistrationPlate);
        RouteParcelLifecycleSupport.PromoteToOutForDeliveryFromDispatchedRouteAdjustment(
            dbContext,
            parcel,
            now,
            actor,
            vehicleLocation,
            $"Parcel added to dispatched route {route.Id}. Reason: {reason}");

        var auditEntry = new RouteParcelAdjustmentAuditEntry
        {
            RouteId = route.Id,
            ParcelId = parcel.Id,
            Action = RouteParcelAdjustmentAction.Added,
            TrackingNumber = parcel.TrackingNumber,
            Reason = reason,
            AffectedStopSequence = targetStop.Sequence,
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
                    RouteParcelAdjustmentAction.Added.ToString(),
                    parcel.TrackingNumber,
                    reason,
                    now),
                cancellationToken);
        }

        return route;
    }
}
