using System.Text.Json;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Commands;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Services;

public sealed class ParcelImportProcessor(
    IAppDbContext db,
    IFileStorageService fileStorageService,
    IParcelImportFileParser parser,
    IParcelRegistrationService registrationService)
{
    private const int ProgressSaveBatchSize = 25;
    private readonly RegisterParcelCommandValidator _validator = new();

    public async Task ProcessAsync(Guid parcelImportId, CancellationToken cancellationToken = default)
    {
        var parcelImport = await db.ParcelImports
            .Include(x => x.RowFailures)
            .SingleOrDefaultAsync(x => x.Id == parcelImportId, cancellationToken);

        if (parcelImport is null)
        {
            throw new InvalidOperationException($"Parcel import '{parcelImportId}' was not found.");
        }

        parcelImport.Status = ParcelImportStatus.Processing;
        parcelImport.StartedAt ??= DateTimeOffset.UtcNow;
        parcelImport.FailureMessage = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await using var sourceStream = await OpenSourceFileStreamAsync(parcelImport, cancellationToken);
            var parsedFile = await parser.ParseAsync(
                parcelImport.FileName,
                sourceStream,
                cancellationToken);

            parcelImport.TotalRows = parsedFile.TotalRows;
            await db.SaveChangesAsync(cancellationToken);

            foreach (var row in parsedFile.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var mappedRow = ParcelImportRowMapper.TryMap(parcelImport.ShipperAddressId, row.Values);
                    if (!mappedRow.IsSuccess || mappedRow.Dto is null)
                    {
                        RecordFailure(parcelImport, row, mappedRow.ErrorMessage ?? "Row could not be mapped.");
                    }
                    else
                    {
                        var validation = _validator.Validate(new RegisterParcelCommand(mappedRow.Dto));
                        if (!validation.IsValid)
                        {
                            RecordFailure(
                                parcelImport,
                                row,
                                string.Join("; ", validation.Errors.Select(x => x.ErrorMessage)));
                        }
                        else
                        {
                            await registrationService.RegisterAsync(
                                mappedRow.Dto,
                                cancellationToken,
                                parcelImport.Id,
                                parcelImport.CreatedBy);

                            parcelImport.ImportedRows++;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    RecordFailure(parcelImport, row, ex.Message);
                }
                finally
                {
                    parcelImport.ProcessedRows++;
                    parcelImport.RejectedRows = parcelImport.RowFailures.Count;

                    if (ShouldSaveProgress(parcelImport.ProcessedRows, parcelImport.TotalRows))
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                }
            }

            parcelImport.Status = parcelImport.RowFailures.Count switch
            {
                0 => ParcelImportStatus.Completed,
                _ => ParcelImportStatus.CompletedWithErrors,
            };
            parcelImport.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            parcelImport.Status = ParcelImportStatus.Failed;
            parcelImport.CompletedAt = DateTimeOffset.UtcNow;
            parcelImport.FailureMessage = ex.Message;
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static void RecordFailure(
        ParcelImport parcelImport,
        ParcelImportParsedRow row,
        string errorMessage)
    {
        parcelImport.RowFailures.Add(
            new ParcelImportRowFailure
            {
                ParcelImportId = parcelImport.Id,
                RowNumber = row.RowNumber,
                OriginalRowValues = JsonSerializer.Serialize(row.Values),
                ErrorMessage = errorMessage,
            });
    }

    private static bool ShouldSaveProgress(int processedRows, int totalRows)
    {
        return processedRows == totalRows
            || processedRows % ProgressSaveBatchSize == 0;
    }

    private async Task<Stream> OpenSourceFileStreamAsync(
        ParcelImport parcelImport,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(parcelImport.SourceFileKey))
        {
            var storedObject = await fileStorageService.OpenReadAsync(parcelImport.SourceFileKey, cancellationToken);
            return storedObject.Content;
        }

        if (parcelImport.SourceFile is { Length: > 0 })
        {
            return new MemoryStream(parcelImport.SourceFile, writable: false);
        }

        throw new InvalidOperationException($"Parcel import '{parcelImport.Id}' has no source file content.");
    }
}
