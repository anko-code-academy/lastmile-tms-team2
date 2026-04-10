using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.Mappings;
using LastMile.TMS.Application.Routes.Services;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class CreateRouteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser,
    IRoutePlanningService routePlanningService) : IRequestHandler<CreateRouteCommand, Route>
{
    private static readonly ParcelStatus[] EligibleParcelStatuses = [ParcelStatus.Sorted, ParcelStatus.Staged];

    public async Task<Route> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        var normalizedRouteStartDate = RouteAssignmentSupport.NormalizeUtc(request.Dto.StartDate);

        if (request.Dto.AssignmentMode == RouteAssignmentMode.ManualParcels)
        {
            var manuallySelectedParcelIds = request.Dto.ParcelIds
                .Where(parcelId => parcelId != Guid.Empty)
                .Distinct()
                .ToList();

            var requestedParcels = await dbContext.Parcels
                .AsNoTracking()
                .Where(parcel => manuallySelectedParcelIds.Contains(parcel.Id))
                .Select(parcel => new
                {
                    parcel.Id,
                    parcel.TrackingNumber,
                    parcel.ZoneId,
                    parcel.Status,
                })
                .ToListAsync(cancellationToken);

            if (requestedParcels.Count != manuallySelectedParcelIds.Count)
            {
                throw new InvalidOperationException("One or more parcels not found");
            }

            if (requestedParcels.Any(parcel => parcel.ZoneId != request.Dto.ZoneId))
            {
                throw new InvalidOperationException("One or more parcels do not belong to the selected route zone.");
            }

            var ineligibleParcels = requestedParcels
                .Where(parcel => !EligibleParcelStatuses.Contains(parcel.Status))
                .Select(parcel => parcel.TrackingNumber)
                .OrderBy(trackingNumber => trackingNumber)
                .ToList();

            if (ineligibleParcels.Count > 0)
            {
                throw new InvalidOperationException(
                    $"One or more parcels are not eligible for route assignment: {string.Join(", ", ineligibleParcels)}");
            }

            var requestedParcelIdSet = manuallySelectedParcelIds.ToHashSet();
            var manuallyAssignedParcels = await dbContext.Routes
                .AsNoTracking()
                .Where(route =>
                    RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(route.Status)
                    && route.Parcels.Any(parcel => requestedParcelIdSet.Contains(parcel.Id)))
                .SelectMany(route => route.Parcels
                    .Where(parcel => requestedParcelIdSet.Contains(parcel.Id))
                    .Select(parcel => parcel.TrackingNumber))
                .Distinct()
                .OrderBy(trackingNumber => trackingNumber)
                .ToListAsync(cancellationToken);

            if (manuallyAssignedParcels.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Parcels already assigned to an active route: {string.Join(", ", manuallyAssignedParcels)}");
            }
        }

        var plan = await routePlanningService.BuildPlanAsync(
            new RoutePlanRequest
            {
                ZoneId = request.Dto.ZoneId,
                VehicleId = request.Dto.VehicleId,
                DriverId = request.Dto.DriverId,
                StartDate = request.Dto.StartDate,
                AssignmentMode = request.Dto.AssignmentMode,
                StopMode = request.Dto.StopMode,
                ParcelIds = request.Dto.ParcelIds,
                Stops = request.Dto.Stops,
            },
            cancellationToken);

        var vehicle = await dbContext.Vehicles
            .Include(v => v.Depot)
            .FirstOrDefaultAsync(v => v.Id == request.Dto.VehicleId, cancellationToken);

        if (vehicle is null)
            throw new InvalidOperationException("Vehicle not found");

        if (!RouteAssignmentSupport.IsVehicleAssignableStatus(vehicle.Status))
            throw new InvalidOperationException($"Vehicle is not available. Current status: {vehicle.Status}");

        var plannedParcelIds = plan.Stops
            .SelectMany(stop => stop.Parcels)
            .Select(parcel => parcel.ParcelId)
            .Distinct()
            .ToList();

        if (plannedParcelIds.Count == 0)
        {
            throw new InvalidOperationException("No eligible parcels were selected for the route.");
        }

        var parcels = await dbContext.Parcels
            .Where(p => plannedParcelIds.Contains(p.Id))
            .Include(parcel => parcel.RecipientAddress)
            .ToListAsync(cancellationToken);

        if (parcels.Count != plannedParcelIds.Count)
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

        if (driver.ZoneId != request.Dto.ZoneId)
            throw new InvalidOperationException("Driver must belong to the selected route zone.");

        if (request.Dto.ZoneId != plan.ZoneId)
            throw new InvalidOperationException("Route planning zone mismatch.");

        var requestedParcelIds = plannedParcelIds.ToHashSet();
        var parcelsAlreadyAssigned = await dbContext.Routes
            .AsNoTracking()
            .Where(route =>
                RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(route.Status)
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
        route.Status = RouteStatus.Draft;
        route.ZoneId = request.Dto.ZoneId;
        route.StartDate = normalizedRouteStartDate;
        route.PlannedDistanceMeters = plan.PlannedDistanceMeters;
        route.PlannedDurationSeconds = plan.PlannedDurationSeconds;
        route.PlannedPath = plan.PlannedPath;
        route.CreatedAt = now;
        route.CreatedBy = currentUser.UserName ?? currentUser.UserId;

        dbContext.Routes.Add(route);

        foreach (var parcel in parcels)
        {
            route.Parcels.Add(parcel);
        }

        var parcelById = parcels.ToDictionary(parcel => parcel.Id);
        foreach (var plannedStop in plan.Stops.OrderBy(stop => stop.Sequence))
        {
            var routeStop = new RouteStop
            {
                Id = Guid.NewGuid(),
                RouteId = route.Id,
                Sequence = plannedStop.Sequence,
                RecipientLabel = plannedStop.RecipientLabel,
                Street1 = plannedStop.Parcels.FirstOrDefault()?.AddressLine.Split(',')[0].Trim() ?? string.Empty,
                Street2 = null,
                City = string.Empty,
                State = string.Empty,
                PostalCode = string.Empty,
                CountryCode = string.Empty,
                StopLocation = plannedStop.StopLocation,
                CreatedAt = now,
                CreatedBy = route.CreatedBy,
            };

            if (plannedStop.Parcels.Count > 0)
            {
                var anchorAddress = parcelById[plannedStop.Parcels[0].ParcelId].RecipientAddress;
                routeStop.Street1 = anchorAddress.Street1;
                routeStop.Street2 = anchorAddress.Street2;
                routeStop.City = anchorAddress.City;
                routeStop.State = anchorAddress.State;
                routeStop.PostalCode = anchorAddress.PostalCode;
                routeStop.CountryCode = anchorAddress.CountryCode;
            }

            foreach (var stopParcel in plannedStop.Parcels)
            {
                routeStop.Parcels.Add(parcelById[stopParcel.ParcelId]);
            }

            route.Stops.Add(routeStop);
            dbContext.RouteStops.Add(routeStop);
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
