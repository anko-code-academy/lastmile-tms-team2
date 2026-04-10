using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Support;

internal static class RouteLoadOutSupport
{
    public static IQueryable<Route> GetLoadOutRoutes(IAppDbContext db, Guid depotId) =>
        db.Routes
            .AsNoTracking()
            .Where(route =>
                route.Status == RouteStatus.Dispatched
                && route.Vehicle.DepotId == depotId
                && route.Parcels.Any(p => p.Status == ParcelStatus.Staged || p.Status == ParcelStatus.Loaded));

    public static IQueryable<Route> GetTrackedLoadOutRoutes(IAppDbContext db, Guid depotId) =>
        db.Routes
            .Where(route =>
                route.Status == RouteStatus.Dispatched
                && route.Vehicle.DepotId == depotId);

    public static Task<RouteLoadOutBoardDto?> LoadBoardAsync(
        IAppDbContext db,
        Guid routeId,
        Guid depotId,
        CancellationToken cancellationToken) =>
        db.Routes
            .AsNoTracking()
            .Where(route =>
                (route.Status == RouteStatus.Dispatched || route.Status == RouteStatus.InProgress)
                && route.Vehicle.DepotId == depotId
                && route.Id == routeId)
            .Select(route => new RouteLoadOutBoardDto
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
                LoadedParcelCount = route.Parcels.Count(p => p.Status == ParcelStatus.Loaded),
                RemainingParcelCount = route.Parcels.Count(p => p.Status == ParcelStatus.Staged),
                ExpectedParcels = route.Parcels
                    .OrderBy(p => p.TrackingNumber)
                    .Select(p => new RouteLoadOutExpectedParcelDto
                    {
                        ParcelId = p.Id,
                        TrackingNumber = p.TrackingNumber,
                        Barcode = p.TrackingNumber,
                        Status = p.Status.ToString(),
                        IsLoaded = p.Status == ParcelStatus.Loaded,
                    })
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);
}
