using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Zones.Commands;
using LastMile.TMS.Application.Zones.DTOs;
using LastMile.TMS.Application.Zones.Services;
using LastMile.TMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Zones.Commands.Handlers;

public class UpdateZoneCommandHandler(
    IAppDbContext db,
    IZoneBoundaryParser boundaryParser)
    : IRequestHandler<UpdateZoneCommand, ZoneDto?>
{
    public async Task<ZoneDto?> Handle(UpdateZoneCommand request, CancellationToken cancellationToken)
    {
        var zone = await db.Zones
            .Include(z => z.Depot)
            .FirstOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zone is null)
            return null;

        zone.Name = request.Name;
        zone.DepotId = request.DepotId;
        zone.IsActive = request.IsActive;

        if (HasBoundaryInput(request))
        {
            var polygon = ParseBoundary(request);
            if (polygon is null)
                throw new ArgumentException("Failed to parse zone boundary from the provided input.");
            zone.Boundary = polygon;
        }

        await db.SaveChangesAsync(cancellationToken);

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

    private static bool HasBoundaryInput(UpdateZoneCommand request)
    {
        return !string.IsNullOrWhiteSpace(request.GeoJson)
            || (request.Coordinates is { Count: > 0 })
            || !string.IsNullOrWhiteSpace(request.BoundaryWkt);
    }

    private NetTopologySuite.Geometries.Polygon? ParseBoundary(UpdateZoneCommand request)
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
