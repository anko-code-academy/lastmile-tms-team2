using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Application.Routes;

public static class RouteMapping
{
    public static RouteDto ToDto(this Route r) => new()
    {
        Id = r.Id,
        VehicleId = r.VehicleId,
        VehiclePlate = r.Vehicle?.RegistrationPlate ?? string.Empty,
        DriverId = r.DriverId,
        DriverName = r.Driver != null ? $"{r.Driver.FirstName} {r.Driver.LastName}" : string.Empty,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        StartMileage = r.StartMileage,
        EndMileage = r.EndMileage,
        TotalMileage = r.TotalMileage,
        Status = r.Status,
        ParcelCount = r.ParcelCount,
        ParcelsDelivered = r.ParcelsDelivered,
        CreatedAt = r.CreatedAt,
    };
}
