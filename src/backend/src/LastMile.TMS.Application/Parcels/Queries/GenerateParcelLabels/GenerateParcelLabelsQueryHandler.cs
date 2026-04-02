using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Reads;
using LastMile.TMS.Application.Parcels.Services;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Queries;

public sealed class GenerateParcelLabelsQueryHandler(
    IParcelReadService readService,
    IParcelLabelGenerator labelGenerator)
    : IRequestHandler<GenerateParcelLabelsQuery, GeneratedLabelFileDto>
{
    public async Task<GeneratedLabelFileDto> Handle(
        GenerateParcelLabelsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ParcelIds.Count == 0)
        {
            throw new ArgumentException("At least one parcel id is required.", nameof(request.ParcelIds));
        }

        var labelData = await readService.GetParcelLabelDataAsync(request.ParcelIds, cancellationToken);
        var labelDataById = labelData.ToDictionary(parcel => parcel.Id);

        var missingIds = request.ParcelIds
            .Where(id => !labelDataById.ContainsKey(id))
            .Distinct()
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Could not generate labels. Parcel ids not found: {string.Join(", ", missingIds)}");
        }

        var orderedLabels = request.ParcelIds
            .Select(id => labelDataById[id])
            .ToArray();

        return await labelGenerator.GenerateAsync(orderedLabels, request.Format, cancellationToken);
    }
}
