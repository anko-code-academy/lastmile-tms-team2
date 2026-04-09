using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.Mappings;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class CreateRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IRequestHandler<CreateRouteCommand, Route>
{
    private static readonly ParcelStatus[] RouteCreationStatuses = [ParcelStatus.Sorted, ParcelStatus.Staged];

    public async Task<Route> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles
            .Include(v => v.Depot)
            .FirstOrDefaultAsync(v => v.Id == request.Dto.VehicleId, cancellationToken);

        if (vehicle is null)
            throw new InvalidOperationException("Vehicle not found");

        if (!RouteAssignmentSupport.IsVehicleAssignableStatus(vehicle.Status))
            throw new InvalidOperationException($"Vehicle is not available. Current status: {vehicle.Status}");

        var parcels = await dbContext.Parcels
            .Where(p => request.Dto.ParcelIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (parcels.Count != request.Dto.ParcelIds.Count)
            throw new InvalidOperationException("One or more parcels not found");

        var totalParcelCount = parcels.Count;
        if (totalParcelCount > vehicle.ParcelCapacity)
        {
            throw new InvalidOperationException(
                $"Parcel capacity exceeded. Vehicle capacity: {vehicle.ParcelCapacity}, Requested: {totalParcelCount}");
        }

        var totalWeightKg = RouteAssignmentSupport.GetTotalWeightKg(parcels);
        if (totalWeightKg > vehicle.WeightCapacity)
        {
            throw new InvalidOperationException(
                $"Weight capacity exceeded. Vehicle capacity: {vehicle.WeightCapacity}kg, Requested: {totalWeightKg:F2}kg");
        }

        var driver = await dbContext.Drivers
            .Include(d => d.User)
            .Include(d => d.AvailabilitySchedule)
            .FirstOrDefaultAsync(d => d.Id == request.Dto.DriverId, cancellationToken);

        if (driver is null)
            throw new InvalidOperationException("Driver not found");

        if (driver.Status != DriverStatus.Active)
            throw new InvalidOperationException($"Driver is not available. Current status: {driver.Status}");

        if (driver.DepotId != vehicle.DepotId)
            throw new InvalidOperationException("Driver and vehicle must belong to the same depot.");

        if (!RouteAssignmentSupport.IsDriverScheduleCompatible(driver.AvailabilitySchedule, request.Dto.StartDate))
            throw new InvalidOperationException("Driver is not available for the route service date.");

        if (parcels.Any(parcel => !RouteCreationStatuses.Contains(parcel.Status)))
            throw new InvalidOperationException("Only parcels with status Sorted or Staged can be assigned to a route.");

        if (parcels.Any(parcel => parcel.ZoneId != driver.ZoneId))
            throw new InvalidOperationException("All route parcels must belong to the driver's zone.");

        var requestedParcelIds = request.Dto.ParcelIds.ToHashSet();
        var parcelsAlreadyAssigned = await dbContext.Routes
            .AsNoTracking()
            .Where(route =>
                (route.Status == RouteStatus.Planned || route.Status == RouteStatus.InProgress)
                && route.Parcels.Any(parcel => requestedParcelIds.Contains(parcel.Id)))
            .SelectMany(route => route.Parcels
                .Where(parcel => requestedParcelIds.Contains(parcel.Id))
                .Select(parcel => parcel.TrackingNumber))
            .Distinct()
            .ToListAsync(cancellationToken);

        if (parcelsAlreadyAssigned.Count > 0)
            throw new InvalidOperationException(
                $"Parcels already assigned to an active route: {string.Join(", ", parcelsAlreadyAssigned)}");

        var serviceDayStart = RouteAssignmentSupport.GetServiceDayStart(request.Dto.StartDate);
        var serviceDayEnd = RouteAssignmentSupport.GetServiceDayEnd(request.Dto.StartDate);
        var driverHasSameDayRoute = await dbContext.Routes
            .AsNoTracking()
            .AnyAsync(
                r => r.DriverId == request.Dto.DriverId
                    && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(r.Status)
                    && r.StartDate >= serviceDayStart
                    && r.StartDate < serviceDayEnd,
                cancellationToken);

        if (driverHasSameDayRoute)
            throw new InvalidOperationException(
                "Driver is already assigned to another planned or in-progress route on that service date.");

        var vehicleHasSameDayRoute = await dbContext.Routes
            .AsNoTracking()
            .AnyAsync(
                r => r.VehicleId == request.Dto.VehicleId
                    && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(r.Status)
                    && r.StartDate >= serviceDayStart
                    && r.StartDate < serviceDayEnd,
                cancellationToken);

        if (vehicleHasSameDayRoute)
            throw new InvalidOperationException(
                "Vehicle is already assigned to another planned or in-progress route on that service date.");

        var now = DateTimeOffset.UtcNow;
        var route = request.Dto.ToEntity();
        if (route.Id == Guid.Empty)
        {
            route.Id = Guid.NewGuid();
        }
        route.Status = RouteStatus.Planned;
        route.CreatedAt = now;
        route.CreatedBy = currentUser.UserName ?? currentUser.UserId;

        dbContext.Routes.Add(route);

        foreach (var parcel in parcels)
        {
            route.Parcels.Add(parcel);
        }

        route.AssignmentAuditTrail.Add(
            RouteAssignmentSupport.CreateAuditEntry(
                route.Id,
                RouteAssignmentAuditAction.Assigned,
                driver,
                vehicle,
                route.CreatedBy));

        vehicle.Status = VehicleStatus.InUse;

        await dbContext.SaveChangesAsync(cancellationToken);

        route.Vehicle = vehicle;
        route.Driver = driver;
        return route;
    }
}
