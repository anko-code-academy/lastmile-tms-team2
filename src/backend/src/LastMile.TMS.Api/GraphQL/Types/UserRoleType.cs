using HotChocolate.Types;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Api.GraphQL.Types;

public sealed class UserRoleType : EnumType<PredefinedRole>
{
    protected override void Configure(IEnumTypeDescriptor<PredefinedRole> descriptor)
    {
        descriptor.Name("UserRole");

        descriptor.Value(PredefinedRole.Admin).Name(nameof(PredefinedRole.Admin));
        descriptor.Value(PredefinedRole.OperationsManager).Name(nameof(PredefinedRole.OperationsManager));
        descriptor.Value(PredefinedRole.Dispatcher).Name(nameof(PredefinedRole.Dispatcher));
        descriptor.Value(PredefinedRole.WarehouseOperator).Name(nameof(PredefinedRole.WarehouseOperator));
        descriptor.Value(PredefinedRole.Driver).Name(nameof(PredefinedRole.Driver));
    }
}
