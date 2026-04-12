namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed class SortTargetBinDto
{
    public Guid BinLocationId { get; init; }

    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable path: storage zone / aisle / bin.
    /// </summary>
    public string StoragePath { get; init; } = string.Empty;

    public bool IsRecommended { get; init; }
}

/// <summary>
/// Instructions for warehouse sort: delivery zone and candidate bins for the parcel.
/// </summary>
public sealed class ParcelSortInstructionDto
{
    public Guid ParcelId { get; init; }

    public string TrackingNumber { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public Guid DeliveryZoneId { get; init; }

    public string DeliveryZoneName { get; init; } = string.Empty;

    public Guid DepotId { get; init; }

    public string DepotName { get; init; } = string.Empty;

    public bool DeliveryZoneIsActive { get; init; }

    /// <summary>
    /// When false, <see cref="BlockReasonCode"/> explains why sorting is blocked.
    /// </summary>
    public bool CanSort { get; init; }

    public string? BlockReasonCode { get; init; }

    public string? BlockReasonMessage { get; init; }

    public IReadOnlyList<SortTargetBinDto> TargetBins { get; init; } = Array.Empty<SortTargetBinDto>();

    public Guid? RecommendedBinLocationId { get; init; }
}
