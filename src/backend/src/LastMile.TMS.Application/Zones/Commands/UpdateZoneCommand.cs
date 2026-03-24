using LastMile.TMS.Application.Zones.DTOs;
using MediatR;

namespace LastMile.TMS.Application.Zones.Commands;

public record UpdateZoneCommand(
    Guid Id,
    string Name,
    Guid DepotId,
    bool IsActive,
    string? GeoJson,
    List<List<double>>? Coordinates,
    string? BoundaryWkt
) : IRequest<ZoneDto?>;
