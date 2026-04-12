using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Application.Routes.Support;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Routes.Queries;

public sealed record GetDispatchedRouteParcelCandidatesQuery(Guid RouteId)
    : IRequest<IReadOnlyList<RouteParcelAdjustmentCandidateDto>>;

public sealed class GetDispatchedRouteParcelCandidatesQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetDispatchedRouteParcelCandidatesQuery, IReadOnlyList<RouteParcelAdjustmentCandidateDto>>
{
    public async Task<IReadOnlyList<RouteParcelAdjustmentCandidateDto>> Handle(
        GetDispatchedRouteParcelCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        var route = await dbContext.Routes
            .Include(candidate => candidate.Zone)
            .ThenInclude(zone => zone.Depot)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.RouteId, cancellationToken)
            ?? throw new InvalidOperationException("Route not found.");

        if (route.Status != RouteStatus.Dispatched)
        {
            throw new InvalidOperationException("Only dispatched routes can load adjustment candidates.");
        }

        var candidateIdsAssignedToActiveRoutes = await dbContext.Routes
            .Where(candidate =>
                candidate.Id != route.Id
                && RouteAssignmentSupport.ActiveAssignmentStatuses.Contains(candidate.Status))
            .SelectMany(candidate => candidate.Parcels.Select(parcel => parcel.Id))
            .Distinct()
            .ToListAsync(cancellationToken);

        var excludedIds = candidateIdsAssignedToActiveRoutes.ToHashSet();

        var parcels = await dbContext.Parcels
            .AsNoTracking()
            .Include(candidate => candidate.RecipientAddress)
            .Include(candidate => candidate.Zone)
            .Where(candidate =>
                candidate.Status == ParcelStatus.Staged
                && candidate.ZoneId == route.ZoneId
                && candidate.Zone.DepotId == route.Zone.DepotId
                && !excludedIds.Contains(candidate.Id))
            .OrderBy(candidate => candidate.TrackingNumber)
            .ToListAsync(cancellationToken);

        return parcels
            .Select(candidate => new RouteParcelAdjustmentCandidateDto
            {
                Id = candidate.Id,
                TrackingNumber = candidate.TrackingNumber,
                RecipientLabel = RouteParcelAdjustmentSupport.BuildRecipientLabel(candidate),
                AddressLine = RouteParcelAdjustmentSupport.BuildAddressLine(candidate.RecipientAddress),
                Longitude = candidate.RecipientAddress.GeoLocation == null ? null : candidate.RecipientAddress.GeoLocation.X,
                Latitude = candidate.RecipientAddress.GeoLocation == null ? null : candidate.RecipientAddress.GeoLocation.Y,
                Status = candidate.Status,
            })
            .ToList();
    }
}
