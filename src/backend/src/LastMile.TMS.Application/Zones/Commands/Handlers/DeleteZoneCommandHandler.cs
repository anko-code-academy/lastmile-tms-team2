using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Zones.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Zones.Commands.Handlers;

public class DeleteZoneCommandHandler(IAppDbContext db) : IRequestHandler<DeleteZoneCommand, bool>
{
    public async Task<bool> Handle(DeleteZoneCommand request, CancellationToken cancellationToken)
    {
        var zone = await db.Zones
            .Include(z => z.Parcels)
            .FirstOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zone is null)
            return false;

        if (zone.Parcels.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete zone '{zone.Name}' because it has {zone.Parcels.Count} parcel(s) assigned to it. Remove or reassign the parcels first.");
        }

        db.Zones.Remove(zone);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
