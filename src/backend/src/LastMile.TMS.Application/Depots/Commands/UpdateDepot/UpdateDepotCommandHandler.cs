using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Depots.Mappings;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Depots.Commands;

public sealed class UpdateDepotCommandHandler(
    IAppDbContext db,
    IGeocodingService geocodingService)
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

        request.Dto.UpdateEntity(depot);

        if (request.Dto.Address is not null)
        {
            request.Dto.Address.UpdateEntity(depot.Address);
            depot.Address.CountryCode = depot.Address.CountryCode.ToUpperInvariant();
            await DepotAddressGeocodingSupport.ApplyGeoLocationAsync(
                depot.Address,
                geocodingService,
                cancellationToken);
        }

        if (request.Dto.OperatingHours is not null)
        {
            var incomingDtos = request.Dto.OperatingHours;

            var daysToKeep = incomingDtos.Select(d => d.DayOfWeek).ToHashSet();
            var toRemove = depot.OperatingHours
                .Where(oh => !daysToKeep.Contains(oh.DayOfWeek))
                .ToList();
            foreach (var removed in toRemove)
                depot.OperatingHours.Remove(removed);

            foreach (var dto in incomingDtos)
            {
                var existing = depot.OperatingHours
                    .FirstOrDefault(oh => oh.DayOfWeek == dto.DayOfWeek);

                if (existing is not null)
                    dto.UpdateEntity(existing);
                else
                {
                    var newHours = dto.ToEntity();
                    newHours.DepotId = depot.Id;
                    depot.OperatingHours.Add(newHours);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return depot;
    }
}
