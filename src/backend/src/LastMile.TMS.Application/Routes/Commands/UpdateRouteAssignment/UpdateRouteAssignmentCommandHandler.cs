using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class UpdateRouteAssignmentCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IRequestHandler<UpdateRouteAssignmentCommand, Route?>
{
    public async Task<Route?> Handle(
        UpdateRouteAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .Include(r => r.Parcels)
            .Include(r => r.AssignmentAuditTrail)
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (route is null)
        {
            return null;
        }

        if (route.Status != RouteStatus.Planned)
        {
            throw new InvalidOperationException(
                "Only planned routes can be reassigned before dispatch.");
        }

        if (route.DriverId == request.Dto.DriverId && route.VehicleId == request.Dto.VehicleId)
        {
            return route;
        }

        var driver = await dbContext.Drivers
            .Include(d => d.AvailabilitySchedule)
            .FirstOrDefaultAsync(d => d.Id == request.Dto.DriverId, cancellationToken);

        if (driver is null)
        {
            throw new InvalidOperationException("Driver not found");
        }

        if (driver.Status != DriverStatus.Active)
        {
            throw new InvalidOperationException(
                $"Driver is not available. Current status: {driver.Status}");
        }

        if (!RouteAssignmentSupport.IsDriverScheduleCompatible(driver.AvailabilitySchedule, route.StartDate))
        {
            throw new InvalidOperationException(
                "Driver is not available for the route service date.");
        }

        var vehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.Dto.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            throw new InvalidOperationException("Vehicle not found");
        }

        if (!RouteAssignmentSupport.IsVehicleAssignableStatus(vehicle.Status))
        {
            throw new InvalidOperationException(
                $"Vehicle is not available. Current status: {vehicle.Status}");
        }

        if (driver.DepotId != vehicle.DepotId)
        {
            throw new InvalidOperationException("Driver and vehicle must belong to the same depot.");
        }

        if (route.Parcels.Any(parcel => parcel.ZoneId != driver.ZoneId))
        {
            throw new InvalidOperationException(
                "All route parcels must belong to the driver's zone.");
        }

        if (!RouteAssignmentSupport.DoParcelsFitVehicle(route.Parcels.ToList(), vehicle))
        {
            throw new InvalidOperationException(
                "Route parcels exceed the selected vehicle capacity.");
        }

        var serviceDayStart = RouteAssignmentSupport.GetServiceDayStart(route.StartDate);
        var serviceDayEnd = RouteAssignmentSupport.GetServiceDayEnd(route.StartDate);

        var driverHasSameDayRoute = await dbContext.Routes
            .AsNoTracking()
            .AnyAsync(
                r => r.Id != route.Id
                    && r.DriverId == driver.Id
                    && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(r.Status)
                    && r.StartDate >= serviceDayStart
                    && r.StartDate < serviceDayEnd,
                cancellationToken);

        if (driverHasSameDayRoute)
        {
            throw new InvalidOperationException(
                "Driver is already assigned to another planned or in-progress route on that service date.");
        }

        var vehicleHasSameDayRoute = await dbContext.Routes
            .AsNoTracking()
            .AnyAsync(
                r => r.Id != route.Id
                    && r.VehicleId == vehicle.Id
                    && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(r.Status)
                    && r.StartDate >= serviceDayStart
                    && r.StartDate < serviceDayEnd,
                cancellationToken);

        if (vehicleHasSameDayRoute)
        {
            throw new InvalidOperationException(
                "Vehicle is already assigned to another planned or in-progress route on that service date.");
        }

        var previousDriver = route.Driver;
        var previousVehicle = route.Vehicle;

        var auditEntry = RouteAssignmentSupport.CreateAuditEntry(
            route.Id,
            RouteAssignmentAuditAction.Reassigned,
            driver,
            vehicle,
            currentUser.UserName ?? currentUser.UserId,
            previousDriver,
            previousVehicle);

        route.Driver = driver;
        route.DriverId = driver.Id;
        route.Vehicle = vehicle;
        route.VehicleId = vehicle.Id;
        route.LastModifiedAt = DateTimeOffset.UtcNow;
        route.LastModifiedBy = currentUser.UserName ?? currentUser.UserId;
        route.AssignmentAuditTrail.Add(auditEntry);
        dbContext.RouteAssignmentAuditEntries.Add(auditEntry);

        vehicle.Status = VehicleStatus.InUse;

        if (previousVehicle.Id != vehicle.Id)
        {
            var previousVehicleHasOtherActiveRoutes = await dbContext.Routes
                .AsNoTracking()
                .AnyAsync(
                    r => r.Id != route.Id
                        && r.VehicleId == previousVehicle.Id
                        && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(r.Status),
                    cancellationToken);

            if (!previousVehicleHasOtherActiveRoutes)
            {
                previousVehicle.Status = VehicleStatus.Available;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return route;
    }
}
