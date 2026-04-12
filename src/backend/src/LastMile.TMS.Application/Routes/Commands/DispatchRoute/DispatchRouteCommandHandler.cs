using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class DispatchRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    IParcelUpdateNotifier parcelUpdateNotifier) : IRequestHandler<DispatchRouteCommand, Route?>
{
    public async Task<Route?> Handle(DispatchRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .Include(candidate => candidate.Vehicle)
            .Include(candidate => candidate.Driver)
            .Include(candidate => candidate.Parcels)
            .ThenInclude(parcel => parcel.TrackingEvents)
            .Include(candidate => candidate.Parcels)
            .ThenInclude(parcel => parcel.ChangeHistory)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (route is null)
        {
            return null;
        }

        if (route.Status != RouteStatus.Draft)
        {
            throw new InvalidOperationException("Only draft routes can be dispatched.");
        }

        var hasAssignedDriver = route.DriverId != Guid.Empty
            && await dbContext.Drivers
                .AsNoTracking()
                .AnyAsync(candidate => candidate.Id == route.DriverId, cancellationToken);

        if (!hasAssignedDriver)
        {
            throw new InvalidOperationException("A driver must be assigned before dispatch.");
        }

        var hasAssignedVehicle = route.VehicleId != Guid.Empty
            && await dbContext.Vehicles
                .AsNoTracking()
                .AnyAsync(candidate => candidate.Id == route.VehicleId, cancellationToken);

        if (!hasAssignedVehicle)
        {
            throw new InvalidOperationException("A vehicle must be assigned before dispatch.");
        }

        if (route.Parcels.Count == 0)
        {
            throw new InvalidOperationException("At least one parcel must be assigned before dispatch.");
        }

        var parcelsNotReady = route.Parcels
            .Where(candidate => candidate.Status != ParcelStatus.Loaded)
            .Select(candidate => candidate.TrackingNumber)
            .ToList();

        if (parcelsNotReady.Count > 0)
        {
            throw new InvalidOperationException(
                $"All assigned parcels must be loaded before dispatch. Not ready: {string.Join(", ", parcelsNotReady)}");
        }

        var now = DateTimeOffset.UtcNow;
        var actor = currentUser.UserName ?? currentUser.UserId ?? "System";
        var updatedParcels = new List<Parcel>();
        var vehicleLocation = RouteParcelLifecycleSupport.GetVehicleLocation(route.Vehicle?.RegistrationPlate);

        foreach (var parcel in route.Parcels.Where(candidate => candidate.Status == ParcelStatus.Loaded))
        {
            if (RouteParcelLifecycleSupport.TransitionStatus(
                dbContext,
                parcel,
                ParcelStatus.OutForDelivery,
                now,
                actor,
                vehicleLocation,
                $"Out for delivery on route {route.Id} after dispatch."))
            {
                updatedParcels.Add(parcel);
            }
        }

        route.Status = RouteStatus.Dispatched;
        route.DispatchedAt = now;
        route.LastModifiedAt = now;
        route.LastModifiedBy = actor;

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var parcel in updatedParcels)
        {
            await parcelUpdateNotifier.NotifyParcelUpdatedAsync(
                new ParcelUpdateNotification(parcel.TrackingNumber, parcel.Status.ToString(), parcel.LastModifiedAt),
                cancellationToken);
        }

        return route;
    }
}
