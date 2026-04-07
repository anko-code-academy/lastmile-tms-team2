using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Common.Models;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class CreateParcelImportCommandHandler(
    IAppDbContext db,
    IFileStorageService fileStorageService,
    IParcelImportFileParser parser,
    IParcelImportJobScheduler scheduler,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateParcelImportCommand, Guid>
{
    public async Task<Guid> Handle(CreateParcelImportCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var parcelImportId = Guid.NewGuid();

        await using (var validationStream = new MemoryStream(dto.SourceFile, writable: false))
        {
            _ = await parser.ParseAsync(dto.FileName, validationStream, cancellationToken);
        }

        var sourceFileKey = StorageObjectKeys.BuildParcelImportSourceFileKey(parcelImportId, dto.FileName);
        await using (var uploadStream = new MemoryStream(dto.SourceFile, writable: false))
        {
            await fileStorageService.UploadAsync(
                sourceFileKey,
                uploadStream,
                "application/octet-stream",
                cancellationToken);
        }

        var parcelImport = new ParcelImport
        {
            Id = parcelImportId,
            FileName = dto.FileName,
            FileFormat = dto.FileFormat,
            ShipperAddressId = dto.ShipperAddressId,
            Status = ParcelImportStatus.Queued,
            SourceFileKey = sourceFileKey,
            CreatedBy = currentUser.UserName ?? currentUser.UserId,
        };

        db.ParcelImports.Add(parcelImport);
        await db.SaveChangesAsync(cancellationToken);

        await scheduler.ScheduleAsync(parcelImportId, cancellationToken);
        return parcelImportId;
    }
}
