using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Support;

internal static class DepotParcelInventorySupport
{
    internal static readonly ParcelStatus[] InventoryStatuses =
    [
        ParcelStatus.ReceivedAtDepot,
        ParcelStatus.Sorted,
        ParcelStatus.Staged,
        ParcelStatus.Loaded,
        ParcelStatus.Exception,
    ];

    public static IQueryable<Parcel> GetDepotInventoryParcels(
        IAppDbContext db,
        Guid depotId) =>
        db.Parcels
            .AsNoTracking()
            .Where(parcel =>
                InventoryStatuses.Contains(parcel.Status)
                && parcel.Zone.DepotId == depotId);

    public static bool IsInventoryStatus(ParcelStatus status) =>
        InventoryStatuses.Contains(status);
}
