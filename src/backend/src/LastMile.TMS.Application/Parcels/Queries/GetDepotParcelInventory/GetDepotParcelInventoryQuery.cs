using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Support;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Queries;

public sealed record GetDepotParcelInventoryQuery(int AgingThresholdMinutes)
    : IRequest<DepotParcelInventoryDashboardDto?>;

public sealed class GetDepotParcelInventoryQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetDepotParcelInventoryQuery, DepotParcelInventoryDashboardDto?>
{
    public async Task<DepotParcelInventoryDashboardDto?> Handle(
        GetDepotParcelInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var depotId = await InboundReceivingSupport.GetCurrentDepotIdAsync(db, currentUser, cancellationToken);
        if (depotId is null || depotId == Guid.Empty)
        {
            return null;
        }

        var depot = await db.Depots
            .AsNoTracking()
            .Where(candidate => candidate.Id == depotId.Value && candidate.IsActive)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Name,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (depot is null)
        {
            return null;
        }

        var thresholdMinutes = Math.Max(1, request.AgingThresholdMinutes);
        var now = DateTimeOffset.UtcNow;
        var thresholdTime = now.AddMinutes(-thresholdMinutes);
        var inventoryParcels = DepotParcelInventorySupport.GetDepotInventoryParcels(db, depot.Id);

        var statusGroups = await inventoryParcels
            .GroupBy(parcel => parcel.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count(),
            })
            .ToListAsync(cancellationToken);

        var zoneCounts = await inventoryParcels
            .GroupBy(parcel => new
            {
                parcel.ZoneId,
                ZoneName = parcel.Zone.Name,
            })
            .Select(group => new DepotParcelInventoryZoneCountDto
            {
                ZoneId = group.Key.ZoneId,
                ZoneName = group.Key.ZoneName,
                Count = group.Count(),
            })
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.ZoneName)
            .ToArrayAsync(cancellationToken);

        var agingCount = await inventoryParcels
            .CountAsync(parcel => (parcel.LastModifiedAt ?? parcel.CreatedAt) <= thresholdTime, cancellationToken);

        var statusLookup = statusGroups.ToDictionary(group => group.Status, group => group.Count);

        return new DepotParcelInventoryDashboardDto
        {
            DepotId = depot.Id,
            DepotName = depot.Name,
            GeneratedAt = now,
            StatusCounts = DepotParcelInventorySupport.InventoryStatuses
                .Select(status => new DepotParcelInventoryStatusCountDto
                {
                    Status = ParcelStatusGraphQl.ToGraphQlName(status),
                    Count = statusLookup.GetValueOrDefault(status, 0),
                })
                .ToArray(),
            ZoneCounts = zoneCounts,
            AgingAlert = new DepotParcelAgingAlertDto
            {
                ThresholdMinutes = thresholdMinutes,
                Count = agingCount,
            },
        };
    }
}
