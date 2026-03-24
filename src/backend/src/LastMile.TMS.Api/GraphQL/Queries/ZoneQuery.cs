using HotChocolate;
using LastMile.TMS.Application.Zones.DTOs;
using LastMile.TMS.Application.Zones.Queries;
using MediatR;

namespace LastMile.TMS.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class ZoneQuery
{
    public async Task<List<ZoneDto>> GetZones(
        [Service] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetAllZonesQuery(), cancellationToken);
    }

    public async Task<ZoneDto?> GetZone(
        Guid id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetZoneByIdQuery(id), cancellationToken);
    }
}
