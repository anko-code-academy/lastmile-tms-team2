using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Common.Models;
using LastMile.TMS.Application.Drivers.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class StorageBackfillRunner(
    IAppDbContext dbContext,
    IFileStorageService fileStorageService,
    IWebHostEnvironment environment,
    ILogger<StorageBackfillRunner> logger)
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public async Task<StorageBackfillResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var result = new StorageBackfillResult();

        result.DriverPhotosMigrated = await BackfillDriverPhotosAsync(cancellationToken);
        result.ParcelImportFilesMigrated = await BackfillParcelImportsAsync(cancellationToken);
        (result.DeliveryConfirmationPhotosMigrated, result.DeliveryConfirmationSignaturesMigrated) =
            await BackfillDeliveryConfirmationsAsync(cancellationToken);

        logger.LogInformation(
            "Storage backfill completed. Drivers={DriversMigrated}, ParcelImports={ParcelImportsMigrated}, DeliveryPhotos={DeliveryPhotosMigrated}, DeliverySignatures={DeliverySignaturesMigrated}",
            result.DriverPhotosMigrated,
            result.ParcelImportFilesMigrated,
            result.DeliveryConfirmationPhotosMigrated,
            result.DeliveryConfirmationSignaturesMigrated);

        return result;
    }

    private async Task<int> BackfillDriverPhotosAsync(CancellationToken cancellationToken)
    {
        var migrated = 0;
        var drivers = await dbContext.Drivers
            .Where(x => x.PhotoUrl != null && x.PhotoUrl != "")
            .ToListAsync(cancellationToken);

        foreach (var driver in drivers)
        {
            if (!DriverPhotoReference.TryParse(driver.PhotoUrl, out var photoReference))
            {
                logger.LogWarning("Skipping driver {DriverId}: unsupported photo URL {PhotoUrl}", driver.Id, driver.PhotoUrl);
                continue;
            }

            if (photoReference.Location == DriverPhotoLocation.ObjectStorage)
            {
                continue;
            }

            var legacyPath = Path.Combine(GetWebRootPath(), "uploads", "drivers", photoReference.FileName);
            if (!File.Exists(legacyPath))
            {
                logger.LogWarning("Skipping driver {DriverId}: legacy photo file {LegacyPath} does not exist", driver.Id, legacyPath);
                continue;
            }

            await using (var stream = File.OpenRead(legacyPath))
            {
                await fileStorageService.UploadAsync(
                    photoReference.StorageKey,
                    stream,
                    ResolveContentType(photoReference.FileName),
                    cancellationToken);
            }

            driver.PhotoUrl = DriverPhotoReference.BuildObjectStorageUrl(photoReference.FileName);
            await dbContext.SaveChangesAsync(cancellationToken);
            TryDeleteLegacyFile(legacyPath);
            migrated++;
        }

        return migrated;
    }

    private async Task<int> BackfillParcelImportsAsync(CancellationToken cancellationToken)
    {
        var migrated = 0;
        var parcelImports = await dbContext.ParcelImports
            .Where(x => (x.SourceFileKey == null || x.SourceFileKey == "") && x.SourceFile != null)
            .ToListAsync(cancellationToken);

        foreach (var parcelImport in parcelImports)
        {
            if (parcelImport.SourceFile is not { Length: > 0 })
            {
                continue;
            }

            var sourceFileKey = StorageObjectKeys.BuildParcelImportSourceFileKey(parcelImport.Id, parcelImport.FileName);
            await using var stream = new MemoryStream(parcelImport.SourceFile, writable: false);
            await fileStorageService.UploadAsync(
                sourceFileKey,
                stream,
                "application/octet-stream",
                cancellationToken);

            parcelImport.SourceFileKey = sourceFileKey;
            parcelImport.SourceFile = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            migrated++;
        }

        return migrated;
    }

    private async Task<(int photosMigrated, int signaturesMigrated)> BackfillDeliveryConfirmationsAsync(
        CancellationToken cancellationToken)
    {
        var photosMigrated = 0;
        var signaturesMigrated = 0;
        var deliveryConfirmations = await dbContext.DeliveryConfirmations.ToListAsync(cancellationToken);

        foreach (var deliveryConfirmation in deliveryConfirmations)
        {
            var updated = false;

            if (string.IsNullOrWhiteSpace(deliveryConfirmation.PhotoKey)
                && deliveryConfirmation.Photo is { Length: > 0 })
            {
                var photoKey = StorageObjectKeys.BuildDeliveryConfirmationPhotoKey(deliveryConfirmation.Id);
                await using var photoStream = new MemoryStream(deliveryConfirmation.Photo, writable: false);
                await fileStorageService.UploadAsync(
                    photoKey,
                    photoStream,
                    "application/octet-stream",
                    cancellationToken);

                deliveryConfirmation.PhotoKey = photoKey;
                deliveryConfirmation.Photo = null;
                photosMigrated++;
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(deliveryConfirmation.SignatureImageKey)
                && deliveryConfirmation.SignatureImage is { Length: > 0 })
            {
                var signatureKey = StorageObjectKeys.BuildDeliveryConfirmationSignatureKey(deliveryConfirmation.Id);
                await using var signatureStream = new MemoryStream(deliveryConfirmation.SignatureImage, writable: false);
                await fileStorageService.UploadAsync(
                    signatureKey,
                    signatureStream,
                    "application/octet-stream",
                    cancellationToken);

                deliveryConfirmation.SignatureImageKey = signatureKey;
                deliveryConfirmation.SignatureImage = null;
                signaturesMigrated++;
                updated = true;
            }

            if (updated)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return (photosMigrated, signaturesMigrated);
    }

    private string GetWebRootPath()
    {
        return string.IsNullOrEmpty(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
    }

    private static string ResolveContentType(string fileName)
    {
        return ContentTypeProvider.TryGetContentType(fileName, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static void TryDeleteLegacyFile(string legacyPath)
    {
        try
        {
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

public sealed class StorageBackfillResult
{
    public int DriverPhotosMigrated { get; set; }

    public int ParcelImportFilesMigrated { get; set; }

    public int DeliveryConfirmationPhotosMigrated { get; set; }

    public int DeliveryConfirmationSignaturesMigrated { get; set; }
}
