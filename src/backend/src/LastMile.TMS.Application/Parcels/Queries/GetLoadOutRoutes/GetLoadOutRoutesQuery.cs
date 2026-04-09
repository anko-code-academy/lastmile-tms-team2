using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Queries;

public sealed record GetLoadOutRoutesQuery : IRequest<IReadOnlyList<LoadOutRouteDto>>;

public sealed class GetLoadOutRoutesQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetLoadOutRoutesQuery, IReadOnlyList<LoadOutRouteDto>>
{
    public async Task<IReadOnlyList<LoadOutRouteDto>> Handle(
        GetLoadOutRoutesQuery request,
        CancellationToken cancellationToken)
    {
        var depotId = await InboundReceivingSupport.GetCurrentDepotIdAsync(db, currentUser, cancellationToken);
        if (depotId is null || depotId == Guid.Empty)
        {
            return [];
        }

        return await RouteLoadOutSupport.GetLoadOutRoutes(db, depotId.Value)
            .OrderBy(route => route.StartDate)
            .Select(route => new LoadOutRouteDto
            {
                Id = route.Id,
                VehicleId = route.VehicleId,
                VehiclePlate = route.Vehicle.RegistrationPlate,
                DriverId = route.DriverId,
                DriverName = $"{route.Driver.FirstName} {route.Driver.LastName}".Trim(),
                Status = route.Status,
                StagingArea = route.StagingArea,
                StartDate = route.StartDate,
                ExpectedParcelCount = route.Parcels.Count,
                LoadedParcelCount = route.Parcels.Count(p => p.Status == ParcelStatus.Loaded),
                RemainingParcelCount = route.Parcels.Count(p => p.Status == ParcelStatus.Staged),
            })
            .ToArrayAsync(cancellationToken);
    }
}
