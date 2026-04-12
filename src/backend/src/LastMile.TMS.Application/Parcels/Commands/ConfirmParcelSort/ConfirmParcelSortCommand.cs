using LastMile.TMS.Application.Parcels.DTOs;
using MediatR;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed record ConfirmParcelSortCommand(Guid ParcelId, Guid BinLocationId) : IRequest<ParcelDto>;
