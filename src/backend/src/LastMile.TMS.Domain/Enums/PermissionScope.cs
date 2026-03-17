namespace LastMile.TMS.Domain.Enums;

[Flags]
public enum PermissionScope
{
    Read   = 1,
    Write  = 2,
    Delete = 4,
    All    = Read | Write | Delete
}
