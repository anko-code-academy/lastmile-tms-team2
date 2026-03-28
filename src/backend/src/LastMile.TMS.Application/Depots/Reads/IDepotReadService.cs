using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Application.Depots.Reads;

public interface IDepotReadService
{
    IQueryable<Depot> GetDepots();
    IQueryable<Depot> GetDepotById(Guid id);
}
