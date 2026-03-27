using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Reads;

public sealed class RouteReadService(IAppDbContext dbContext) : IRouteReadService
{
    public IQueryable<RouteDto> GetRoutes() =>
        dbContext.Routes
            .AsNoTracking()
            .Select(r => new RouteDto
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                VehiclePlate = r.Vehicle != null ? r.Vehicle.RegistrationPlate : string.Empty,
                DriverId = r.DriverId,
                DriverName = r.Driver != null ? r.Driver.FirstName + " " + r.Driver.LastName : string.Empty,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                StartMileage = r.StartMileage,
                EndMileage = r.EndMileage,
                TotalMileage = r.EndMileage > 0 ? r.EndMileage - r.StartMileage : 0,
                Status = r.Status,
                ParcelCount = r.Parcels.Count,
                ParcelsDelivered = r.Parcels.Count(p => p.Status == ParcelStatus.Delivered),
                CreatedAt = r.CreatedAt,
            });
}
