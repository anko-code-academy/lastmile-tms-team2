namespace LastMile.TMS.Application.Parcels.Services;

public interface IParcelImportFileParser
{
    Task<ParcelImportParsedFile> ParseAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);
}
