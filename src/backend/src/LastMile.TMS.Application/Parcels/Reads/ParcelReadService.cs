using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Mappings;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Reads;

public sealed class ParcelReadService(IAppDbContext dbContext) : IParcelReadService
{
    private static readonly ParcelStatus[] RouteCreationStatuses = [ParcelStatus.Sorted, ParcelStatus.Staged];
    private static readonly ParcelStatus[] PreLoadStatuses =
        [ParcelStatus.Registered, ParcelStatus.ReceivedAtDepot, ParcelStatus.Sorted, ParcelStatus.Staged];

    public IQueryable<Parcel> GetParcelsForRouteCreation(Guid vehicleId, Guid driverId) =>
        from parcel in dbContext.Parcels.AsNoTracking()
        from driver in dbContext.Drivers.AsNoTracking().Where(d => d.Id == driverId)
        from vehicle in dbContext.Vehicles.AsNoTracking().Where(v => v.Id == vehicleId)
        where RouteCreationStatuses.Contains(parcel.Status)
              && driver.DepotId == vehicle.DepotId
              && parcel.ZoneId == driver.ZoneId
              && parcel.Zone.DepotId == vehicle.DepotId
        orderby parcel.TrackingNumber
        select parcel;

    public IQueryable<Parcel> GetRegisteredParcels() =>
        dbContext.Parcels
            .AsNoTracking()
            .Where(p => p.Status == ParcelStatus.Registered);

    public IQueryable<Parcel> GetPreLoadParcels() =>
        dbContext.Parcels
            .AsNoTracking()
            .Where(p => PreLoadStatuses.Contains(p.Status));

    public async Task<ParcelDetailDto?> GetParcelByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await GetParcelDetailAsync(
            query => query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken),
            cancellationToken);

    public async Task<ParcelDetailDto?> GetParcelByTrackingNumberAsync(
        string trackingNumber,
        CancellationToken cancellationToken = default)
        => await GetParcelDetailAsync(
            query => query.FirstOrDefaultAsync(
                p => p.TrackingNumber == trackingNumber,
                cancellationToken),
            cancellationToken);

    private async Task<ParcelDetailDto?> GetParcelDetailAsync(
        Func<IQueryable<Parcel>, Task<Parcel?>> loadParcel,
        CancellationToken cancellationToken)
    {
        var parcel = await loadParcel(dbContext.Parcels
            .AsNoTracking()
            .Include(p => p.ShipperAddress)
            .Include(p => p.RecipientAddress)
            .Include(p => p.ChangeHistory)
            .Include(p => p.TrackingEvents)
            .Include(p => p.DeliveryConfirmation)
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot));

        if (parcel is null)
        {
            return null;
        }

        var routeAssignment = await dbContext.Routes
            .AsNoTracking()
            .Where(route => route.Parcels.Any(assignedParcel => assignedParcel.Id == parcel.Id))
            .OrderBy(route => route.Status == RouteStatus.InProgress
                ? 0
                : route.Status == RouteStatus.Dispatched
                    ? 1
                    : route.Status == RouteStatus.Draft
                        ? 2
                        : 3)
            .ThenByDescending(route => route.StartDate)
            .Select(route => new ParcelRouteAssignmentDto
            {
                RouteId = route.Id,
                RouteStatus = route.Status.ToString(),
                StartDate = route.StartDate,
                EndDate = route.EndDate,
                DriverId = route.DriverId,
                DriverName = $"{route.Driver.FirstName} {route.Driver.LastName}".Trim(),
                VehicleId = route.VehicleId,
                VehiclePlate = route.Vehicle.RegistrationPlate,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return parcel.ToDetailDto(routeAssignment);
    }

    public async Task<IReadOnlyList<ParcelLabelDataDto>> GetParcelLabelDataAsync(
        IReadOnlyCollection<Guid> parcelIds,
        CancellationToken cancellationToken = default)
    {
        if (parcelIds.Count == 0)
        {
            return [];
        }

        var parcels = await dbContext.Parcels
            .AsNoTracking()
            .Include(p => p.RecipientAddress)
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .Where(p => parcelIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return parcels
            .Select(parcel => parcel.ToLabelDataDto())
            .ToArray();
    }

    public async Task<IReadOnlyList<TrackingEventDto>> GetTrackingEventsAsync(
        Guid parcelId,
        CancellationToken cancellationToken = default)
    {
        var events = await dbContext.Parcels
            .AsNoTracking()
            .Where(p => p.Id == parcelId)
            .SelectMany(p => p.TrackingEvents)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        return events.Select(e => e.ToDto()).ToList();
    }
}
