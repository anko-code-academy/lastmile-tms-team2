using System.Text;
using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class ParcelLabelGenerator(IZplLabelRasterizer rasterizer) : IParcelLabelGenerator
{
    private const int PrintDensityDpmm = 8;
    private const int LabelWidthDots = 812;
    private const int LabelHeightDots = 1218;
    private readonly IZplLabelRasterizer _rasterizer = rasterizer;

    public Task<GeneratedLabelFileDto> GenerateAsync(
        IReadOnlyList<ParcelLabelDataDto> parcels,
        LabelOutputFormat format,
        CancellationToken cancellationToken = default)
    {
        if (parcels.Count == 0)
        {
            throw new ArgumentException("At least one parcel label is required.", nameof(parcels));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(format switch
        {
            LabelOutputFormat.Zpl => GenerateZplFile(parcels),
            LabelOutputFormat.Pdf => GeneratePdfFile(parcels, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported label format.")
        });
    }

    private static GeneratedLabelFileDto GenerateZplFile(IReadOnlyList<ParcelLabelDataDto> parcels)
    {
        var zpl = string.Join(Environment.NewLine, parcels.Select(BuildLabelZpl));
        var bytes = Encoding.UTF8.GetBytes(zpl);
        return new GeneratedLabelFileDto(
            bytes,
            "text/plain; charset=utf-8",
            parcels.Count == 1
                ? $"parcel-{SanitizeFileSegment(parcels[0].TrackingNumber)}.zpl"
                : "parcel-labels-4x6.zpl");
    }

    private GeneratedLabelFileDto GeneratePdfFile(
        IReadOnlyList<ParcelLabelDataDto> parcels,
        CancellationToken cancellationToken)
    {
        var labelImages = parcels
            .Select(parcel =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _rasterizer.Rasterize(BuildLabelZpl(parcel), cancellationToken);
            })
            .ToArray();

        var pdfBytes = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.8f, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Content().Column(column =>
                {
                    foreach (var image in labelImages)
                    {
                        column.Item()
                            .Height(12.8f, Unit.Centimetre)
                            .PaddingBottom(0.35f, Unit.Centimetre)
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(0.15f, Unit.Centimetre)
                            .AlignCenter()
                            .AlignMiddle()
                            .Image(image)
                            .FitArea();
                    }
                });
            });
        }).GeneratePdf();

        return new GeneratedLabelFileDto(
            pdfBytes,
            "application/pdf",
            parcels.Count == 1
                ? $"parcel-{SanitizeFileSegment(parcels[0].TrackingNumber)}-a4.pdf"
                : "parcel-labels-a4.pdf");
    }

    private static string BuildLabelZpl(ParcelLabelDataDto parcel)
    {
        var titleFont = new ZplFont(fontWidth: 38, fontHeight: 38);
        var bodyFont = new ZplFont(fontWidth: 26, fontHeight: 26);
        var smallFont = new ZplFont(fontWidth: 22, fontHeight: 22);

        var elements = new List<ZplElementBase>
        {
            new ZplRaw($"^PW{LabelWidthDots}^LL{LabelHeightDots}^CI28"),
            new ZplTextField(parcel.TrackingNumber, 40, 35, titleFont),
            new ZplRaw($"^FO535,35^BQN,2,6^FDLA,{parcel.TrackingNumber}^FS"),
            new ZplRaw("^FO40,105^GB460,0,3^FS"),
        };

        var currentY = 135;
        foreach (var line in BuildRecipientLines(parcel))
        {
            elements.Add(new ZplTextField(line, 40, currentY, bodyFont));
            currentY += 42;
        }

        currentY += 18;
        elements.Add(new ZplTextField($"Sort zone: {FormatOrDash(parcel.SortZone)}", 40, currentY, bodyFont));
        currentY += 42;
        elements.Add(new ZplTextField($"Parcel type: {FormatOrDash(parcel.ParcelType)}", 40, currentY, bodyFont));
        currentY += 64;
        elements.Add(new ZplTextField("Tracking number", 40, currentY, smallFont));
        currentY += 34;
        elements.Add(new ZplTextField(parcel.TrackingNumber, 40, currentY, bodyFont));

        elements.Add(new ZplRaw($"^FO40,835^BY3,2,165^BCN,165,Y,N,N,A^FD{parcel.TrackingNumber}^FS"));
        elements.Add(new ZplRaw("^FO25,25^GB760,1168,3^FS"));

        return new ZplEngine(elements).ToZplString(new ZplRenderOptions
        {
            AddEmptyLineBeforeElementStart = false,
        });
    }

    private static IReadOnlyList<string> BuildRecipientLines(ParcelLabelDataDto parcel)
    {
        var lines = new List<string>();
        var primaryRecipient = FirstNonEmpty(parcel.RecipientName, parcel.CompanyName, "Recipient");
        var companyLine = !string.IsNullOrWhiteSpace(parcel.CompanyName) &&
                          !string.Equals(parcel.CompanyName, primaryRecipient, StringComparison.OrdinalIgnoreCase)
            ? parcel.CompanyName!.Trim()
            : null;

        lines.Add(primaryRecipient);

        if (!string.IsNullOrWhiteSpace(companyLine))
        {
            lines.Add(companyLine);
        }

        lines.Add(parcel.Street1.Trim());

        if (!string.IsNullOrWhiteSpace(parcel.Street2))
        {
            lines.Add(parcel.Street2.Trim());
        }

        lines.Add($"{parcel.City.Trim()}, {parcel.State.Trim()} {parcel.PostalCode.Trim()}".Trim());
        lines.Add(parcel.CountryCode.Trim().ToUpperInvariant());

        return lines.Take(6).ToArray();
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string FormatOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string SanitizeFileSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(invalidChars.Contains(character) ? '-' : character);
        }

        return builder.ToString();
    }
}
