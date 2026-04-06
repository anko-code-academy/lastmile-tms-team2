using HotChocolate;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Api.GraphQL.Parcels;

public sealed class ParcelListType : EntityObjectType<Parcel>
{
    protected override void ConfigureFields(IObjectTypeDescriptor<Parcel> descriptor)
    {
        descriptor.Name("RegisteredParcel");
        descriptor.BindFieldsExplicitly();

        // Primary identity and status
        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.TrackingNumber);
        descriptor.Field(p => p.Status).Type<StringType>();
        descriptor.Field(p => p.ServiceType).Type<StringType>();
        descriptor.Field(p => p.ParcelType);

        // Dimensions
        descriptor.Field(p => p.Weight);
        descriptor.Field(p => p.WeightUnit).Type<StringType>();
        descriptor.Field(p => p.Length);
        descriptor.Field(p => p.Width);
        descriptor.Field(p => p.Height);
        descriptor.Field(p => p.DimensionUnit).Type<StringType>();

        // Value
        descriptor.Field(p => p.DeclaredValue);
        descriptor.Field(p => p.Currency);

        // Delivery
        descriptor.Field(p => p.EstimatedDeliveryDate);
        descriptor.Field(p => p.ActualDeliveryDate);
        descriptor.Field(p => p.DeliveryAttempts);

        // Description
        descriptor.Field(p => p.Description);

        // Zone — projected so EF JOINs it; nested field resolver flattens to zoneName
        descriptor.Field(p => p.ZoneId);
        descriptor.Field(p => p.Zone)
            .IsProjected(true)
            .Resolve(ctx => ctx.Parent<Parcel>().Zone)
            .Type<ObjectType<Zone>>();
        descriptor.Field("zoneName")
            .Type<StringType>()
            .Resolve(ctx => ctx.Parent<Parcel>().Zone?.Name);

        // Depot via Zone
        descriptor.Field("depotId")
            .Type<UuidType>()
            .Resolve(ctx => ctx.Parent<Parcel>().Zone?.DepotId ?? Guid.Empty);
        descriptor.Field("depotName")
            .Type<StringType>()
            .Resolve(ctx => ctx.Parent<Parcel>().Zone?.Depot?.Name);

        // Recipient address — projected so EF JOINs it; all recipient fields exposed at root level (matching original contract)
        descriptor.Field(p => p.RecipientAddress).IsProjected(true);
        descriptor.Field("recipientContactName")
            .Type<StringType>()
            .Resolve(ctx => ctx.Parent<Parcel>().RecipientAddress?.ContactName);
        descriptor.Field("recipientCompanyName")
            .Type<StringType>()
            .Resolve(ctx => ctx.Parent<Parcel>().RecipientAddress?.CompanyName);
        descriptor.Field("recipientStreet1")
            .Type<StringType>()
            .Resolve(ctx => ctx.Parent<Parcel>().RecipientAddress?.Street1);
        descriptor.Field("recipientCity")
            .Type<StringType>()
            .Resolve(ctx => ctx.Parent<Parcel>().RecipientAddress?.City);
        descriptor.Field("recipientPostalCode")
            .Type<StringType>()
            .Resolve(ctx => ctx.Parent<Parcel>().RecipientAddress?.PostalCode);

        // Timestamps
        descriptor.Field(p => p.CreatedAt);
        descriptor.Field(p => p.LastModifiedAt);
    }
}

public sealed class ParcelRecipientAddressType : ObjectType<Address>
{
    protected override void Configure(IObjectTypeDescriptor<Address> descriptor)
    {
        descriptor.Name("ParcelRecipientAddress");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(a => a.ContactName).Name("recipientContactName");
        descriptor.Field(a => a.CompanyName).Name("recipientCompanyName");
        descriptor.Field(a => a.Street1).Name("recipientStreet1");
        descriptor.Field(a => a.City).Name("recipientCity");
        descriptor.Field(a => a.PostalCode).Name("recipientPostalCode");
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

public sealed class TrackingEventType : ObjectType<TrackingEventDto>
{
    protected override void Configure(IObjectTypeDescriptor<TrackingEventDto> descriptor)
    {
        descriptor.Name("TrackingEvent");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(e => e.Id);
        descriptor.Field(e => e.Timestamp);
        descriptor.Field(e => e.EventType);
        descriptor.Field(e => e.Description);
        descriptor.Field(e => e.Location);
        descriptor.Field(e => e.Operator);
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
        descriptor.Field(p => p.ParcelType);
        descriptor.Field(p => p.Weight);
        descriptor.Field(p => p.CreatedAt);
        descriptor.Field(p => p.LastModifiedAt);
        descriptor.Field(p => p.EstimatedDeliveryDate);
        descriptor.Field(p => p.RecipientAddress).Name("recipientContactName");
        descriptor.Field(p => p.Zone).Name("zoneName");
    }
}
