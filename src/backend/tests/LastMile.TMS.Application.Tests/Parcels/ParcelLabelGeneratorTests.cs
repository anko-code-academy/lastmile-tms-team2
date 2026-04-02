using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Infrastructure.Services;
using QuestPDF.Infrastructure;

namespace LastMile.TMS.Application.Tests.Parcels;

public class ParcelLabelGeneratorTests
{
    private readonly TestZplLabelRasterizer _rasterizer = new();
    private readonly ParcelLabelGenerator _generator;

    public ParcelLabelGeneratorTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _generator = new ParcelLabelGenerator(_rasterizer);
    }

    [Fact]
    public async Task GenerateAsync_Zpl_ContainsQrBarcodeAndTrackingNumber()
    {
        var parcel = CreateParcel("LM202604010001");

        var file = await _generator.GenerateAsync([parcel], LabelOutputFormat.Zpl, CancellationToken.None);
        var zpl = Encoding.UTF8.GetString(file.Content);

        file.ContentType.Should().Be("text/plain; charset=utf-8");
        file.FileName.Should().Be("parcel-LM202604010001.zpl");
        zpl.Should().Contain("^BQN");
        zpl.Should().Contain("^BCN");
        zpl.Should().Contain(parcel.TrackingNumber);
        zpl.Should().Contain("Sort zone: North Zone");
        zpl.Should().Contain("Parcel type: Box");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public async Task GenerateAsync_Pdf_ReturnsNonEmptyBytesAndExpectedPageMarkers(int labelCount, int minimumPageMarkers)
    {
        var parcels = Enumerable.Range(1, labelCount)
            .Select(index => CreateParcel($"LM20260401000{index}"))
            .ToArray();

        var file = await _generator.GenerateAsync(parcels, LabelOutputFormat.Pdf, CancellationToken.None);
        var pdfText = Encoding.ASCII.GetString(file.Content);
        var pageMarkers = Regex.Matches(pdfText, @"/Type\s*/Page\b").Count;

        file.ContentType.Should().Be("application/pdf");
        file.Content.Should().NotBeEmpty();
        if (labelCount == 1)
        {
            file.FileName.Should().Be("parcel-LM202604010001-a4.pdf");
        }
        else
        {
            file.FileName.Should().Be("parcel-labels-a4.pdf");
        }

        pageMarkers.Should().BeGreaterThanOrEqualTo(minimumPageMarkers);
        _rasterizer.RenderedZpl.Should().HaveCount(labelCount);
        _rasterizer.RenderedZpl.Should().OnlyContain(zpl => zpl.Contains("^BQN") && zpl.Contains("^BCN"));
    }

    private static ParcelLabelDataDto CreateParcel(string trackingNumber) =>
        new()
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            RecipientName = "Jamie Carter",
            CompanyName = "Acme Retail",
            Street1 = "123 Main St",
            Street2 = "Suite 400",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            CountryCode = "US",
            SortZone = "North Zone",
            ParcelType = "Box"
        };

    private sealed class TestZplLabelRasterizer : IZplLabelRasterizer
    {
        private static readonly byte[] TestPngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=");

        public List<string> RenderedZpl { get; } = [];

        public byte[] Rasterize(string zpl, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RenderedZpl.Add(zpl);
            return TestPngBytes;
        }
    }
}
