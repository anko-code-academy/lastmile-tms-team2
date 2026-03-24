using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Zones.DTOs;
using LastMile.TMS.Application.Zones.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Zones.Queries.Handlers;

public class GetZoneByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetZoneByIdQuery, ZoneDto?>
{
    public async Task<ZoneDto?> Handle(GetZoneByIdQuery request, CancellationToken cancellationToken)
    {
        var zone = await db.Zones
            .Include(z => z.Depot)
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zone is null)
            return null;

        return new ZoneDto(
            zone.Id,
            zone.Name,
            zone.Boundary.AsText(),
            zone.IsActive,
            zone.DepotId,
            zone.Depot?.Name,
            zone.CreatedAt,
            zone.LastModifiedAt);
    }
}
