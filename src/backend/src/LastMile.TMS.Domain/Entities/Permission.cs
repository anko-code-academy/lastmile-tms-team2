using LastMile.TMS.Domain.Common;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Entities;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public PermissionScope Scope { get; set; }

    public virtual ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
}