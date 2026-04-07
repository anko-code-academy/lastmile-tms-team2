using LastMile.TMS.Application.BinLocations.DTOs;
using LastMile.TMS.Application.BinLocations.Mappings;
using LastMile.TMS.Application.BinLocations.Support;
using LastMile.TMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.BinLocations.Commands;

public sealed class UpdateBinLocationCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateBinLocationCommand, BinLocationResultDto?>
{
    public async Task<BinLocationResultDto?> Handle(UpdateBinLocationCommand request, CancellationToken cancellationToken)
    {
        var entity = await db.BinLocations
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var name = BinLocationNameNormalizer.Normalize(request.Dto.Name);
        var normalizedName = BinLocationNameNormalizer.NormalizeForUniqueness(name);

        var duplicateExists = await db.BinLocations
            .AnyAsync(
                x => x.Id != request.Id
                    && x.StorageAisleId == entity.StorageAisleId
                    && x.NormalizedName == normalizedName,
                cancellationToken);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"A bin location named '{name}' already exists for this storage aisle.");
        }

        entity.Name = name;
        entity.NormalizedName = normalizedName;
        if (request.Dto.IsActive.HasValue)
        {
            entity.IsActive = request.Dto.IsActive.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return entity.ToResultDto();
    }
}
