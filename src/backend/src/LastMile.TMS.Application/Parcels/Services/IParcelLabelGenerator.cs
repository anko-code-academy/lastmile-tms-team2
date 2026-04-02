using LastMile.TMS.Application.Parcels.DTOs;

namespace LastMile.TMS.Application.Parcels.Services;

public interface IParcelLabelGenerator
{
    Task<GeneratedLabelFileDto> GenerateAsync(
        IReadOnlyList<ParcelLabelDataDto> parcels,
        LabelOutputFormat format,
        CancellationToken cancellationToken = default);
}
