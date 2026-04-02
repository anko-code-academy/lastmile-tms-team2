namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed record ParcelLabelDownloadRequestDto
{
    public IReadOnlyList<Guid> ParcelIds { get; init; } = [];

    public ParcelLabelDownloadRequestDto() { }
}
