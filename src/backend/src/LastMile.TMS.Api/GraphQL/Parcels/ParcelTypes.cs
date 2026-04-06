using HotChocolate;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Api.GraphQL.Parcels;

public sealed class ParcelListType : EntityObjectType<Parcel>
{
    protected override void ConfigureFields(IObjectTypeDescriptor<Parcel> descriptor)
    {
        descriptor.Name("RegisteredParcel");
        descriptor.BindFieldsExplicitly();

        // Primary identity and status
        descriptor.Field(p => p.Id).IsProjected(true);
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

        // Zone and depot labels are loaded by Parcel.Id so paging/projection do not depend on composite selections.
        descriptor.Field(p => p.ZoneId).IsProjected(true);
        descriptor.Field("zoneName")
            .Type<StringType>()
            .Resolve(async ctx => (await LoadParcelListLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).ZoneName);

        descriptor.Field("depotId")
            .Type<UuidType>()
            .Resolve(async ctx => (await LoadParcelListLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).DepotId);
        descriptor.Field("depotName")
            .Type<StringType>()
            .Resolve(async ctx => (await LoadParcelListLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).DepotName);

        // Recipient address labels stay flattened at the root to match the existing frontend contract.
        descriptor.Field("recipientContactName")
            .Type<StringType>()
            .Resolve(async ctx => (await LoadParcelListLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).RecipientContactName);
        descriptor.Field("recipientCompanyName")
            .Type<StringType>()
            .Resolve(async ctx => (await LoadParcelListLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).RecipientCompanyName);
        descriptor.Field("recipientStreet1")
            .Type<StringType>()
            .Resolve(async ctx => (await LoadParcelListLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).RecipientStreet1);
        descriptor.Field("recipientCity")
            .Type<StringType>()
            .Resolve(async ctx => (await LoadParcelListLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).RecipientCity);
        descriptor.Field("recipientPostalCode")
            .Type<StringType>()
            .Resolve(async ctx => (await LoadParcelListLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).RecipientPostalCode);

        // Timestamps
        descriptor.Field(p => p.CreatedAt);
        descriptor.Field(p => p.LastModifiedAt);
    }

    private sealed record ParcelListLabels(
        string? ZoneName,
        Guid? DepotId,
        string? DepotName,
        string? RecipientContactName,
        string? RecipientCompanyName,
        string? RecipientStreet1,
        string? RecipientCity,
        string? RecipientPostalCode)
    {
        public static ParcelListLabels Empty { get; } =
            new(null, null, null, null, null, null, null, null);
    }

    private static async Task<ParcelListLabels> LoadParcelListLabelsAsync(
        IResolverContext ctx,
        Guid parcelId)
    {
        var labels = await ctx.BatchDataLoader<Guid, ParcelListLabels>(
                async (parcelIds, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var rows = await dbContext.Parcels
                        .AsNoTracking()
                        .Where(p => parcelIds.Contains(p.Id))
                        .Select(p => new
                        {
                            p.Id,
                            ZoneName = p.Zone.Name,
                            DepotId = (Guid?)p.Zone.DepotId,
                            DepotName = p.Zone.Depot.Name,
                            RecipientContactName = p.RecipientAddress.ContactName,
                            RecipientCompanyName = p.RecipientAddress.CompanyName,
                            RecipientStreet1 = p.RecipientAddress.Street1,
                            RecipientCity = p.RecipientAddress.City,
                            RecipientPostalCode = p.RecipientAddress.PostalCode,
                        })
                        .ToListAsync(ct);

                    return parcelIds.ToDictionary(
                        id => id,
                        id =>
                        {
                            var row = rows.FirstOrDefault(r => r.Id == id);
                            return row is null
                                ? ParcelListLabels.Empty
                                : new ParcelListLabels(
                                    row.ZoneName,
                                    row.DepotId,
                                    row.DepotName,
                                    row.RecipientContactName,
                                    row.RecipientCompanyName,
                                    row.RecipientStreet1,
                                    row.RecipientCity,
                                    row.RecipientPostalCode);
                        });
                },
                "ParcelListLabelsByParcelId")
            .LoadAsync(parcelId);

        return labels ?? ParcelListLabels.Empty;
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
        descriptor.Field(p => p.Id).IsProjected(true);
        descriptor.Field(p => p.TrackingNumber);
        descriptor.Field(p => p.Weight);
        descriptor.Field(p => p.WeightUnit).Type<StringType>();
        descriptor.Field(p => p.ZoneId).IsProjected(true);
        descriptor.Field("zoneName")
            .Type<StringType>()
            .Resolve(async ctx => (await LoadRouteOptionLabelsAsync(ctx, ctx.Parent<Parcel>().Id)).ZoneName);
    }

    private sealed record ParcelRouteOptionLabels(string? ZoneName)
    {
        public static ParcelRouteOptionLabels Empty { get; } = new((string?)null);
    }

    private static async Task<ParcelRouteOptionLabels> LoadRouteOptionLabelsAsync(
        IResolverContext ctx,
        Guid parcelId)
    {
        var labels = await ctx.BatchDataLoader<Guid, ParcelRouteOptionLabels>(
                async (parcelIds, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var rows = await dbContext.Parcels
                        .AsNoTracking()
                        .Where(p => parcelIds.Contains(p.Id))
                        .Select(p => new
                        {
                            p.Id,
                            ZoneName = p.Zone.Name,
                        })
                        .ToListAsync(ct);

                    return parcelIds.ToDictionary(
                        id => id,
                        id =>
                        {
                            var row = rows.FirstOrDefault(r => r.Id == id);
                            return row is null
                                ? ParcelRouteOptionLabels.Empty
                                : new ParcelRouteOptionLabels(row.ZoneName);
                        });
                },
                "ParcelRouteOptionLabelsByParcelId")
            .LoadAsync(parcelId);

        return labels ?? ParcelRouteOptionLabels.Empty;
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
