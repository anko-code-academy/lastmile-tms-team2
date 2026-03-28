using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.PostgreSql;
using LastMile.TMS.Api.Diagnostics;
using LastMile.TMS.Api.GraphQL.Common;
using LastMile.TMS.Api.GraphQL.Depots;
using LastMile.TMS.Api.GraphQL.Drivers;
using LastMile.TMS.Api.GraphQL.Parcels;
using LastMile.TMS.Api.GraphQL.Routes;
using LastMile.TMS.Api.GraphQL.Users;
using LastMile.TMS.Api.GraphQL.Vehicles;
using LastMile.TMS.Api.GraphQL.Zones;
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
        services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .ModifyCostOptions(o => o.MaxFieldCost = 1000000)
            .AddTypeExtension<DepotQuery>()
            .AddTypeExtension<DriverQuery>()
            .AddTypeExtension<ParcelQuery>()
            .AddTypeExtension<RouteQuery>()
            .AddTypeExtension<UserManagementQuery>()
            .AddTypeExtension<VehicleQuery>()
            .AddTypeExtension<ZoneQuery>()
            .AddTypeExtension<DepotMutation>()
            .AddTypeExtension<ParcelMutation>()
            .AddTypeExtension<RouteMutation>()
            .AddTypeExtension<UserManagementMutation>()
            .AddTypeExtension<VehicleMutation>()
            .AddTypeExtension<ZoneMutation>()
            .AddType<VehicleDtoType>()
            .AddType<UserRoleType>()
            .AddType<UserManagementUserDtoType>()
            .AddType<AddressType>()
            .AddType<OperatingHoursType>()
            .AddType<DepotType>()
            .AddType<AddressDtoFilterInputType>()
            .AddType<AddressDtoSortInputType>()
            .AddType<OperatingHoursDtoFilterInputType>()
            .AddType<ListFilterInputTypeOfOperatingHoursDtoFilterInputType>()
            .AddType<DepotDtoFilterInputType>()
            .AddType<DepotDtoSortInputType>()
            .AddType<ZoneType>()
            .AddType<ParcelType>()
            .AddType<AddressInputType>()
            .AddType<OperatingHoursInputType>()
            .AddType<CreateDepotInputType>()
            .AddType<UpdateDepotInputType>()
            .AddType<CreateZoneInputType>()
            .AddType<UpdateZoneInputType>()
            .AddType<RegisterParcelInputType>()
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
