using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Vehicles.DTOs;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Vehicles.Reads;

public sealed class VehicleReadService(IAppDbContext dbContext) : IVehicleReadService
{
    public IQueryable<VehicleDto> GetVehicles() =>
        dbContext.Vehicles
            .AsNoTracking()
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                RegistrationPlate = v.RegistrationPlate,
                Type = v.Type,
                ParcelCapacity = v.ParcelCapacity,
                WeightCapacity = v.WeightCapacity,
                Status = v.Status,
                DepotId = v.DepotId,
                DepotName = v.Depot != null ? v.Depot.Name : string.Empty,
                TotalRoutes = dbContext.Routes.Count(r => r.VehicleId == v.Id),
                RoutesCompleted = dbContext.Routes.Count(r => r.VehicleId == v.Id && r.Status == RouteStatus.Completed),
                TotalMileage = dbContext.Routes
                    .Where(r => r.VehicleId == v.Id && r.Status == RouteStatus.Completed && r.EndMileage > 0)
                    .Sum(r => r.EndMileage - r.StartMileage),
                CreatedAt = v.CreatedAt,
                LastModifiedAt = v.LastModifiedAt,
            });
}
