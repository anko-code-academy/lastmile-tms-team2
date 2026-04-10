using LastMile.TMS.Domain.Entities;
using MediatR;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed record StartRouteCommand(Guid Id) : IRequest<Route?>;
