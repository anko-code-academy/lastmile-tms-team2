using LastMile.TMS.Domain.Entities;
using MediatR;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed record DispatchRouteCommand(Guid Id) : IRequest<Route?>;
