using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Mappings;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Reads;

public sealed class ParcelReadService(IAppDbContext dbContext) : IParcelReadService
{
    private static readonly ParcelStatus[] RouteCreationStatuses = [ParcelStatus.Sorted, ParcelStatus.Staged];
    private static readonly ParcelStatus[] PreLoadStatuses =
        [ParcelStatus.Registered, ParcelStatus.ReceivedAtDepot, ParcelStatus.Sorted, ParcelStatus.Staged];

    public IQueryable<Parcel> GetParcelsForRouteCreation() =>
        dbContext.Parcels
            .AsNoTracking()
            .Where(p => RouteCreationStatuses.Contains(p.Status))
            .OrderBy(p => p.TrackingNumber);

    public IQueryable<ParcelDto> GetRegisteredParcels() =>
        dbContext.Parcels
            .AsNoTracking()
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .Where(p => p.Status == ParcelStatus.Registered)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ToDto());

    public IQueryable<ParcelDto> GetPreLoadParcels() =>
        dbContext.Parcels
            .AsNoTracking()
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .Where(p => PreLoadStatuses.Contains(p.Status))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ToDto());

    public async Task<ParcelDetailDto?> GetParcelByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parcel = await dbContext.Parcels
            .AsNoTracking()
            .Include(p => p.RecipientAddress)
            .Include(p => p.ChangeHistory)
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return parcel?.ToDetailDto();
    }

    public async Task<IReadOnlyList<ParcelLabelDataDto>> GetParcelLabelDataAsync(
        IReadOnlyCollection<Guid> parcelIds,
        CancellationToken cancellationToken = default)
    {
        if (parcelIds.Count == 0)
        {
            return [];
        }

        var parcels = await dbContext.Parcels
            .AsNoTracking()
            .Include(p => p.RecipientAddress)
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .Where(p => parcelIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return parcels
            .Select(parcel => parcel.ToLabelDataDto())
            .ToArray();
    }

    public async Task<IReadOnlyList<TrackingEventDto>> GetTrackingEventsAsync(
        Guid parcelId,
        CancellationToken cancellationToken = default)
    {
        var events = await dbContext.Parcels
            .AsNoTracking()
            .Where(p => p.Id == parcelId)
            .SelectMany(p => p.TrackingEvents)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        return events.Select(e => e.ToDto()).ToList();
    }
}
