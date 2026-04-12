using LastMile.TMS.Application.Parcels.DTOs;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Queries;

public sealed record GetParcelSortInstructionQuery(
    string TrackingNumber,
    Guid? DepotId = null) : IRequest<ParcelSortInstructionDto?>;
