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
        descriptor.Field(r => r.ZoneId).IsProjected(true);
        descriptor.Field("zoneName")
            .Type<StringType>()
            .Resolve(async ctx =>
                (await LoadRouteZoneLabelAsync(ctx, ctx.Parent<RouteEntity>().Id)).ZoneName);
        descriptor.Field("depotId")
            .Type<NonNullType<UuidType>>()
            .Resolve(async ctx =>
                (await LoadRouteDepotSnapshotAsync(ctx, ctx.Parent<RouteEntity>().Id)).DepotId);
        descriptor.Field("depotName")
            .Type<StringType>()
            .Resolve(async ctx =>
                (await LoadRouteDepotSnapshotAsync(ctx, ctx.Parent<RouteEntity>().Id)).DepotName);
        descriptor.Field("depotAddressLine")
            .Type<StringType>()
            .Resolve(async ctx =>
                (await LoadRouteDepotSnapshotAsync(ctx, ctx.Parent<RouteEntity>().Id)).AddressLine);
        descriptor.Field("depotLongitude")
            .Type<FloatType>()
            .Resolve(async ctx =>
                (await LoadRouteDepotSnapshotAsync(ctx, ctx.Parent<RouteEntity>().Id)).Longitude);
        descriptor.Field("depotLatitude")
            .Type<FloatType>()
            .Resolve(async ctx =>
                (await LoadRouteDepotSnapshotAsync(ctx, ctx.Parent<RouteEntity>().Id)).Latitude);
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
        descriptor.Field(r => r.DispatchedAt);
        descriptor.Field(r => r.EndDate);
        descriptor.Field(r => r.StartMileage);
        descriptor.Field(r => r.EndMileage);
        descriptor.Field(r => r.StagingArea);
        descriptor.Field(r => r.CancellationReason);
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
        descriptor.Field(r => r.PlannedDistanceMeters);
        descriptor.Field(r => r.PlannedDurationSeconds);
        descriptor.Field("estimatedStopCount")
            .Type<NonNullType<IntType>>()
            .Resolve(async ctx => await LoadRouteStopCountAsync(ctx));
        descriptor.Field("path")
            .Type<NonNullType<ListType<NonNullType<RoutePathPointType>>>>()
            .Resolve(async ctx => await LoadRoutePathAsync(ctx));
        descriptor.Field("stops")
            .Type<NonNullType<ListType<NonNullType<RouteStopType>>>>()
            .Resolve(async ctx => await LoadRouteStopsAsync(ctx));
        descriptor.Field(r => r.CreatedAt);
        descriptor.Field(r => r.LastModifiedAt).Name("updatedAt");
        descriptor.Field(r => r.AssignmentAuditTrail)
            .Type<NonNullType<ListType<NonNullType<RouteAssignmentAuditEntryType>>>>()
            .Resolve(async ctx => await LoadAssignmentAuditTrailAsync(ctx));
        descriptor.Field("parcelAdjustmentAuditTrail")
            .Type<NonNullType<ListType<NonNullType<RouteParcelAdjustmentAuditEntryType>>>>()
            .Resolve(async ctx => await LoadParcelAdjustmentAuditTrailAsync(ctx));
        descriptor.Field("latestParcelAdjustment")
            .Type<RouteParcelAdjustmentAuditEntryType>()
            .Resolve(async ctx => (await LoadParcelAdjustmentAuditTrailAsync(ctx)).FirstOrDefault());
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

    private static async Task<RouteZoneLabel> LoadRouteZoneLabelAsync(IResolverContext ctx, Guid routeId)
    {
        var label = await ctx.BatchDataLoader<Guid, RouteZoneLabel>(
                async (routeIds, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var rows = await dbContext.Routes
                        .AsNoTracking()
                        .Where(route => routeIds.Contains(route.Id))
                        .Select(route => new
                        {
                            route.Id,
                            ZoneName = route.Zone.Name,
                        })
                        .ToListAsync(ct);

                    return routeIds.ToDictionary(
                        id => id,
                        id =>
                        {
                            var row = rows.FirstOrDefault(candidate => candidate.Id == id);
                            return row is null
                                ? RouteZoneLabel.Empty
                                : new RouteZoneLabel(row.ZoneName);
                        });
                },
                "RouteZoneLabelByRouteId")
            .LoadAsync(routeId);

        return label ?? RouteZoneLabel.Empty;
    }

    private sealed record RouteZoneLabel(string ZoneName)
    {
        public static RouteZoneLabel Empty { get; } = new(string.Empty);
    }

    private static async Task<RouteDepotSnapshot> LoadRouteDepotSnapshotAsync(IResolverContext ctx, Guid routeId)
    {
        var depot = await ctx.BatchDataLoader<Guid, RouteDepotSnapshot>(
                async (routeIds, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var rows = await dbContext.Routes
                        .AsNoTracking()
                        .Where(route => routeIds.Contains(route.Id))
                        .Select(route => new
                        {
                            route.Id,
                            route.Zone.DepotId,
                            DepotName = route.Zone.Depot.Name,
                            route.Zone.Depot.Address.Street1,
                            route.Zone.Depot.Address.Street2,
                            route.Zone.Depot.Address.City,
                            route.Zone.Depot.Address.State,
                            route.Zone.Depot.Address.PostalCode,
                            DepotPoint = route.Zone.Depot.Address.GeoLocation,
                        })
                        .ToListAsync(ct);

                    return routeIds.ToDictionary(
                        id => id,
                        id =>
                        {
                            var row = rows.FirstOrDefault(candidate => candidate.Id == id);
                            return row is null
                                ? RouteDepotSnapshot.Empty
                                : new RouteDepotSnapshot(
                                    row.DepotId,
                                    row.DepotName,
                                    BuildAddressLine(
                                        row.Street1,
                                        row.Street2,
                                        row.City,
                                        row.State,
                                        row.PostalCode),
                                    row.DepotPoint?.X,
                                    row.DepotPoint?.Y);
                        });
                },
                "RouteDepotSnapshotByRouteId")
            .LoadAsync(routeId);

        return depot ?? RouteDepotSnapshot.Empty;
    }

    private sealed record RouteDepotSnapshot(
        Guid DepotId,
        string DepotName,
        string AddressLine,
        double? Longitude,
        double? Latitude)
    {
        public static RouteDepotSnapshot Empty { get; } = new(Guid.Empty, string.Empty, string.Empty, null, null);
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

    private static Task<int> LoadRouteStopCountAsync(IResolverContext ctx)
    {
        var routeId = ctx.Parent<RouteEntity>().Id;

        return ctx.BatchDataLoader<Guid, int>(
                async (ids, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var counts = await dbContext.RouteStops
                        .AsNoTracking()
                        .Where(stop => ids.Contains(stop.RouteId))
                        .GroupBy(stop => stop.RouteId)
                        .Select(group => new { RouteId = group.Key, Count = group.Count() })
                        .ToListAsync(ct);

                    return ids.ToDictionary(
                        id => id,
                        id => counts.FirstOrDefault(candidate => candidate.RouteId == id)?.Count ?? 0);
                },
                "RouteStopCountByRouteId")
            .LoadAsync(routeId);
    }

    private static async Task<List<RouteStopDto>> LoadRouteStopsAsync(IResolverContext ctx)
    {
        var routeId = ctx.Parent<RouteEntity>().Id;
        var stops = await ctx.BatchDataLoader<Guid, List<RouteStopDto>>(
                async (ids, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var rows = await dbContext.RouteStops
                        .AsNoTracking()
                        .Where(stop => ids.Contains(stop.RouteId))
                        .Include(stop => stop.Parcels)
                        .ThenInclude(parcel => parcel.RecipientAddress)
                        .OrderBy(stop => stop.Sequence)
                        .ToListAsync(ct);

                    return ids.ToDictionary(
                        id => id,
                        id => rows
                            .Where(stop => stop.RouteId == id)
                            .OrderBy(stop => stop.Sequence)
                            .Select(stop => new RouteStopDto
                            {
                                Id = stop.Id.ToString(),
                                Sequence = stop.Sequence,
                                RecipientLabel = stop.RecipientLabel,
                                AddressLine = BuildAddressLine(stop),
                                Longitude = stop.StopLocation.X,
                                Latitude = stop.StopLocation.Y,
                                Parcels = stop.Parcels
                                    .OrderBy(parcel => parcel.TrackingNumber)
                                    .Select(parcel => new RouteStopParcelDto
                                    {
                                        ParcelId = parcel.Id,
                                        TrackingNumber = parcel.TrackingNumber,
                                        RecipientLabel = string.IsNullOrWhiteSpace(parcel.RecipientAddress.ContactName)
                                            ? parcel.RecipientAddress.CompanyName ?? parcel.TrackingNumber
                                            : parcel.RecipientAddress.ContactName,
                                        AddressLine = BuildAddressLine(parcel.RecipientAddress),
                                        Status = parcel.Status,
                                    })
                                    .ToList(),
                            })
                            .ToList());
                },
                "RouteStopsByRouteId")
            .LoadAsync(routeId);

        return stops ?? [];
    }

    private static async Task<List<RoutePathPointDto>> LoadRoutePathAsync(IResolverContext ctx)
    {
        var routeId = ctx.Parent<RouteEntity>().Id;
        var path = await ctx.BatchDataLoader<Guid, List<RoutePathPointDto>>(
                async (ids, ct) =>
                {
                    var dbContext = ctx.Service<IAppDbContext>();
                    var rows = await dbContext.Routes
                        .AsNoTracking()
                        .Where(route => ids.Contains(route.Id))
                        .Select(route => new
                        {
                            route.Id,
                            route.PlannedPath,
                        })
                        .ToListAsync(ct);

                    return ids.ToDictionary(
                        id => id,
                        id =>
                        {
                            var row = rows.FirstOrDefault(candidate => candidate.Id == id);
                            return row?.PlannedPath?.Coordinates
                                .Select(coordinate => new RoutePathPointDto
                                {
                                    Longitude = coordinate.X,
                                    Latitude = coordinate.Y,
                                })
                                .ToList()
                                ?? [];
                        });
                },
                "RoutePathByRouteId")
            .LoadAsync(routeId);

        return path ?? [];
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

    private static Task<List<RouteParcelAdjustmentAuditEntry>> LoadParcelAdjustmentAuditTrailAsync(
        IResolverContext ctx)
    {
        var routeId = ctx.Parent<RouteEntity>().Id;

        return LoadAsync();

        async Task<List<RouteParcelAdjustmentAuditEntry>> LoadAsync()
        {
            var rows = await ctx.BatchDataLoader<Guid, List<RouteParcelAdjustmentAuditEntry>>(
                    async (ids, ct) =>
                    {
                        var dbContext = ctx.Service<IAppDbContext>();
                        var auditRows = await dbContext.RouteParcelAdjustmentAuditEntries
                            .AsNoTracking()
                            .Where(entry => ids.Contains(entry.RouteId))
                            .OrderByDescending(entry => entry.ChangedAt)
                            .ToListAsync(ct);

                        return ids.ToDictionary(
                            id => id,
                            id => auditRows.Where(entry => entry.RouteId == id).ToList());
                    },
                    "RouteParcelAdjustmentAuditTrailByRouteId")
                .LoadAsync(routeId);

            return rows ?? [];
        }
    }

    private static string BuildAddressLine(RouteStop stop) => BuildAddressLine(
        stop.Street1,
        stop.Street2,
        stop.City,
        stop.State,
        stop.PostalCode);

    private static string BuildAddressLine(Address address) => BuildAddressLine(
        address.Street1,
        address.Street2,
        address.City,
        address.State,
        address.PostalCode);

    private static string BuildAddressLine(
        string street1,
        string? street2,
        string city,
        string state,
        string postalCode)
    {
        var parts = new[] { street1, street2, city, state, postalCode }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(", ", parts);
    }
}

public sealed class RouteFilterInputType : FilterInputType<RouteEntity>
{
    protected override void Configure(IFilterInputTypeDescriptor<RouteEntity> descriptor)
    {
        descriptor.Name("RouteFilterInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(r => r.Id);
        descriptor.Field(r => r.ZoneId);
        descriptor.Field(r => r.VehicleId);
        descriptor.Field(r => r.DriverId);
        descriptor.Field(r => r.StartDate);
        descriptor.Field(r => r.DispatchedAt);
        descriptor.Field(r => r.EndDate);
        descriptor.Field(r => r.StartMileage);
        descriptor.Field(r => r.EndMileage);
        descriptor.Field(r => r.PlannedDistanceMeters);
        descriptor.Field(r => r.PlannedDurationSeconds);
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
        descriptor.Field(r => r.ZoneId);
        descriptor.Field(r => r.VehicleId);
        descriptor.Field(r => r.DriverId);
        descriptor.Field(r => r.StartDate);
        descriptor.Field(r => r.DispatchedAt);
        descriptor.Field(r => r.EndDate);
        descriptor.Field(r => r.StartMileage);
        descriptor.Field(r => r.EndMileage);
        descriptor.Field(r => r.PlannedDistanceMeters);
        descriptor.Field(r => r.PlannedDurationSeconds);
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

public sealed class RouteParcelAdjustmentAuditEntryType : ObjectType<RouteParcelAdjustmentAuditEntry>
{
    protected override void Configure(IObjectTypeDescriptor<RouteParcelAdjustmentAuditEntry> descriptor)
    {
        descriptor.Name("RouteParcelAdjustmentAuditEntry");
        descriptor.Field(x => x.Id);
        descriptor.Field(x => x.Action);
        descriptor.Field(x => x.ParcelId);
        descriptor.Field(x => x.TrackingNumber);
        descriptor.Field(x => x.Reason);
        descriptor.Field(x => x.AffectedStopSequence);
        descriptor.Field(x => x.ChangedAt);
        descriptor.Field(x => x.ChangedBy);
    }
}

public sealed class RouteParcelAdjustmentCandidateType : ObjectType<RouteParcelAdjustmentCandidateDto>
{
    protected override void Configure(IObjectTypeDescriptor<RouteParcelAdjustmentCandidateDto> descriptor)
    {
        descriptor.Name("RouteParcelAdjustmentCandidate");
        descriptor.BindFieldsImplicitly();
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

public sealed class RoutePlanPreviewType : ObjectType<RoutePlanPreviewDto>
{
    protected override void Configure(IObjectTypeDescriptor<RoutePlanPreviewDto> descriptor)
    {
        descriptor.Name("RoutePlanPreview");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class RoutePlanParcelCandidateType : ObjectType<RoutePlanParcelCandidateDto>
{
    protected override void Configure(IObjectTypeDescriptor<RoutePlanParcelCandidateDto> descriptor)
    {
        descriptor.Name("RoutePlanParcelCandidate");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class RouteStopType : ObjectType<RouteStopDto>
{
    protected override void Configure(IObjectTypeDescriptor<RouteStopDto> descriptor)
    {
        descriptor.Name("RouteStop");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class RouteStopParcelType : ObjectType<RouteStopParcelDto>
{
    protected override void Configure(IObjectTypeDescriptor<RouteStopParcelDto> descriptor)
    {
        descriptor.Name("RouteStopParcel");
        descriptor.BindFieldsImplicitly();
    }
}

public sealed class RoutePathPointType : ObjectType<RoutePathPointDto>
{
    protected override void Configure(IObjectTypeDescriptor<RoutePathPointDto> descriptor)
    {
        descriptor.Name("RoutePathPoint");
        descriptor.BindFieldsImplicitly();
    }
}
