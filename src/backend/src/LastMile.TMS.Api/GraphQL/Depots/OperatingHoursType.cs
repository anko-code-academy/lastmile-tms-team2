using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Depots;

public sealed class OperatingHoursType : EntityObjectType<OperatingHours>
{
    protected override void ConfigureFields(IObjectTypeDescriptor<OperatingHours> descriptor)
    {
        descriptor.Name("OperatingHours");
        descriptor.Field(o => o.DayOfWeek);
        descriptor.Field(o => o.OpenTime);
        descriptor.Field(o => o.ClosedTime);
        descriptor.Field(o => o.IsClosed);
    }
}
