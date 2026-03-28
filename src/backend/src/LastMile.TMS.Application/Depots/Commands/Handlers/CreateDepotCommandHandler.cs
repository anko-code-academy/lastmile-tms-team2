using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Depots;
using LastMile.TMS.Application.Depots.Commands;
using LastMile.TMS.Domain.Entities;
using MediatR;

namespace LastMile.TMS.Application.Depots.Commands.Handlers;

public class CreateDepotCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateDepotCommand, Depot>
{
    public async Task<Depot> Handle(CreateDepotCommand request, CancellationToken cancellationToken)
    {
        var address = request.Address.ToEntity();
        address.CountryCode = address.CountryCode.ToUpperInvariant();

        var depot = new Depot
        {
            Name = request.Name,
            Address = address,
            IsActive = request.IsActive,
        };

        if (request.OperatingHours is not null)
        {
            foreach (var hours in request.OperatingHours)
            {
                depot.OperatingHours.Add(hours.ToEntity());
            }
        }

        db.Depots.Add(depot);
        await db.SaveChangesAsync(cancellationToken);

        return depot;
    }
}
