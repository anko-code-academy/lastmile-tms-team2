using LastMile.TMS.Application.Parcels.DTOs;

namespace LastMile.TMS.Application.Parcels.Reads;

public interface IParcelReadService
{
    IQueryable<ParcelOptionDto> GetParcelsForRouteCreation();
}
