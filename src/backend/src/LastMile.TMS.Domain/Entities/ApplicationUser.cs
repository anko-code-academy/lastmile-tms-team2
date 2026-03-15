using LastMile.TMS.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace LastMile.TMS.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? ZoneId { get; set; }

    // Audit fields (cannot inherit BaseAuditableEntity since base is IdentityUser)
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
}
