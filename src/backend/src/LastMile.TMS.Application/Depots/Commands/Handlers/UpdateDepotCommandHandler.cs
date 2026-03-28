using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Depots;
using LastMile.TMS.Application.Depots.Commands;
using LastMile.TMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Depots.Commands.Handlers;

public class UpdateDepotCommandHandler(
    IAppDbContext db)
    : IRequestHandler<UpdateDepotCommand, Depot?>
{
    public async Task<Depot?> Handle(UpdateDepotCommand request, CancellationToken cancellationToken)
    {
        var depot = await db.Depots
            .Include(d => d.Address)
            .Include(d => d.OperatingHours)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (depot is null)
            return null;

        depot.Name = request.Name;
        depot.IsActive = request.IsActive;

        if (request.Address is not null)
        {
            var updatedAddress = request.Address.ToEntity();
            updatedAddress.CountryCode = updatedAddress.CountryCode.ToUpperInvariant();

            depot.Address.Street1 = updatedAddress.Street1;
            depot.Address.Street2 = updatedAddress.Street2;
            depot.Address.City = updatedAddress.City;
            depot.Address.State = updatedAddress.State;
            depot.Address.PostalCode = updatedAddress.PostalCode;
            depot.Address.CountryCode = updatedAddress.CountryCode;
            depot.Address.IsResidential = updatedAddress.IsResidential;
            depot.Address.ContactName = updatedAddress.ContactName;
            depot.Address.CompanyName = updatedAddress.CompanyName;
            depot.Address.Phone = updatedAddress.Phone;
            depot.Address.Email = updatedAddress.Email;
        }

        if (request.OperatingHours is not null)
        {
            depot.OperatingHours.Clear();
            foreach (var hours in request.OperatingHours)
            {
                var operatingHours = hours.ToEntity();
                operatingHours.DepotId = depot.Id;
                depot.OperatingHours.Add(operatingHours);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return depot;
    }
}
