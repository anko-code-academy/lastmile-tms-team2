using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Depots;

public sealed class AddressDtoFilterInputType : FilterInputType<Address>
{
    protected override void Configure(IFilterInputTypeDescriptor<Address> descriptor)
    {
        descriptor.Name("AddressDtoFilterInput");
        descriptor.BindFieldsExplicitly();
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

public sealed class AddressDtoSortInputType : SortInputType<Address>
{
    protected override void Configure(ISortInputTypeDescriptor<Address> descriptor)
    {
        descriptor.Name("AddressDtoSortInput");
        descriptor.BindFieldsExplicitly();
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

public sealed class OperatingHoursDtoFilterInputType : FilterInputType<OperatingHours>
{
    protected override void Configure(IFilterInputTypeDescriptor<OperatingHours> descriptor)
    {
        descriptor.Name("OperatingHoursDtoFilterInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(o => o.DayOfWeek);
        descriptor.Field(o => o.OpenTime);
        descriptor.Field(o => o.ClosedTime);
        descriptor.Field(o => o.IsClosed);
    }
}

public sealed class ListFilterInputTypeOfOperatingHoursDtoFilterInputType
    : ListFilterInputType<OperatingHoursDtoFilterInputType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("ListFilterInputTypeOfOperatingHoursDtoFilterInput");
        base.Configure(descriptor);
    }
}

public sealed class DepotDtoFilterInputType : FilterInputType<Depot>
{
    protected override void Configure(IFilterInputTypeDescriptor<Depot> descriptor)
    {
        descriptor.Name("DepotDtoFilterInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(d => d.Id);
        descriptor.Field(d => d.Name);
        descriptor.Field(d => d.Address).Type<AddressDtoFilterInputType>();
        descriptor.Field(d => d.OperatingHours).Type<ListFilterInputTypeOfOperatingHoursDtoFilterInputType>();
        descriptor.Field(d => d.IsActive);
        descriptor.Field(d => d.CreatedAt);
        descriptor.Field(d => d.LastModifiedAt).Name("updatedAt");
    }
}

public sealed class DepotDtoSortInputType : SortInputType<Depot>
{
    protected override void Configure(ISortInputTypeDescriptor<Depot> descriptor)
    {
        descriptor.Name("DepotDtoSortInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(d => d.Id);
        descriptor.Field(d => d.Name);
        descriptor.Field(d => d.Address).Type<AddressDtoSortInputType>();
        descriptor.Field(d => d.IsActive);
        descriptor.Field(d => d.CreatedAt);
        descriptor.Field(d => d.LastModifiedAt).Name("updatedAt");
    }
}
