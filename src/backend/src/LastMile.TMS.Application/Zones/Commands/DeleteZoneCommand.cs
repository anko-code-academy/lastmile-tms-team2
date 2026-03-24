using MediatR;

namespace LastMile.TMS.Application.Zones.Commands;

public record DeleteZoneCommand(Guid Id) : IRequest<bool>;
