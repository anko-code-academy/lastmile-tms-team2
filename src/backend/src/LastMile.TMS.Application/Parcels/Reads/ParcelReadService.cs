using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Reads;

public sealed class ParcelReadService(IAppDbContext dbContext) : IParcelReadService
{
    private static readonly ParcelStatus[] RouteCreationStatuses = [ParcelStatus.Sorted, ParcelStatus.Staged];

    public IQueryable<ParcelOptionDto> GetParcelsForRouteCreation() =>
        dbContext.Parcels
            .AsNoTracking()
            .Where(p => RouteCreationStatuses.Contains(p.Status))
            .OrderBy(p => p.TrackingNumber)
            .Select(p => new ParcelOptionDto
            {
                Id = p.Id,
                TrackingNumber = p.TrackingNumber,
                Weight = p.Weight,
                WeightUnit = p.WeightUnit,
            });
}
