using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.Mappings;
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

        if (vehicle.Status != VehicleStatus.Available)
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

        var totalWeightKg = parcels.Sum(p => p.WeightUnit == WeightUnit.Lb ? p.Weight * 0.453592m : p.Weight);
        if (totalWeightKg > vehicle.WeightCapacity)
        {
            throw new InvalidOperationException(
                $"Weight capacity exceeded. Vehicle capacity: {vehicle.WeightCapacity}kg, Requested: {totalWeightKg:F2}kg");
        }

        var driver = await dbContext.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == request.Dto.DriverId, cancellationToken);

        if (driver is null)
            throw new InvalidOperationException("Driver not found");

        if (driver.DepotId != vehicle.DepotId)
            throw new InvalidOperationException("Driver and vehicle must belong to the same depot.");

        if (parcels.Any(parcel => !RouteCreationStatuses.Contains(parcel.Status)))
            throw new InvalidOperationException("Only parcels with status Sorted or Staged can be assigned to a route.");

        if (parcels.Any(parcel => parcel.ZoneId != driver.ZoneId))
            throw new InvalidOperationException("All route parcels must belong to the driver's zone.");

        var requestedStart = request.Dto.StartDate;
        var requestedEndExclusive = DateTimeOffset.MaxValue;
        var driverHasOverlappingRoute = await dbContext.Routes
            .AsNoTracking()
            .AnyAsync(
                r => r.DriverId == request.Dto.DriverId
                    && (r.Status == RouteStatus.Planned || r.Status == RouteStatus.InProgress)
                    && r.StartDate < requestedEndExclusive
                    && requestedStart < (r.EndDate ?? DateTimeOffset.MaxValue),
                cancellationToken);

        if (driverHasOverlappingRoute)
            throw new InvalidOperationException(
                "Driver is already assigned to a planned or in-progress route that overlaps the requested time.");

        var vehicleHasOverlappingRoute = await dbContext.Routes
            .AsNoTracking()
            .AnyAsync(
                r => r.VehicleId == request.Dto.VehicleId
                    && (r.Status == RouteStatus.Planned || r.Status == RouteStatus.InProgress)
                    && r.StartDate < requestedEndExclusive
                    && requestedStart < (r.EndDate ?? DateTimeOffset.MaxValue),
                cancellationToken);

        if (vehicleHasOverlappingRoute)
            throw new InvalidOperationException(
                "Vehicle is already assigned to a planned or in-progress route that overlaps the requested time.");

        var now = DateTimeOffset.UtcNow;
        var route = request.Dto.ToEntity();
        route.Status = RouteStatus.Planned;
        route.CreatedAt = now;
        route.CreatedBy = currentUser.UserName ?? currentUser.UserId;

        dbContext.Routes.Add(route);

        foreach (var parcel in parcels)
        {
            route.Parcels.Add(parcel);
            if (parcel.Status == ParcelStatus.Sorted || parcel.Status == ParcelStatus.Staged)
            {
                parcel.Status = ParcelStatus.Loaded;
            }
        }

        vehicle.Status = VehicleStatus.InUse;

        await dbContext.SaveChangesAsync(cancellationToken);

        route.Vehicle = vehicle;
        route.Driver = driver;
        return route;
    }
}
