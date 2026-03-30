using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Application.Parcels.Reads;

public interface IParcelReadService
{
    IQueryable<Parcel> GetParcelsForRouteCreation();
    IQueryable<Parcel> GetRegisteredParcels();
}
