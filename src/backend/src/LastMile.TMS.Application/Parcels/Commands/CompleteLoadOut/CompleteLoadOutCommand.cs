using LastMile.TMS.Application.Parcels.DTOs;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed record CompleteLoadOutCommand(
    Guid RouteId,
    bool Force) : IRequest<CompleteLoadOutResultDto>;
