using LastMile.TMS.Application.BinLocations.DTOs;
using LastMile.TMS.Application.BinLocations.Mappings;
using LastMile.TMS.Application.BinLocations.Support;
using LastMile.TMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.BinLocations.Commands;

public sealed class UpdateStorageZoneCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateStorageZoneCommand, StorageZoneResultDto?>
{
    public async Task<StorageZoneResultDto?> Handle(UpdateStorageZoneCommand request, CancellationToken cancellationToken)
    {
        var entity = await db.StorageZones
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var name = BinLocationNameNormalizer.Normalize(request.Dto.Name);
        var normalizedName = BinLocationNameNormalizer.NormalizeForUniqueness(name);

        var duplicateExists = await db.StorageZones
            .AnyAsync(
                x => x.Id != request.Id
                    && x.DepotId == entity.DepotId
                    && x.NormalizedName == normalizedName,
                cancellationToken);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"A storage zone named '{name}' already exists for this depot.");
        }

        entity.Name = name;
        entity.NormalizedName = normalizedName;

        await db.SaveChangesAsync(cancellationToken);

        return entity.ToResultDto();
    }
}
