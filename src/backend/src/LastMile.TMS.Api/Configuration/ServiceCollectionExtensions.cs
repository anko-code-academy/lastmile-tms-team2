using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.PostgreSql;
using LastMile.TMS.Api.Diagnostics;
using LastMile.TMS.Api.GraphQL.BinLocations;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Api.GraphQL.Depots;
using LastMile.TMS.Api.GraphQL.Drivers;
using LastMile.TMS.Api.GraphQL.Parcels;
using LastMile.TMS.Api.GraphQL.Routes;
using LastMile.TMS.Api.GraphQL.Users;
using LastMile.TMS.Api.GraphQL.Vehicles;
using LastMile.TMS.Api.GraphQL.Zones;
using LastMile.TMS.Api.Hubs;
using LastMile.TMS.Application.Parcels.Services;
using Microsoft.AspNetCore.Authentication;

namespace LastMile.TMS.Api.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Use OpenIddict JWT validation instead of Identity cookies. Call after <c>AddPersistence</c> and <c>AddInfrastructure</c>.
    /// </summary>
    public static IServiceCollection AddOpenIddictJwtAuthenticationDefaults(this IServiceCollection services)
    {
        services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme =
                OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme =
                OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });
        return services;
    }

    public static IServiceCollection AddLastMileApi(this IServiceCollection services, IConfiguration configuration)
    {
        var disableExternalInfrastructure = configuration.GetValue("Testing:DisableExternalInfrastructure", false);

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        services.AddSignalR();
        services.AddScoped<IParcelUpdateNotifier, SignalRParcelUpdateNotifier>();
        services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .ModifyCostOptions(o => o.MaxFieldCost = 1000000)
            .AddTypeExtension<DepotQueries>()
            .AddTypeExtension<BinLocationQueries>()
            .AddTypeExtension<DriverQueries>()
            .AddTypeExtension<ParcelQueries>()
            .AddTypeExtension<RouteQueries>()
            .AddTypeExtension<UserManagementQueries>()
            .AddTypeExtension<VehicleQueries>()
            .AddTypeExtension<ZoneQueries>()
            .AddTypeExtension<DepotMutations>()
            .AddTypeExtension<BinLocationMutations>()
            .AddTypeExtension<DriverMutations>()
            .AddTypeExtension<ParcelMutations>()
            .AddTypeExtension<RouteMutations>()
            .AddTypeExtension<UserManagementMutations>()
            .AddTypeExtension<VehicleMutations>()
            .AddTypeExtension<ZoneMutations>()
            .AddType<VehicleType>()
            .AddType<VehicleFilterInputType>()
            .AddType<VehicleSortInputType>()
            .AddType<UserRoleType>()
            .AddType<UserManagementUserType>()
            .AddType<UserManagementUserFilterInputType>()
            .AddType<UserManagementUserSortInputType>()
            .AddType<DriverType>()
            .AddType<AddressType>()
            .AddType<GeoLocationType>()
            .AddType<OperatingHoursType>()
            .AddType<DepotType>()
            .AddType<DepotStorageLayoutType>()
            .AddType<StorageZoneType>()
            .AddType<StorageAisleType>()
            .AddType<BinLocationType>()
            .AddType<AddressFilterInputType>()
            .AddType<AddressSortInputType>()
            .AddType<OperatingHoursFilterInputType>()
            .AddType<OperatingHoursListFilterInputType>()
            .AddType<DepotFilterInputType>()
            .AddType<DepotSortInputType>()
            .AddType<ZoneType>()
            .AddType<ZoneFilterInputType>()
            .AddType<ZoneSortInputType>()
            .AddType<RouteType>()
            .AddType<RouteAssignmentAuditEntryType>()
            .AddType<RouteAssignmentCandidatesType>()
            .AddType<AssignableVehicleType>()
            .AddType<AssignableDriverType>()
            .AddType<DriverWorkloadRouteType>()
            .AddType<RouteFilterInputType>()
            .AddType<RouteSortInputType>()
            .AddType<ParcelListType>()
            .AddType<ParcelFilterInputType>()
            .AddType<ParcelSortInputType>()
            .AddType<ParcelDetailType>()
            .AddType<ParcelDetailAddressType>()
            .AddType<ParcelChangeHistoryType>()
            .AddType<ParcelRouteAssignmentType>()
            .AddType<ParcelProofOfDeliveryType>()
            .AddType<ParcelImportHistoryType>()
            .AddType<ParcelImportDetailType>()
            .AddType<ParcelImportRowFailurePreviewType>()
            .AddType<ParcelRouteOptionType>()
            .AddType<SortTargetBinType>()
            .AddType<ParcelSortInstructionType>()
            .AddType<InboundManifestType>()
            .AddType<InboundReceivingSessionType>()
            .AddType<InboundExpectedParcelType>()
            .AddType<InboundScannedParcelType>()
            .AddType<InboundReceivingExceptionType>()
            .AddType<InboundParcelScanResultType>()
            .AddType<StagingRouteType>()
            .AddType<RouteStagingExpectedParcelType>()
            .AddType<RouteStagingBoardType>()
            .AddType<StageParcelForRouteResultType>()
            .AddType<LoadOutRouteType>()
            .AddType<RouteLoadOutExpectedParcelType>()
            .AddType<RouteLoadOutBoardType>()
            .AddType<LoadParcelForRouteResultType>()
            .AddType<CompleteLoadOutResultType>()
            .AddType<DepotParcelInventoryDashboardType>()
            .AddType<DepotParcelInventoryStatusCountType>()
            .AddType<DepotParcelInventoryZoneCountType>()
            .AddType<DepotParcelAgingAlertType>()
            .AddType<DepotParcelInventoryPageInfoType>()
            .AddType<DepotParcelInventoryParcelType>()
            .AddType<DepotParcelInventoryParcelConnectionType>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddAuthorization()
            .AddErrorFilter<DomainExceptionErrorFilter>()
            .AddErrorFilter<GraphQLErrorFilter>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                var allowedOrigins = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost", "http://localhost:3000"];

                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        if (disableExternalInfrastructure)
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = configuration.GetConnectionString("Redis"));

            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(options =>
                    options.UseNpgsqlConnection(configuration.GetConnectionString("HangfireConnection"))));
            services.AddHangfireServer();
        }

        return services;
    }
}
