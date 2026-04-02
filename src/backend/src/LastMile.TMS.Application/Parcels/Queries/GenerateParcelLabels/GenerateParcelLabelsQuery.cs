using LastMile.TMS.Application.Parcels.DTOs;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Queries;

public sealed record GenerateParcelLabelsQuery(
    IReadOnlyList<Guid> ParcelIds,
    LabelOutputFormat Format) : IRequest<GeneratedLabelFileDto>;
