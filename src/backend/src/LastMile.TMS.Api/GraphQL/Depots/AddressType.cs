using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Depots;

public sealed class AddressType : EntityObjectType<Address>
{
    protected override void ConfigureFields(IObjectTypeDescriptor<Address> descriptor)
    {
        descriptor.Name("Address");
        descriptor.Field(a => a.Street1);
        descriptor.Field(a => a.Street2);
        descriptor.Field(a => a.City);
        descriptor.Field(a => a.State);
        descriptor.Field(a => a.PostalCode);
        descriptor.Field(a => a.CountryCode);
        descriptor.Field(a => a.IsResidential);
        descriptor.Field(a => a.ContactName);
        descriptor.Field(a => a.CompanyName);
        descriptor.Field(a => a.Phone);
        descriptor.Field(a => a.Email);
    }
}
