using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Zones.DTOs;
using LastMile.TMS.Application.Zones.Queries;
using LastMile.TMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Zones.Queries.Handlers;

public class GetAllZonesQueryHandler(IAppDbContext db) : IRequestHandler<GetAllZonesQuery, List<ZoneDto>>
{
    public async Task<List<ZoneDto>> Handle(GetAllZonesQuery request, CancellationToken cancellationToken)
    {
        var zones = await db.Zones
            .Include(z => z.Depot)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return zones.Select(MapToDto).ToList();
    }

    private static ZoneDto MapToDto(Zone zone) => new(
        zone.Id,
        zone.Name,
        zone.Boundary.AsText(),
        zone.IsActive,
        zone.DepotId,
        zone.Depot?.Name,
        zone.CreatedAt,
        zone.LastModifiedAt);
}
