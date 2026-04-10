using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Vehicles.Commands;

public sealed class DeleteVehicleCommandHandler(IAppDbContext dbContext) : IRequestHandler<DeleteVehicleCommand, bool>
{
    public async Task<bool> Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.FindAsync([request.Id], cancellationToken);

        if (vehicle is null)
            return false;

        var hasActiveRoutes = await dbContext.Routes
            .AnyAsync(r => r.VehicleId == request.Id &&
                (r.Status == RouteStatus.Draft
                 || r.Status == RouteStatus.Dispatched
                 || r.Status == RouteStatus.InProgress),
                cancellationToken);

        if (hasActiveRoutes)
            throw new InvalidOperationException("Cannot delete vehicle that has active routes. Complete or cancel the routes first.");

        dbContext.Vehicles.Remove(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
