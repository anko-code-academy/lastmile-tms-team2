using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Drivers.Commands;

public sealed class DeleteDriverCommandHandler(
    IAppDbContext dbContext,
    IDriverPhotoFileCleanup driverPhotoFileCleanup) : IRequestHandler<DeleteDriverCommand, bool>
{
    public async Task<bool> Handle(DeleteDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = await dbContext.Drivers
            .Include(d => d.AvailabilitySchedule)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (driver is null)
            return false;

        var hasActiveRoutes = await dbContext.Routes
            .AnyAsync(r => r.DriverId == request.Id &&
                (r.Status == RouteStatus.Draft
                 || r.Status == RouteStatus.Dispatched
                 || r.Status == RouteStatus.InProgress),
                cancellationToken);

        if (hasActiveRoutes)
            throw new InvalidOperationException("Cannot delete driver with active routes");

        var photoUrl = driver.PhotoUrl;

        foreach (var availability in driver.AvailabilitySchedule.ToList())
        {
            dbContext.DriverAvailabilities.Remove(availability);
        }

        dbContext.Drivers.Remove(driver);
        await dbContext.SaveChangesAsync(cancellationToken);

        await driverPhotoFileCleanup.TryDeleteStoredPhotoAsync(photoUrl, cancellationToken);

        return true;
    }
}
