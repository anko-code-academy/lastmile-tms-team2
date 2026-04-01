using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;

namespace LastMile.TMS.Api.GraphQL.Zones;

public sealed class ZoneType : EntityObjectType<Zone>
{
    protected override void ConfigureFields(IObjectTypeDescriptor<Zone> descriptor)
    {
        descriptor.Name("Zone");
        descriptor.Field(z => z.Id);
        descriptor.Field(z => z.Name);
        descriptor.Field(z => z.Boundary)
            .IsProjected(true)
            .Name("boundary")
            .Type<StringType>()
            .Resolve(ctx =>
            {
                var zone = ctx.Parent<Zone>();
                return zone.Boundary?.AsText();
            });
        descriptor.Field("boundaryGeoJson")
            .Name("boundaryGeoJson")
            .Type<StringType>()
            .Resolve(ctx =>
            {
                var zone = ctx.Parent<Zone>();
                return zone.Boundary is null ? null : new GeoJsonWriter().Write(zone.Boundary);
            });
        descriptor.Field(z => z.IsActive);
        descriptor.Field(z => z.DepotId).IsProjected(true);
        descriptor.Field("depotName")
            .Type<StringType>()
            .Resolve(async ctx => await LoadDepotNameAsync(ctx, ctx.Parent<Zone>().DepotId));
        descriptor.Field(z => z.CreatedAt);
        descriptor.Field(z => z.LastModifiedAt).Name("updatedAt");
    }

    private static Task<string?> LoadDepotNameAsync(IResolverContext ctx, Guid depotId) =>
        ctx.BatchDataLoader<Guid, string?>(
                async (ids, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var depots = await dbContext.Depots
                        .AsNoTracking()
                        .Where(d => ids.Contains(d.Id))
                        .Select(d => new { d.Id, d.Name })
                        .ToListAsync(ct);

                    return ids.ToDictionary(
                        id => id,
                        id => depots.FirstOrDefault(d => d.Id == id)?.Name);
                })
            .LoadAsync(depotId);
}

public sealed class ZoneFilterInputType : FilterInputType<Zone>
{
    protected override void Configure(IFilterInputTypeDescriptor<Zone> descriptor)
    {
        descriptor.Name("ZoneFilterInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(z => z.Id);
        descriptor.Field(z => z.Name);
        descriptor.Field(z => z.IsActive);
        descriptor.Field(z => z.DepotId);
        descriptor.Field(z => z.CreatedAt);
        descriptor.Field(z => z.LastModifiedAt).Name("updatedAt");
    }
}

public sealed class ZoneSortInputType : SortInputType<Zone>
{
    protected override void Configure(ISortInputTypeDescriptor<Zone> descriptor)
    {
        descriptor.Name("ZoneSortInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(z => z.Id);
        descriptor.Field(z => z.Name);
        descriptor.Field(z => z.IsActive);
        descriptor.Field(z => z.DepotId);
        descriptor.Field(z => z.CreatedAt);
        descriptor.Field(z => z.LastModifiedAt).Name("updatedAt");
    }
}
