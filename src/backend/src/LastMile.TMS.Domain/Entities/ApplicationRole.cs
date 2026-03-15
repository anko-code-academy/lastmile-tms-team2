using LastMile.TMS.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace LastMile.TMS.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsDefault { get; set; }

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
