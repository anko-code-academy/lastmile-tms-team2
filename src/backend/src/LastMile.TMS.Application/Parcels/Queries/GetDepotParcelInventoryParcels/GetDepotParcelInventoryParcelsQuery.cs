using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Queries;

public sealed record GetDepotParcelInventoryParcelsQuery(
    int AgingThresholdMinutes,
    ParcelStatus? Status,
    Guid? ZoneId,
    bool AgingOnly,
    int First,
    string? After)
    : IRequest<DepotParcelInventoryParcelConnectionDto>;

public sealed class GetDepotParcelInventoryParcelsQueryHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetDepotParcelInventoryParcelsQuery, DepotParcelInventoryParcelConnectionDto>
{
    public async Task<DepotParcelInventoryParcelConnectionDto> Handle(
        GetDepotParcelInventoryParcelsQuery request,
        CancellationToken cancellationToken)
    {
        var depotId = await InboundReceivingSupport.GetCurrentDepotIdAsync(db, currentUser, cancellationToken);
        if (depotId is null || depotId == Guid.Empty)
        {
            return EmptyConnection();
        }

        var thresholdMinutes = Math.Max(1, request.AgingThresholdMinutes);
        var now = DateTimeOffset.UtcNow;
        var thresholdTime = now.AddMinutes(-thresholdMinutes);
        var first = Math.Clamp(request.First <= 0 ? 20 : request.First, 1, 100);
        var skip = ParseCursor(request.After);

        var query = DepotParcelInventorySupport.GetDepotInventoryParcels(db, depotId.Value);

        if (request.Status.HasValue)
        {
            if (!DepotParcelInventorySupport.IsInventoryStatus(request.Status.Value))
            {
                return EmptyConnection();
            }

            query = query.Where(parcel => parcel.Status == request.Status.Value);
        }

        if (request.ZoneId.HasValue)
        {
            query = query.Where(parcel => parcel.ZoneId == request.ZoneId.Value);
        }

        if (request.AgingOnly)
        {
            query = query.Where(parcel => (parcel.LastModifiedAt ?? parcel.CreatedAt) <= thresholdTime);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return EmptyConnection();
        }

        var items = await query
            .OrderBy(parcel => parcel.LastModifiedAt ?? parcel.CreatedAt)
            .ThenBy(parcel => parcel.TrackingNumber)
            .Skip(skip)
            .Take(first)
            .Select(parcel => new
            {
                parcel.Id,
                parcel.TrackingNumber,
                parcel.Status,
                parcel.ZoneId,
                ZoneName = parcel.Zone.Name,
                LastUpdatedAt = parcel.LastModifiedAt ?? parcel.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return EmptyConnection();
        }

        var endIndex = skip + items.Count;

        return new DepotParcelInventoryParcelConnectionDto
        {
            TotalCount = totalCount,
            PageInfo = new DepotParcelInventoryPageInfoDto
            {
                HasNextPage = endIndex < totalCount,
                HasPreviousPage = skip > 0,
                StartCursor = skip.ToString(),
                EndCursor = endIndex.ToString(),
            },
            Nodes = items
                .Select(item => new DepotParcelInventoryParcelDto
                {
                    Id = item.Id,
                    TrackingNumber = item.TrackingNumber,
                    Status = ParcelStatusGraphQl.ToGraphQlName(item.Status),
                    ZoneId = item.ZoneId,
                    ZoneName = item.ZoneName,
                    LastUpdatedAt = item.LastUpdatedAt,
                    AgeMinutes = Math.Max(0, (int)Math.Floor((now - item.LastUpdatedAt).TotalMinutes)),
                })
                .ToArray(),
        };
    }

    private static DepotParcelInventoryParcelConnectionDto EmptyConnection() =>
        new()
        {
            TotalCount = 0,
            PageInfo = new DepotParcelInventoryPageInfoDto
            {
                HasNextPage = false,
                HasPreviousPage = false,
                StartCursor = null,
                EndCursor = null,
            },
            Nodes = Array.Empty<DepotParcelInventoryParcelDto>(),
        };

    private static int ParseCursor(string? after) =>
        int.TryParse(after, out var offset) && offset > 0 ? offset : 0;
}
