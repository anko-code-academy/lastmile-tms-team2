using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Support;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Queries;

public sealed record GetRouteLoadOutBoardQuery(Guid RouteId) : IRequest<RouteLoadOutBoardDto?>;

public sealed class GetRouteLoadOutBoardQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetRouteLoadOutBoardQuery, RouteLoadOutBoardDto?>
{
    public async Task<RouteLoadOutBoardDto?> Handle(
        GetRouteLoadOutBoardQuery request,
        CancellationToken cancellationToken)
    {
        var depotId = await InboundReceivingSupport.GetCurrentDepotIdAsync(db, currentUser, cancellationToken);
        if (depotId is null || depotId == Guid.Empty)
        {
            return null;
        }

        return await RouteLoadOutSupport.LoadBoardAsync(db, request.RouteId, depotId.Value, cancellationToken);
    }
}
