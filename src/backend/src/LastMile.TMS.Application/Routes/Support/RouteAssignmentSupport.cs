using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Routes.Support;

internal static class RouteAssignmentSupport
{
    internal static readonly RouteStatus[] ActiveAssignmentStatuses =
    [
        RouteStatus.Draft,
        RouteStatus.Dispatched,
        RouteStatus.InProgress,
    ];

    internal static DateTimeOffset NormalizeUtc(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero
            ? value
            : value.ToUniversalTime();

    internal static DateTimeOffset GetServiceDayStart(DateTimeOffset serviceDate) =>
        NormalizeUtc(
            new DateTimeOffset(
                serviceDate.Year,
                serviceDate.Month,
                serviceDate.Day,
                0,
                0,
                0,
                serviceDate.Offset));

    internal static DateTimeOffset GetServiceDayEnd(DateTimeOffset serviceDate) =>
        NormalizeUtc(
            new DateTimeOffset(
                serviceDate.Year,
                serviceDate.Month,
                serviceDate.Day,
                0,
                0,
                0,
                serviceDate.Offset).AddDays(1));

    internal static bool IsVehicleAssignableStatus(VehicleStatus status) =>
        status != VehicleStatus.Maintenance && status != VehicleStatus.Retired;

    internal static bool IsDriverScheduleCompatible(
        IEnumerable<DriverAvailability>? availabilitySchedule,
        DateTimeOffset serviceDate)
    {
        var entries = availabilitySchedule?.ToList() ?? [];
        if (entries.Count == 0)
        {
            return true;
        }

        var entry = entries.FirstOrDefault(x => x.DayOfWeek == serviceDate.DayOfWeek);
        return entry is null || entry.IsAvailable;
    }

    internal static decimal GetTotalWeightKg(IEnumerable<Parcel> parcels) =>
        parcels.Sum(parcel => parcel.WeightUnit == WeightUnit.Lb
            ? parcel.Weight * 0.453592m
            : parcel.Weight);

    internal static bool DoParcelsFitVehicle(IReadOnlyCollection<Parcel> parcels, Vehicle vehicle)
    {
        return parcels.Count <= vehicle.ParcelCapacity
            && GetTotalWeightKg(parcels) <= vehicle.WeightCapacity;
    }

    internal static RouteAssignmentAuditEntry CreateAuditEntry(
        Guid routeId,
        RouteAssignmentAuditAction action,
        Driver newDriver,
        Vehicle newVehicle,
        string? changedBy,
        Driver? previousDriver = null,
        Vehicle? previousVehicle = null)
    {
        return new RouteAssignmentAuditEntry
        {
            Id = Guid.NewGuid(),
            RouteId = routeId,
            Action = action,
            PreviousDriverId = previousDriver?.Id,
            PreviousDriverName = previousDriver is null ? null : FormatDriverName(previousDriver),
            NewDriverId = newDriver.Id,
            NewDriverName = FormatDriverName(newDriver),
            PreviousVehicleId = previousVehicle?.Id,
            PreviousVehiclePlate = previousVehicle?.RegistrationPlate,
            NewVehicleId = newVehicle.Id,
            NewVehiclePlate = newVehicle.RegistrationPlate,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = changedBy,
        };
    }

    internal static string FormatDriverName(Driver driver) =>
        $"{driver.FirstName} {driver.LastName}".Trim();
}
