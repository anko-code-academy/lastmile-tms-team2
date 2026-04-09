using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Domain.Entities;
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
        descriptor.Field(r => r.VehicleId).IsProjected(true);
        descriptor.Field(r => r.DriverId).IsProjected(true);
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
        descriptor.Field(r => r.StagingArea);
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
            .Resolve(async ctx =>
                (await LoadParcelStatsAsync(ctx) ?? RouteParcelStats.Empty(ctx.Parent<RouteEntity>().Id)).ParcelCount);
        descriptor.Field("parcelsDelivered")
            .Type<NonNullType<IntType>>()
            .Resolve(async ctx =>
                (await LoadParcelStatsAsync(ctx) ?? RouteParcelStats.Empty(ctx.Parent<RouteEntity>().Id)).ParcelsDelivered);
        descriptor.Field(r => r.CreatedAt);
        descriptor.Field(r => r.LastModifiedAt).Name("updatedAt");
        descriptor.Field(r => r.AssignmentAuditTrail)
            .Type<NonNullType<ListType<NonNullType<RouteAssignmentAuditEntryType>>>>()
            .Resolve(async ctx => await LoadAssignmentAuditTrailAsync(ctx));
    }

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
                },
                "RouteLabelsByRouteId")
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
                },
                "RouteParcelStatsByRouteId")
            .LoadAsync(routeId);
    }

    private sealed record RouteParcelStats(Guid RouteId, int ParcelCount, int ParcelsDelivered)
    {
        public static RouteParcelStats Empty(Guid routeId) => new(routeId, 0, 0);
    }

    private static Task<List<RouteAssignmentAuditEntry>> LoadAssignmentAuditTrailAsync(IResolverContext ctx)
    {
        var routeId = ctx.Parent<RouteEntity>().Id;

        return LoadAsync();

        async Task<List<RouteAssignmentAuditEntry>> LoadAsync()
        {
            var rows = await ctx.BatchDataLoader<Guid, List<RouteAssignmentAuditEntry>>(
                    async (ids, ct) =>
                    {
                        var dbContext = ctx.Service<IAppDbContext>();
                        var auditRows = await dbContext.RouteAssignmentAuditEntries
                            .AsNoTracking()
                            .Where(entry => ids.Contains(entry.RouteId))
                            .OrderByDescending(entry => entry.ChangedAt)
                            .ToListAsync(ct);

                        return ids.ToDictionary(
                            id => id,
                            id => auditRows.Where(entry => entry.RouteId == id).ToList());
                    },
                    "RouteAssignmentAuditTrailByRouteId")
                .LoadAsync(routeId);

            return rows ?? [];
        }
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
        descriptor.Field(r => r.StagingArea);
        descriptor.Field(r => r.Status);
        descriptor.Field(r => r.CreatedAt);
        descriptor.Field(r => r.LastModifiedAt).Name("updatedAt");
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
        descriptor.Field(r => r.StagingArea);
        descriptor.Field(r => r.Status);
        descriptor.Field(r => r.CreatedAt);
        descriptor.Field(r => r.LastModifiedAt).Name("updatedAt");
    }
}

public sealed class RouteAssignmentAuditEntryType : ObjectType<RouteAssignmentAuditEntry>
{
    protected override void Configure(IObjectTypeDescriptor<RouteAssignmentAuditEntry> descriptor)
    {
        descriptor.Name("RouteAssignmentAuditEntry");
        descriptor.Field(x => x.Id);
        descriptor.Field(x => x.Action);
        descriptor.Field(x => x.PreviousDriverId);
        descriptor.Field(x => x.PreviousDriverName);
        descriptor.Field(x => x.NewDriverId);
        descriptor.Field(x => x.NewDriverName);
        descriptor.Field(x => x.PreviousVehicleId);
        descriptor.Field(x => x.PreviousVehiclePlate);
        descriptor.Field(x => x.NewVehicleId);
        descriptor.Field(x => x.NewVehiclePlate);
        descriptor.Field(x => x.ChangedAt);
        descriptor.Field(x => x.ChangedBy);
    }
}

public sealed class RouteAssignmentCandidatesType : ObjectType<RouteAssignmentCandidatesDto>
{
    protected override void Configure(IObjectTypeDescriptor<RouteAssignmentCandidatesDto> descriptor)
    {
        descriptor.Name("RouteAssignmentCandidates");
        descriptor.Field(x => x.Vehicles)
            .Type<NonNullType<ListType<NonNullType<AssignableVehicleType>>>>();
        descriptor.Field(x => x.Drivers)
            .Type<NonNullType<ListType<NonNullType<AssignableDriverType>>>>();
    }
}

public sealed class AssignableVehicleType : ObjectType<AssignableVehicleDto>
{
    protected override void Configure(IObjectTypeDescriptor<AssignableVehicleDto> descriptor)
    {
        descriptor.Name("AssignableVehicle");
        descriptor.Field(x => x.Id);
        descriptor.Field(x => x.RegistrationPlate);
        descriptor.Field(x => x.DepotId);
        descriptor.Field(x => x.DepotName);
        descriptor.Field(x => x.ParcelCapacity);
        descriptor.Field(x => x.WeightCapacity);
        descriptor.Field(x => x.Status);
        descriptor.Field(x => x.IsCurrentAssignment);
    }
}

public sealed class AssignableDriverType : ObjectType<AssignableDriverDto>
{
    protected override void Configure(IObjectTypeDescriptor<AssignableDriverDto> descriptor)
    {
        descriptor.Name("AssignableDriver");
        descriptor.Field(x => x.Id);
        descriptor.Field(x => x.DisplayName);
        descriptor.Field(x => x.DepotId);
        descriptor.Field(x => x.ZoneId);
        descriptor.Field(x => x.Status);
        descriptor.Field(x => x.IsCurrentAssignment);
        descriptor.Field(x => x.WorkloadRoutes)
            .Type<NonNullType<ListType<NonNullType<DriverWorkloadRouteType>>>>();
    }
}

public sealed class DriverWorkloadRouteType : ObjectType<DriverWorkloadRouteDto>
{
    protected override void Configure(IObjectTypeDescriptor<DriverWorkloadRouteDto> descriptor)
    {
        descriptor.Name("DriverWorkloadRoute");
        descriptor.Field(x => x.RouteId);
        descriptor.Field(x => x.VehicleId);
        descriptor.Field(x => x.VehiclePlate);
        descriptor.Field(x => x.StartDate);
        descriptor.Field(x => x.Status);
    }
}
