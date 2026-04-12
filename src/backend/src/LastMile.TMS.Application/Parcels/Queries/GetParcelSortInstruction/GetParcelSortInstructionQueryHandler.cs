using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Parcels.Queries;

public sealed class GetParcelSortInstructionQueryHandler(IAppDbContext db)
    : IRequestHandler<GetParcelSortInstructionQuery, ParcelSortInstructionDto?>
{
    public async Task<ParcelSortInstructionDto?> Handle(
        GetParcelSortInstructionQuery request,
        CancellationToken cancellationToken)
    {
        var barcode = request.TrackingNumber.Trim();
        if (string.IsNullOrEmpty(barcode))
        {
            return null;
        }

        var parcel = await db.Parcels
            .AsNoTracking()
            .Include(p => p.Zone)
                .ThenInclude(z => z!.Depot)
            .FirstOrDefaultAsync(p => p.TrackingNumber == barcode, cancellationToken);

        if (parcel is null)
        {
            return null;
        }

        var zone = parcel.Zone;
        var depot = zone.Depot;

        if (request.DepotId is { } filterDepotId && filterDepotId != depot.Id)
        {
            return BuildInstruction(parcel, zone, depot, canSort: false,
                "WRONG_DEPOT",
                "This parcel belongs to a different depot than the one you selected. Switch depot or move the parcel to the correct facility.");
        }

        if (!zone.IsActive)
        {
            return BuildInstruction(parcel, zone, depot, canSort: false,
                "ZONE_INACTIVE",
                "Delivery zone is inactive. Update zones or reassign the parcel before sorting.");
        }

        if (parcel.Status != ParcelStatus.ReceivedAtDepot)
        {
            return BuildInstruction(parcel, zone, depot, canSort: false,
                "WRONG_STATUS",
                $"Parcel must be in '{ParcelStatus.ReceivedAtDepot}' status to sort. Current status: {parcel.Status}.");
        }

        var (targetBins, recommendedId) = await LoadTargetBinsAsync(parcel.ZoneId, depot.Id, cancellationToken);

        if (targetBins.Count == 0)
        {
            return BuildInstruction(parcel, zone, depot, canSort: false,
                "NO_TARGET_BINS",
                "No active bin locations are linked to this delivery zone in this depot. Configure bin locations (delivery zone assignment) or use exception handling.");
        }

        return new ParcelSortInstructionDto
        {
            ParcelId = parcel.Id,
            TrackingNumber = parcel.TrackingNumber,
            Status = parcel.Status.ToString(),
            DeliveryZoneId = zone.Id,
            DeliveryZoneName = zone.Name,
            DepotId = depot.Id,
            DepotName = depot.Name,
            DeliveryZoneIsActive = zone.IsActive,
            CanSort = true,
            BlockReasonCode = null,
            BlockReasonMessage = null,
            TargetBins = targetBins,
            RecommendedBinLocationId = recommendedId,
        };
    }

    private static ParcelSortInstructionDto BuildInstruction(
        Parcel parcel, Zone zone, Depot depot,
        bool canSort, string blockReasonCode, string blockReasonMessage) => new()
        {
            ParcelId = parcel.Id,
            TrackingNumber = parcel.TrackingNumber,
            Status = parcel.Status.ToString(),
            DeliveryZoneId = zone.Id,
            DeliveryZoneName = zone.Name,
            DepotId = depot.Id,
            DepotName = depot.Name,
            DeliveryZoneIsActive = zone.IsActive,
            CanSort = canSort,
            BlockReasonCode = canSort ? null : blockReasonCode,
            BlockReasonMessage = canSort ? null : blockReasonMessage,
            TargetBins = [],
        };

    private async Task<(List<SortTargetBinDto> Bins, Guid? RecommendedId)> LoadTargetBinsAsync(
        Guid zoneId, Guid depotId, CancellationToken cancellationToken)
    {
        var binRows = await db.BinLocations
            .AsNoTracking()
            .Where(b =>
                b.IsActive
                && b.DeliveryZoneId == zoneId
                && b.StorageAisle.StorageZone.DepotId == depotId)
            .OrderBy(b => b.StorageAisle.StorageZone.Name)
            .ThenBy(b => b.StorageAisle.Name)
            .ThenBy(b => b.Name)
            .Select(b => new
            {
                b.Id,
                b.Name,
                StorageZoneName = b.StorageAisle.StorageZone.Name,
                AisleName = b.StorageAisle.Name,
            })
            .ToListAsync(cancellationToken);

        var targetBins = new List<SortTargetBinDto>(binRows.Count);
        for (var i = 0; i < binRows.Count; i++)
        {
            var row = binRows[i];
            var path = $"{row.StorageZoneName} / {row.AisleName} / {row.Name}";
            targetBins.Add(
                new SortTargetBinDto
                {
                    BinLocationId = row.Id,
                    Name = row.Name,
                    StoragePath = path,
                    IsRecommended = i == 0,
                });
        }

        Guid? recommendedId = targetBins.Count > 0 ? targetBins[0].BinLocationId : null;
        return (targetBins, recommendedId);
    }
}
