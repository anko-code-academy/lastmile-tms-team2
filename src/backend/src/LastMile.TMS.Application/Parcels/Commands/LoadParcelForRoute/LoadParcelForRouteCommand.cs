using LastMile.TMS.Application.Parcels.DTOs;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed record LoadParcelForRouteCommand(
    Guid RouteId,
    string Barcode) : IRequest<LoadParcelForRouteResultDto>;
