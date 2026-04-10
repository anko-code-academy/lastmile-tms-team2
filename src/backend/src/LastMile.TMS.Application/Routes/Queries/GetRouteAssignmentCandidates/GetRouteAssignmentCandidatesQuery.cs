using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Queries;

public sealed record GetRouteAssignmentCandidatesQuery(
    DateTimeOffset ServiceDate,
    Guid ZoneId,
    Guid? RouteId = null) : IRequest<RouteAssignmentCandidatesDto>;

public sealed class GetRouteAssignmentCandidatesQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetRouteAssignmentCandidatesQuery, RouteAssignmentCandidatesDto>
{
    public async Task<RouteAssignmentCandidatesDto> Handle(
        GetRouteAssignmentCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        Guid? currentVehicleId = null;
        Guid? currentDriverId = null;
        Guid effectiveZoneId = request.ZoneId;

        if (request.RouteId.HasValue)
        {
            var currentRoute = await dbContext.Routes
                .AsNoTracking()
                .Where(route => route.Id == request.RouteId.Value)
                .Select(route => new
                {
                    route.VehicleId,
                    route.DriverId,
                    route.ZoneId,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (currentRoute is null)
            {
                throw new InvalidOperationException("Route not found");
            }

            currentVehicleId = currentRoute.VehicleId;
            currentDriverId = currentRoute.DriverId;
            effectiveZoneId = currentRoute.ZoneId;
        }

        var zone = await dbContext.Zones
            .AsNoTracking()
            .Where(candidate => candidate.Id == effectiveZoneId && candidate.IsActive)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.DepotId,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Zone not found.");

        var serviceDayStart = RouteAssignmentSupport.GetServiceDayStart(request.ServiceDate);
        var serviceDayEnd = RouteAssignmentSupport.GetServiceDayEnd(request.ServiceDate);

        var sameDayRoutes = await dbContext.Routes
            .AsNoTracking()
            .Where(route =>
                route.StartDate >= serviceDayStart
                && route.StartDate < serviceDayEnd
                && (!request.RouteId.HasValue || route.Id != request.RouteId.Value))
            .Select(route => new
            {
                route.Id,
                route.DriverId,
                route.VehicleId,
                route.StartDate,
                route.Status,
                VehiclePlate = route.Vehicle.RegistrationPlate,
            })
            .ToListAsync(cancellationToken);

        var conflictingDriverIds = sameDayRoutes
            .Where(route => RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(route.Status))
            .Select(route => route.DriverId)
            .ToHashSet();

        var conflictingVehicleIds = sameDayRoutes
            .Where(route => RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(route.Status))
            .Select(route => route.VehicleId)
            .ToHashSet();

        var workloadByDriverId = sameDayRoutes
            .GroupBy(route => route.DriverId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DriverWorkloadRouteDto>)group
                    .OrderBy(route => route.StartDate)
                    .Select(route => new DriverWorkloadRouteDto
                    {
                        RouteId = route.Id,
                        VehicleId = route.VehicleId,
                        VehiclePlate = route.VehiclePlate,
                        StartDate = route.StartDate,
                        Status = route.Status,
                    })
                    .ToList());

        var vehicles = await dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.DepotId == zone.DepotId)
            .Select(vehicle => new AssignableVehicleDto
            {
                Id = vehicle.Id,
                RegistrationPlate = vehicle.RegistrationPlate,
                DepotId = vehicle.DepotId,
                DepotName = vehicle.Depot.Name,
                ParcelCapacity = vehicle.ParcelCapacity,
                WeightCapacity = vehicle.WeightCapacity,
                Status = vehicle.Status,
                IsCurrentAssignment = currentVehicleId == vehicle.Id,
            })
            .ToListAsync(cancellationToken);

        var drivers = await dbContext.Drivers
            .AsNoTracking()
            .Include(driver => driver.AvailabilitySchedule)
            .Where(driver => driver.ZoneId == effectiveZoneId && driver.DepotId == zone.DepotId)
            .ToListAsync(cancellationToken);

        var assignableVehicles = vehicles
            .Where(vehicle =>
                vehicle.IsCurrentAssignment
                || (RouteAssignmentSupport.IsVehicleAssignableStatus(vehicle.Status)
                    && !conflictingVehicleIds.Contains(vehicle.Id)))
            .OrderBy(vehicle => vehicle.RegistrationPlate)
            .ToList();

        var assignableDrivers = drivers
            .Where(driver =>
                currentDriverId == driver.Id
                || (driver.Status == DriverStatus.Active
                    && !conflictingDriverIds.Contains(driver.Id)
                    && RouteAssignmentSupport.IsDriverScheduleCompatible(
                        driver.AvailabilitySchedule,
                        request.ServiceDate)))
            .OrderBy(driver => driver.LastName)
            .ThenBy(driver => driver.FirstName)
            .Select(driver => new AssignableDriverDto
            {
                Id = driver.Id,
                DisplayName = RouteAssignmentSupport.FormatDriverName(driver),
                DepotId = driver.DepotId,
                ZoneId = driver.ZoneId,
                Status = driver.Status,
                IsCurrentAssignment = currentDriverId == driver.Id,
                WorkloadRoutes = workloadByDriverId.GetValueOrDefault(driver.Id) ?? [],
            })
            .ToList();

        return new RouteAssignmentCandidatesDto
        {
            Vehicles = assignableVehicles,
            Drivers = assignableDrivers,
        };
    }
}
