using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Application.Parcels.Reads;

public interface IParcelReadService
{
    IQueryable<Parcel> GetParcelsForRouteCreation();
    IQueryable<ParcelDto> GetRegisteredParcels(string? search = null, ParcelFilter? filter = null);
    IQueryable<ParcelDto> GetPreLoadParcels(string? search = null, ParcelFilter? filter = null);
    Task<ParcelDetailDto?> GetParcelByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ParcelLabelDataDto>> GetParcelLabelDataAsync(
        IReadOnlyCollection<Guid> parcelIds,
        CancellationToken cancellationToken = default);
}
