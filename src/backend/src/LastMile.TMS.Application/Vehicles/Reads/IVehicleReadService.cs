using LastMile.TMS.Application.Vehicles.DTOs;

namespace LastMile.TMS.Application.Vehicles.Reads;

public interface IVehicleReadService
{
    IQueryable<VehicleDto> GetVehicles();
}
