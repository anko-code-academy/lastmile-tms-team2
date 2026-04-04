using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Parcels;

public sealed class ParcelType : ObjectType<ParcelDto>
{
    protected override void Configure(IObjectTypeDescriptor<ParcelDto> descriptor)
    {
        descriptor.Name("RegisteredParcel");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class ParcelDetailType : ObjectType<ParcelDetailDto>
{
    protected override void Configure(IObjectTypeDescriptor<ParcelDetailDto> descriptor)
    {
        descriptor.Name("ParcelDetail");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class ParcelDetailAddressType : ObjectType<ParcelDetailAddressDto>
{
    protected override void Configure(IObjectTypeDescriptor<ParcelDetailAddressDto> descriptor)
    {
        descriptor.Name("ParcelDetailAddress");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class ParcelChangeHistoryType : ObjectType<ParcelChangeHistoryDto>
{
    protected override void Configure(IObjectTypeDescriptor<ParcelChangeHistoryDto> descriptor)
    {
        descriptor.Name("ParcelChangeHistory");
        descriptor.BindFieldsImplicitly();
    }
}
public sealed class ParcelImportHistoryType : ObjectType<ParcelImportHistoryDto>
{
    protected override void Configure(IObjectTypeDescriptor<ParcelImportHistoryDto> descriptor)
    {
        descriptor.Name("ParcelImportHistory");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class ParcelImportDetailType : ObjectType<ParcelImportDetailDto>
{
    protected override void Configure(IObjectTypeDescriptor<ParcelImportDetailDto> descriptor)
    {
        descriptor.Name("ParcelImport");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class ParcelImportRowFailurePreviewType : ObjectType<ParcelImportRowFailurePreviewDto>
{
    protected override void Configure(IObjectTypeDescriptor<ParcelImportRowFailurePreviewDto> descriptor)
    {
        descriptor.Name("ParcelImportRowFailurePreview");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class ParcelRouteOptionType : EntityObjectType<Parcel>
{
    protected override void ConfigureFields(IObjectTypeDescriptor<Parcel> descriptor)
    {
        descriptor.Name("ParcelRouteOption");
        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.TrackingNumber);
        descriptor.Field(p => p.Weight);
        descriptor.Field(p => p.WeightUnit);
    }
}

public sealed class ParcelFilterInputType : FilterInputType<Parcel>
{
    protected override void Configure(IFilterInputTypeDescriptor<Parcel> descriptor)
    {
        descriptor.Name("ParcelFilterInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.TrackingNumber);
        descriptor.Field(p => p.ZoneId);
        descriptor.Field(p => p.Status);
        descriptor.Field(p => p.ServiceType);
        descriptor.Field(p => p.Weight);
        descriptor.Field(p => p.WeightUnit);
        descriptor.Field(p => p.Length);
        descriptor.Field(p => p.Width);
        descriptor.Field(p => p.Height);
        descriptor.Field(p => p.DimensionUnit);
        descriptor.Field(p => p.DeclaredValue);
        descriptor.Field(p => p.Currency);
        descriptor.Field(p => p.ParcelType);
        descriptor.Field(p => p.Description);
        descriptor.Field(p => p.DeliveryAttempts);
        descriptor.Field(p => p.EstimatedDeliveryDate);
        descriptor.Field(p => p.CreatedAt);
        descriptor.Field(p => p.LastModifiedAt);
    }
}

public sealed class ParcelSortInputType : SortInputType<Parcel>
{
    protected override void Configure(ISortInputTypeDescriptor<Parcel> descriptor)
    {
        descriptor.Name("ParcelSortInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.TrackingNumber);
        descriptor.Field(p => p.Status);
        descriptor.Field(p => p.ServiceType);
        descriptor.Field(p => p.Weight);
        descriptor.Field(p => p.CreatedAt);
        descriptor.Field(p => p.LastModifiedAt);
    }
}
