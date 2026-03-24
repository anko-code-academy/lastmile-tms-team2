using LastMile.TMS.Application.Zones.DTOs;
using MediatR;

namespace LastMile.TMS.Application.Zones.Commands;

public record CreateZoneCommand(
    string Name,
    Guid DepotId,
    bool IsActive,
    string? GeoJson,
    List<List<double>>? Coordinates,
    string? BoundaryWkt
) : IRequest<ZoneDto>;
