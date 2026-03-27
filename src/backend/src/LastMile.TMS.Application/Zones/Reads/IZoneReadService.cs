using LastMile.TMS.Application.Zones.DTOs;

namespace LastMile.TMS.Application.Zones.Reads;

public interface IZoneReadService
{
    IQueryable<ZoneDto> GetZones();
    Task<ZoneDto?> GetZoneByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
