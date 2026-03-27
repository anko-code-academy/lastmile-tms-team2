using System.Linq.Expressions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Zones.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Zones.Reads;

public sealed class ZoneReadService(IAppDbContext dbContext) : IZoneReadService
{
    public IQueryable<ZoneDto> GetZones() =>
        dbContext.Zones
            .AsNoTracking()
            .Select(MapToDtoExpression());

    public async Task<ZoneDto?> GetZoneByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var zone = await dbContext.Zones
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

        return zone is null ? null : MapToDto(zone);
    }

    private static ZoneDto MapToDto(Domain.Entities.Zone z) => new(
        z.Id,
        z.Name,
        z.Boundary.AsText(),
        z.IsActive,
        z.DepotId,
        z.Depot?.Name,
        z.CreatedAt,
        z.LastModifiedAt);

    private static Expression<Func<Domain.Entities.Zone, ZoneDto>> MapToDtoExpression() =>
        z => new ZoneDto(
            z.Id,
            z.Name,
            z.Boundary.AsText(),
            z.IsActive,
            z.DepotId,
            z.Depot != null ? z.Depot.Name : null,
            z.CreatedAt,
            z.LastModifiedAt);
}
