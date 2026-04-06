using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Enums;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed record TransitionParcelStatusCommand(
    Guid ParcelId,
    ParcelStatus NewStatus,
    string? Location,
    string? Description)
    : IRequest<ParcelDto>
{
    public TransitionParcelStatusCommand() : this(Guid.Empty, ParcelStatus.Registered, null, null) { }
}
