using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Entities;

public class Driver : BaseAuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Contact info
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // License
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTimeOffset LicenseExpiryDate { get; set; }

    // Photo
    public string? PhotoUrl { get; set; }

    // Zone assignment
    public Guid ZoneId { get; set; }
    public Zone Zone { get; set; } = null!;

    // Depot assignment
    public Guid DepotId { get; set; }
    public Depot Depot { get; set; } = null!;

    // Status
    public DriverStatus Status { get; set; } = DriverStatus.Active;

    // Link to User account (for mobile app login)
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Availability calendar
    public ICollection<DriverAvailability> AvailabilitySchedule { get; set; } = new List<DriverAvailability>();

    /// <summary>
    /// Checks whether the driver's license has expired as of the given date.
    /// </summary>
    public bool IsLicenseExpired(DateTimeOffset asOfDate)
    {
        return LicenseExpiryDate < asOfDate;
    }
}
