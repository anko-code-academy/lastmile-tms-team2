using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Depots.Reads;

public sealed class DepotReadService(IAppDbContext dbContext) : IDepotReadService
{
    public IQueryable<Depot> GetDepots() =>
        dbContext.Depots
            .AsNoTracking();

    public IQueryable<Depot> GetDepotById(Guid id) =>
        dbContext.Depots
            .AsNoTracking()
            .Where(d => d.Id == id);
}
