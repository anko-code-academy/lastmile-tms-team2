using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using RouteEntity = LastMile.TMS.Domain.Entities.Route;

namespace LastMile.TMS.Api.GraphQL.Routes;

public sealed class RouteType : EntityObjectType<RouteEntity>
{
    protected override void ConfigureFields(IObjectTypeDescriptor<RouteEntity> descriptor)
    {
        descriptor.Name("Route");
        descriptor.Field(r => r.Id).IsProjected(true);
        // FKs: keep projected when clients request vehicleId/driverId; also useful for filters.
        descriptor.Field(r => r.VehicleId).IsProjected(true);
        descriptor.Field(r => r.DriverId).IsProjected(true);
        // Resolve plate/name by Route.Id + EF join — does not rely on VehicleId/DriverId on the
        // (possibly partial) parent entity when UseProjection materializes Route.
        descriptor.Field("vehiclePlate")
            .Type<StringType>()
            .Resolve(async ctx =>
                (await LoadRouteLabelsAsync(ctx, ctx.Parent<RouteEntity>().Id)).VehiclePlate);
        descriptor.Field("driverName")
            .Type<StringType>()
            .Resolve(async ctx =>
                (await LoadRouteLabelsAsync(ctx, ctx.Parent<RouteEntity>().Id)).DriverName);
        descriptor.Field(r => r.StartDate);
        descriptor.Field(r => r.EndDate);
        descriptor.Field(r => r.StartMileage);
        descriptor.Field(r => r.EndMileage);
        descriptor.Field("totalMileage")
            .Type<NonNullType<IntType>>()
            .Resolve(ctx =>
            {
                var route = ctx.Parent<RouteEntity>();
                return route.EndMileage > 0 ? route.EndMileage - route.StartMileage : 0;
            });
        descriptor.Field(r => r.Status);
        descriptor.Field("parcelCount")
            .Type<NonNullType<IntType>>()
            .Resolve(async ctx => (await LoadParcelStatsAsync(ctx) ?? RouteParcelStats.Empty(ctx.Parent<RouteEntity>().Id)).ParcelCount);
        descriptor.Field("parcelsDelivered")
            .Type<NonNullType<IntType>>()
            .Resolve(async ctx => (await LoadParcelStatsAsync(ctx) ?? RouteParcelStats.Empty(ctx.Parent<RouteEntity>().Id)).ParcelsDelivered);
        descriptor.Field(r => r.CreatedAt);
    }

    /// <summary>
    /// Single batch loader for plate and driver name. Two separate string batch loaders with the
    /// same Guid key risk mixing results between GraphQL fields in Hot Chocolate.
    /// </summary>
    private static async Task<RouteLabels> LoadRouteLabelsAsync(IResolverContext ctx, Guid routeId)
    {
        var labels = await ctx.BatchDataLoader<Guid, RouteLabels>(
                async (routeIds, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var rows = await dbContext.Routes
                        .AsNoTracking()
                        .Where(r => routeIds.Contains(r.Id))
                        .Select(r => new
                        {
                            r.Id,
                            Plate = r.Vehicle.RegistrationPlate,
                            DriverName = $"{r.Driver.FirstName} {r.Driver.LastName}".Trim(),
                        })
                        .ToListAsync(ct);

                    return routeIds.ToDictionary(
                        id => id,
                        id =>
                        {
                            var row = rows.FirstOrDefault(x => x.Id == id);
                            return row is null
                                ? RouteLabels.Empty
                                : new RouteLabels(row.Plate, row.DriverName);
                        });
                })
            .LoadAsync(routeId);

        return labels ?? RouteLabels.Empty;
    }

    private sealed record RouteLabels(string VehiclePlate, string DriverName)
    {
        public static RouteLabels Empty { get; } = new(string.Empty, string.Empty);
    }

    private static Task<RouteParcelStats?> LoadParcelStatsAsync(IResolverContext ctx)
    {
        var routeId = ctx.Parent<RouteEntity>().Id;

        return ctx.BatchDataLoader<Guid, RouteParcelStats>(
                async (ids, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var stats = await dbContext.Routes
                        .AsNoTracking()
                        .Where(r => ids.Contains(r.Id))
                        .Select(r => new RouteParcelStats(
                            r.Id,
                            r.Parcels.Count,
                            r.Parcels.Count(p => p.Status == ParcelStatus.Delivered)))
                        .ToListAsync(ct);

                    return ids.ToDictionary(
                        id => id,
                        id => stats.FirstOrDefault(s => s.RouteId == id) ?? RouteParcelStats.Empty(id));
                })
            .LoadAsync(routeId);
    }

    private sealed record RouteParcelStats(Guid RouteId, int ParcelCount, int ParcelsDelivered)
    {
        public static RouteParcelStats Empty(Guid routeId) => new(routeId, 0, 0);
    }
}

public sealed class RouteFilterInputType : FilterInputType<RouteEntity>
{
    protected override void Configure(IFilterInputTypeDescriptor<RouteEntity> descriptor)
    {
        descriptor.Name("RouteFilterInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(r => r.Id);
        descriptor.Field(r => r.VehicleId);
        descriptor.Field(r => r.DriverId);
        descriptor.Field(r => r.StartDate);
        descriptor.Field(r => r.EndDate);
        descriptor.Field(r => r.StartMileage);
        descriptor.Field(r => r.EndMileage);
        descriptor.Field(r => r.Status);
        descriptor.Field(r => r.CreatedAt);
    }
}

public sealed class RouteSortInputType : SortInputType<RouteEntity>
{
    protected override void Configure(ISortInputTypeDescriptor<RouteEntity> descriptor)
    {
        descriptor.Name("RouteSortInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(r => r.Id);
        descriptor.Field(r => r.VehicleId);
        descriptor.Field(r => r.DriverId);
        descriptor.Field(r => r.StartDate);
        descriptor.Field(r => r.EndDate);
        descriptor.Field(r => r.StartMileage);
        descriptor.Field(r => r.EndMileage);
        descriptor.Field(r => r.Status);
        descriptor.Field(r => r.CreatedAt);
    }
}
