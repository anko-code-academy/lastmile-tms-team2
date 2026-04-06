using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Parcels.Support;

/// <summary>
/// Maps domain <see cref="ParcelStatus"/> values to GraphQL enum names (UPPER_SNAKE_CASE).
/// </summary>
public static partial class ParcelStatusGraphQl
{
    public static string ToGraphQlName(ParcelStatus status) => status switch
    {
        ParcelStatus.Registered => "REGISTERED",
        ParcelStatus.ReceivedAtDepot => "RECEIVED_AT_DEPOT",
        ParcelStatus.Sorted => "SORTED",
        ParcelStatus.Staged => "STAGED",
        ParcelStatus.Loaded => "LOADED",
        ParcelStatus.OutForDelivery => "OUT_FOR_DELIVERY",
        ParcelStatus.Delivered => "DELIVERED",
        ParcelStatus.FailedAttempt => "FAILED_ATTEMPT",
        ParcelStatus.ReturnedToDepot => "RETURNED_TO_DEPOT",
        ParcelStatus.Cancelled => "CANCELLED",
        ParcelStatus.Exception => "EXCEPTION",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
