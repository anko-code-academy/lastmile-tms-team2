using Hangfire;
using Hangfire.PostgreSql;
using LastMile.TMS.Api.GraphQL;
using LastMile.TMS.Api.GraphQL.Inputs;
using LastMile.TMS.Api.GraphQL.Mutations;
using LastMile.TMS.Api.GraphQL.Queries;
using LastMile.TMS.Api.GraphQL.Types;
using Microsoft.AspNetCore.Authentication;

namespace LastMile.TMS.Api;

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

        services.AddControllers();
        services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddType<VehicleDtoType>()
            .AddType<UserRoleType>()
            .AddTypeExtension<DepotQuery>()
            .AddTypeExtension<ZoneQuery>()
            .AddTypeExtension<UserManagementQuery>()
            .AddTypeExtension<DepotMutation>()
            .AddTypeExtension<ZoneMutation>()
            .AddTypeExtension<ParcelMutation>()
            .AddTypeExtension<UserManagementMutation>()
            .AddType<AddressType>()
            .AddType<OperatingHoursType>()
            .AddType<DepotType>()
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
            .AddAuthorization()
            .AddErrorFilter<DomainExceptionErrorFilter>()
            .AddErrorFilter<GraphQLErrorFilter>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:3000" };

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
