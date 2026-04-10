using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Domain.Entities;
using MediatR;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed record CompleteRouteCommand(Guid Id, CompleteRouteDto Dto) : IRequest<Route?>;
