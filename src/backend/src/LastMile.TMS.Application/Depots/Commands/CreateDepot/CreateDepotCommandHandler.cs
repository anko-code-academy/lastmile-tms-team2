using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Depots.Mappings;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;
using MediatR;

namespace LastMile.TMS.Application.Depots.Commands;

public sealed class CreateDepotCommandHandler(
    IAppDbContext db,
    IGeocodingService geocodingService)
    : IRequestHandler<CreateDepotCommand, Depot>
{
    public async Task<Depot> Handle(CreateDepotCommand request, CancellationToken cancellationToken)
    {
        var depot = request.Dto.ToEntity();
        var address = request.Dto.Address.ToEntity();
        address.CountryCode = address.CountryCode.ToUpperInvariant();
        await DepotAddressGeocodingSupport.ApplyGeoLocationAsync(
            address,
            geocodingService,
            cancellationToken);
        depot.Address = address;

        if (request.Dto.OperatingHours is not null)
        {
            foreach (var hours in request.Dto.OperatingHours)
            {
                depot.OperatingHours.Add(hours.ToEntity());
            }
        }

        db.Depots.Add(depot);
        await db.SaveChangesAsync(cancellationToken);

        return depot;
    }
}
