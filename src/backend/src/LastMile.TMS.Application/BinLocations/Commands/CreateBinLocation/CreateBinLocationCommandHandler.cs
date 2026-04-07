using LastMile.TMS.Application.BinLocations.DTOs;
using LastMile.TMS.Application.BinLocations.Mappings;
using LastMile.TMS.Application.BinLocations.Support;
using LastMile.TMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.BinLocations.Commands;

public sealed class CreateBinLocationCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateBinLocationCommand, BinLocationResultDto>
{
    public async Task<BinLocationResultDto> Handle(CreateBinLocationCommand request, CancellationToken cancellationToken)
    {
        var name = BinLocationNameNormalizer.Normalize(request.Dto.Name);
        var normalizedName = BinLocationNameNormalizer.NormalizeForUniqueness(name);

        var storageAisleExists = await db.StorageAisles
            .AnyAsync(x => x.Id == request.Dto.StorageAisleId, cancellationToken);
        if (!storageAisleExists)
        {
            throw new InvalidOperationException($"Storage aisle '{request.Dto.StorageAisleId}' was not found.");
        }

        var duplicateExists = await db.BinLocations
            .AnyAsync(
                x => x.StorageAisleId == request.Dto.StorageAisleId
                    && x.NormalizedName == normalizedName,
                cancellationToken);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"A bin location named '{name}' already exists for this storage aisle.");
        }

        var entity = request.Dto.ToEntity();
        entity.Name = name;
        entity.NormalizedName = normalizedName;

        try
        {
            db.BinLocations.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            var duplicatePersisted = await db.BinLocations
                .AnyAsync(
                    x => x.StorageAisleId == request.Dto.StorageAisleId
                        && x.NormalizedName == normalizedName,
                    cancellationToken);

            if (duplicatePersisted)
            {
                throw new InvalidOperationException(
                    $"A bin location named '{name}' already exists for this storage aisle.",
                    ex);
            }

            throw;
        }

        return entity.ToResultDto();
    }
}
