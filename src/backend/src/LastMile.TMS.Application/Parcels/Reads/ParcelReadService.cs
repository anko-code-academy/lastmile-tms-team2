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

    public IQueryable<ParcelDto> GetRegisteredParcels(string? search = null)
    {
        var query = dbContext.Parcels
            .AsNoTracking()
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .Include(p => p.RecipientAddress)
            .Where(p => p.Status == ParcelStatus.Registered);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = search.Trim().ToUpperInvariant();
            query = query.Where(p =>
                p.TrackingNumber.ToUpper().Contains(pattern) ||
                (p.RecipientAddress.ContactName ?? string.Empty).ToUpper().Contains(pattern) ||
                (p.RecipientAddress.CompanyName ?? string.Empty).ToUpper().Contains(pattern) ||
                (p.RecipientAddress.Street1 ?? string.Empty).ToUpper().Contains(pattern) ||
                (p.RecipientAddress.City ?? string.Empty).ToUpper().Contains(pattern) ||
                (p.RecipientAddress.PostalCode ?? string.Empty).ToUpper().Contains(pattern));
        }

        return query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ToDto());
    }

    public IQueryable<ParcelDto> GetPreLoadParcels(string? search = null)
    {
        var query = dbContext.Parcels
            .AsNoTracking()
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .Include(p => p.RecipientAddress)
            .Where(p => PreLoadStatuses.Contains(p.Status));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = search.Trim().ToUpperInvariant();
            query = query.Where(p =>
                p.TrackingNumber.ToUpper().Contains(pattern) ||
                (p.RecipientAddress.ContactName ?? string.Empty).ToUpper().Contains(pattern) ||
                (p.RecipientAddress.CompanyName ?? string.Empty).ToUpper().Contains(pattern) ||
                (p.RecipientAddress.Street1 ?? string.Empty).ToUpper().Contains(pattern) ||
                (p.RecipientAddress.City ?? string.Empty).ToUpper().Contains(pattern) ||
                (p.RecipientAddress.PostalCode ?? string.Empty).ToUpper().Contains(pattern));
        }

        return query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ToDto());
    }

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
}
