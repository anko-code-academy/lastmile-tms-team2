using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Zones.Commands;
using LastMile.TMS.Application.Zones.DTOs;
using LastMile.TMS.Application.Zones.Services;
using LastMile.TMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Zones.Commands.Handlers;

public class CreateZoneCommandHandler(
    IAppDbContext db,
    IZoneBoundaryParser boundaryParser)
    : IRequestHandler<CreateZoneCommand, ZoneDto>
{
    public async Task<ZoneDto> Handle(CreateZoneCommand request, CancellationToken cancellationToken)
    {
        var polygon = ParseBoundary(request);
        if (polygon is null)
            throw new ArgumentException("Failed to parse zone boundary from the provided input.");

        var zone = new Zone
        {
            Name = request.Name,
            Boundary = polygon,
            DepotId = request.DepotId,
            IsActive = request.IsActive,
        };

        db.Zones.Add(zone);
        await db.SaveChangesAsync(cancellationToken);

        var depot = await db.Depots
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == zone.DepotId, cancellationToken);

        return new ZoneDto(
            zone.Id,
            zone.Name,
            zone.Boundary.AsText(),
            zone.IsActive,
            zone.DepotId,
            depot?.Name,
            zone.CreatedAt,
            zone.LastModifiedAt);
    }

    private NetTopologySuite.Geometries.Polygon? ParseBoundary(CreateZoneCommand request)
    {
        if (!string.IsNullOrWhiteSpace(request.GeoJson))
            return boundaryParser.ParseGeoJson(request.GeoJson);
        if (request.Coordinates is { Count: > 0 })
            return boundaryParser.ParseCoordinates(request.Coordinates);
        if (!string.IsNullOrWhiteSpace(request.BoundaryWkt))
            return boundaryParser.ParseWkt(request.BoundaryWkt);
        return null;
    }
}
