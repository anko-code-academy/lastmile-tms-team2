namespace LastMile.TMS.Application.Zones.DTOs;

public record ZoneDto(
    Guid Id,
    string Name,
    string Boundary,
    bool IsActive,
    Guid DepotId,
    string? DepotName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
