using HotChocolate;
using HotChocolate.Data;
using LastMile.TMS.Application.Zones.DTOs;
using LastMile.TMS.Application.Zones.Reads;

namespace LastMile.TMS.Api.GraphQL.Zones;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ZoneQuery
{
    [UseProjection]
    [UseSorting]
    [UseFiltering]
    public IQueryable<ZoneDto> GetZones(
        [Service] IZoneReadService readService = null!) =>
        readService.GetZones();

    public Task<ZoneDto?> GetZone(
        Guid id,
        [Service] IZoneReadService readService = null!,
        CancellationToken cancellationToken = default) =>
        readService.GetZoneByIdAsync(id, cancellationToken);
}
