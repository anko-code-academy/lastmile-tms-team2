using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Vehicles.Mappings;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Vehicles.Commands;

public sealed class UpdateVehicleCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IRequestHandler<UpdateVehicleCommand, Vehicle?>
{
    public async Task<Vehicle?> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (vehicle is null)
            return null;

        if (vehicle.RegistrationPlate != request.Dto.RegistrationPlate)
        {
            var plateExists = await dbContext.Vehicles
                .AnyAsync(v => v.RegistrationPlate == request.Dto.RegistrationPlate && v.Id != request.Id, cancellationToken);
            if (plateExists)
                throw new InvalidOperationException($"Vehicle with registration plate '{request.Dto.RegistrationPlate}' already exists.");
        }

        if (request.Dto.Status == VehicleStatus.Available)
        {
            var hasActiveRoute = await dbContext.Routes
                .AnyAsync(
                    r => r.VehicleId == vehicle.Id
                        && (r.Status == RouteStatus.Draft
                            || r.Status == RouteStatus.Dispatched
                            || r.Status == RouteStatus.InProgress),
                    cancellationToken);

            if (hasActiveRoute)
                throw new InvalidOperationException(
                    "Cannot set vehicle to Available while it has an active route. Complete or cancel the routes first.");
        }

        request.Dto.UpdateEntity(vehicle);
        vehicle.LastModifiedAt = DateTimeOffset.UtcNow;
        vehicle.LastModifiedBy = currentUser.UserName ?? currentUser.UserId;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException(
                $"Vehicle with registration plate '{request.Dto.RegistrationPlate}' already exists.");
        }

        return vehicle;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message?.Contains("23505", StringComparison.OrdinalIgnoreCase) == true;
    }
}
