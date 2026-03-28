using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Depots;

public sealed class DepotType : EntityObjectType<Depot>
{
    protected override void ConfigureFields(IObjectTypeDescriptor<Depot> descriptor)
    {
        descriptor.Name("Depot");
        descriptor.Field(d => d.Id);
        descriptor.Field(d => d.Name);
        descriptor.Field(d => d.Address).Type<AddressType>();
        descriptor.Field(d => d.OperatingHours).Type<ListType<NonNullType<OperatingHoursType>>>();
        descriptor.Field(d => d.IsActive);
        descriptor.Field(d => d.CreatedAt);
        descriptor.Field(d => d.LastModifiedAt).Name("updatedAt");
    }
}
