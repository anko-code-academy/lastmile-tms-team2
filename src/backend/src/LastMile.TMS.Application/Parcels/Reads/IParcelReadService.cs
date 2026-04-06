using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Application.Parcels.Reads;

public interface IParcelReadService
{
    IQueryable<Parcel> GetParcelsForRouteCreation();
    IQueryable<Parcel> GetRegisteredParcels();
    IQueryable<Parcel> GetPreLoadParcels();
    Task<ParcelDetailDto?> GetParcelByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ParcelLabelDataDto>> GetParcelLabelDataAsync(
        IReadOnlyCollection<Guid> parcelIds,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrackingEventDto>> GetTrackingEventsAsync(Guid parcelId, CancellationToken cancellationToken = default);
}
