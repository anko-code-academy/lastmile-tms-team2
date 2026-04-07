using LastMile.TMS.Application.BinLocations.DTOs;
using LastMile.TMS.Application.BinLocations.Mappings;
using LastMile.TMS.Application.BinLocations.Support;
using LastMile.TMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.BinLocations.Commands;

public sealed class UpdateStorageAisleCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateStorageAisleCommand, StorageAisleResultDto?>
{
    public async Task<StorageAisleResultDto?> Handle(UpdateStorageAisleCommand request, CancellationToken cancellationToken)
    {
        var entity = await db.StorageAisles
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var name = BinLocationNameNormalizer.Normalize(request.Dto.Name);
        var normalizedName = BinLocationNameNormalizer.NormalizeForUniqueness(name);

        var duplicateExists = await db.StorageAisles
            .AnyAsync(
                x => x.Id != request.Id
                    && x.StorageZoneId == entity.StorageZoneId
                    && x.NormalizedName == normalizedName,
                cancellationToken);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"A storage aisle named '{name}' already exists for this storage zone.");
        }

        entity.Name = name;
        entity.NormalizedName = normalizedName;

        await db.SaveChangesAsync(cancellationToken);

        return entity.ToResultDto();
    }
}
