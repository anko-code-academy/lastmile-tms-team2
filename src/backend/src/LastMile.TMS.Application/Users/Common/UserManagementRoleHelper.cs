using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Users.Common;

internal static class UserManagementRoleHelper
{
    public static string ToIdentityRoleName(PredefinedRole role) => role.ToString();

    public static PredefinedRole? Parse(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        return Enum.TryParse<PredefinedRole>(roleName, ignoreCase: true, out var role)
            ? role
            : null;
    }

    public static string ToDisplayName(PredefinedRole role) =>
        role switch
        {
            PredefinedRole.Admin => "Admin",
            PredefinedRole.OperationsManager => "Operations Manager",
            PredefinedRole.Dispatcher => "Dispatcher",
            PredefinedRole.WarehouseOperator => "Warehouse Operator",
            PredefinedRole.Driver => "Driver",
            _ => role.ToString()
        };
}
