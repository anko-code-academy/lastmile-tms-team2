using FluentAssertions;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Queries;
using LastMile.TMS.Application.Parcels.Reads;
using LastMile.TMS.Application.Parcels.Services;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Parcels;

public class GenerateParcelLabelsQueryHandlerTests
{
    [Fact]
    public async Task Handle_PreservesRequestedParcelOrder()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var readService = Substitute.For<IParcelReadService>();
        var labelGenerator = Substitute.For<IParcelLabelGenerator>();
        IReadOnlyList<ParcelLabelDataDto>? capturedParcels = null;

        readService.GetParcelLabelDataAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([
                CreateLabelData(secondId, "LM202604010002"),
                CreateLabelData(firstId, "LM202604010001")
            ]);

        labelGenerator.GenerateAsync(
                Arg.Do<IReadOnlyList<ParcelLabelDataDto>>(parcels => capturedParcels = parcels),
                LabelOutputFormat.Zpl,
                Arg.Any<CancellationToken>())
            .Returns(new GeneratedLabelFileDto([1, 2, 3], "text/plain; charset=utf-8", "parcel-labels-4x6.zpl"));

        var handler = new GenerateParcelLabelsQueryHandler(readService, labelGenerator);

        var result = await handler.Handle(
            new GenerateParcelLabelsQuery([firstId, secondId], LabelOutputFormat.Zpl),
            CancellationToken.None);

        result.FileName.Should().Be("parcel-labels-4x6.zpl");
        capturedParcels.Should().NotBeNull();
        capturedParcels!.Select(parcel => parcel.Id).Should().Equal(firstId, secondId);
    }

    [Fact]
    public async Task Handle_MissingParcelIds_ThrowsInformativeError()
    {
        var existingId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        var readService = Substitute.For<IParcelReadService>();
        var labelGenerator = Substitute.For<IParcelLabelGenerator>();

        readService.GetParcelLabelDataAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([CreateLabelData(existingId, "LM202604010001")]);

        var handler = new GenerateParcelLabelsQueryHandler(readService, labelGenerator);

        var act = () => handler.Handle(
            new GenerateParcelLabelsQuery([existingId, missingId], LabelOutputFormat.Zpl),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{missingId}*");
    }

    private static ParcelLabelDataDto CreateLabelData(Guid id, string trackingNumber) =>
        new()
        {
            Id = id,
            TrackingNumber = trackingNumber,
            RecipientName = "Jamie Carter",
            Street1 = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            CountryCode = "US",
            SortZone = "North Zone",
            ParcelType = "Box"
        };
}
