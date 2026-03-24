using LastMile.TMS.Application.Zones.DTOs;
using MediatR;

namespace LastMile.TMS.Application.Zones.Queries;

public record GetAllZonesQuery : IRequest<List<ZoneDto>>;
