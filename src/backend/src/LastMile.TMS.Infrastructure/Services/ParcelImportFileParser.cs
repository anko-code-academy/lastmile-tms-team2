using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Parcels.Support;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class ParcelImportFileParser : IParcelImportFileParser
{
    public Task<ParcelImportParsedFile> ParseAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        var bytes = buffer.ToArray();

        if (bytes.Length == 0)
        {
            throw new ParcelImportFileValidationException("The uploaded file is empty.");
        }

        return GetFormat(fileName) switch
        {
            ParcelImportFileFormat.Csv => Task.FromResult(ParseCsv(bytes)),
            ParcelImportFileFormat.Xlsx => Task.FromResult(ParseXlsx(bytes)),
            _ => throw new ParcelImportFileValidationException("Only .csv and .xlsx files are supported."),
        };
    }

    private static ParcelImportFileFormat GetFormat(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".csv" => ParcelImportFileFormat.Csv,
            ".xlsx" => ParcelImportFileFormat.Xlsx,
            _ => throw new ParcelImportFileValidationException("Only .csv and .xlsx files are supported."),
        };
    }

    private static ParcelImportParsedFile ParseCsv(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null,
            });

        if (!csv.Read())
        {
            throw new ParcelImportFileValidationException("The uploaded file is empty.");
        }

        csv.ReadHeader();
        ValidateHeaders(csv.HeaderRecord ?? []);

        var rows = new List<ParcelImportParsedRow>();
        while (csv.Read())
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in ParcelImportTemplateColumns.All)
            {
                var value = csv.GetField(header);
                values[header] = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }

            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new ParcelImportParsedRow(csv.Parser.Row, values));
        }

        return new ParcelImportParsedFile(rows.Count, rows);
    }

    private static ParcelImportParsedFile ParseXlsx(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new ParcelImportFileValidationException("The uploaded file is empty.");

        var headers = ParcelImportTemplateColumns.All
            .Select((_, index) => worksheet.Cell(1, index + 1).GetString())
            .ToList();

        ValidateHeaders(headers);

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<ParcelImportParsedRow>();
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var columnIndex = 0; columnIndex < ParcelImportTemplateColumns.All.Length; columnIndex++)
            {
                var value = worksheet.Cell(rowNumber, columnIndex + 1).GetString();
                values[ParcelImportTemplateColumns.All[columnIndex]] =
                    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }

            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new ParcelImportParsedRow(rowNumber, values));
        }

        return new ParcelImportParsedFile(rows.Count, rows);
    }

    private static void ValidateHeaders(IReadOnlyList<string> headers)
    {
        if (!ParcelImportTemplateColumns.HasCanonicalHeaders(headers))
        {
            throw new ParcelImportFileValidationException("The uploaded file does not match the parcel import template.");
        }
    }
}
