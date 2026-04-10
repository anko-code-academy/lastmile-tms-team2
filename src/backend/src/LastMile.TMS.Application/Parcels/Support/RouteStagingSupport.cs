using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Support;

internal static class RouteStagingSupport
{
    public static IQueryable<Route> GetActiveDepotRoutes(IAppDbContext db, Guid depotId) =>
        db.Routes
            .AsNoTracking()
            .Where(route =>
                (route.Status == RouteStatus.Draft
                 || route.Status == RouteStatus.Dispatched
                 || route.Status == RouteStatus.InProgress)
                && route.Vehicle.DepotId == depotId);

    public static IQueryable<Route> GetTrackedActiveDepotRoutes(IAppDbContext db, Guid depotId) =>
        db.Routes
            .Where(route =>
                (route.Status == RouteStatus.Draft
                 || route.Status == RouteStatus.Dispatched
                 || route.Status == RouteStatus.InProgress)
                && route.Vehicle.DepotId == depotId);

    public static Task<RouteStagingBoardDto?> LoadBoardAsync(
        IAppDbContext db,
        Guid routeId,
        Guid depotId,
        CancellationToken cancellationToken) =>
        GetActiveDepotRoutes(db, depotId)
            .Where(route => route.Id == routeId)
            .Select(route => new RouteStagingBoardDto
            {
                Id = route.Id,
                VehicleId = route.VehicleId,
                VehiclePlate = route.Vehicle.RegistrationPlate,
                DriverId = route.DriverId,
                DriverName = $"{route.Driver.FirstName} {route.Driver.LastName}".Trim(),
                Status = route.Status,
                StagingArea = route.StagingArea,
                StartDate = route.StartDate,
                ExpectedParcelCount = route.Parcels.Count,
                StagedParcelCount = route.Parcels.Count(parcel => parcel.Status == ParcelStatus.Staged),
                RemainingParcelCount = route.Parcels.Count(parcel => parcel.Status != ParcelStatus.Staged),
                ExpectedParcels = route.Parcels
                    .OrderBy(parcel => parcel.TrackingNumber)
                    .Select(parcel => new RouteStagingExpectedParcelDto
                    {
                        ParcelId = parcel.Id,
                        TrackingNumber = parcel.TrackingNumber,
                        Barcode = parcel.TrackingNumber,
                        Status = parcel.Status.ToString(),
                        IsStaged = parcel.Status == ParcelStatus.Staged,
                    })
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);
}
