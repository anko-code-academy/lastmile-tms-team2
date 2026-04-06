using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Mappings;
using LastMile.TMS.Application.Parcels.Support;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class TransitionParcelStatusCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser)
    : IRequestHandler<TransitionParcelStatusCommand, ParcelDto>
{
    public async Task<ParcelDto> Handle(TransitionParcelStatusCommand request, CancellationToken cancellationToken)
    {
        var parcel = await dbContext.Parcels
            .Include(p => p.TrackingEvents)
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .FirstOrDefaultAsync(p => p.Id == request.ParcelId, cancellationToken);

        if (parcel is null)
        {
            throw new InvalidOperationException($"Parcel with ID '{request.ParcelId}' was not found.");
        }

        parcel.TransitionTo(request.NewStatus);

        var actor = currentUser.UserName ?? currentUser.UserId ?? "System";
        var now = DateTimeOffset.UtcNow;
        var trackingEvent = ParcelTrackingEventFactory.CreateForParcelStatus(
            parcel.Id,
            request.NewStatus,
            now,
            request.Location,
            request.Description,
            actor);

        parcel.TrackingEvents.Add(trackingEvent);

        await dbContext.SaveChangesAsync(cancellationToken);

        return parcel.ToDto();
    }
}
