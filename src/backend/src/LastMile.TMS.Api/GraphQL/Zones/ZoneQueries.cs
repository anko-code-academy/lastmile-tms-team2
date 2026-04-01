using HotChocolate;
using HotChocolate.Data;
using LastMile.TMS.Application.Zones.Reads;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Zones;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ZoneQueries
{
    [UseProjection]
    [UseFiltering(typeof(ZoneFilterInputType))]
    [UseSorting(typeof(ZoneSortInputType))]
    public IQueryable<Zone> GetZones(
        [Service] IZoneReadService readService = null!) =>
        readService.GetZones();

    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Zone> GetZone(
        Guid id,
        [Service] IZoneReadService readService = null!) =>
        readService.GetZones().Where(z => z.Id == id);
}
