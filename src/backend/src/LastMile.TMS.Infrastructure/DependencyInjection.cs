using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Zones.Services;
using LastMile.TMS.Infrastructure.Options;
using LastMile.TMS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using QuestPDF.Infrastructure;

namespace LastMile.TMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var disableExternalInfrastructure = configuration.GetValue("Testing:DisableExternalInfrastructure", false);
        var isInMemoryDatabase = string.Equals(
            configuration.GetConnectionString("DefaultConnection"),
            "InMemory",
            StringComparison.OrdinalIgnoreCase);
        var enableTestSupport = configuration.GetValue("Testing:EnableTestSupport", false);

        QuestPDF.Settings.License = LicenseType.Community;

        services.AddHttpContextAccessor();
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection("Storage"))
            .Validate(
                options => disableExternalInfrastructure
                    || (!string.IsNullOrWhiteSpace(options.AccessKey)
                        && !string.IsNullOrWhiteSpace(options.SecretKey)),
                "Storage access key and secret key are required when external infrastructure is enabled.")
            .ValidateOnStart();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDriverPhotoFileCleanup, DriverPhotoFileCleanup>();
        services.AddScoped<DriverPhotoOrphanCleanupJob>();
        services.AddScoped<StorageBackfillRunner>();
        services.AddScoped<FrontendBaseUrlResolver>();
        services.AddScoped<IZoneBoundaryParser, ZoneBoundaryParser>();
        services.AddSingleton<IZplLabelRasterizer, ZplLabelRasterizer>();
        services.AddScoped<IParcelLabelGenerator, ParcelLabelGenerator>();
        services.AddScoped<IParcelImportFileParser, ParcelImportFileParser>();
        services.AddScoped<IParcelImportTemplateGenerator, ParcelImportTemplateGenerator>();
        services.AddScoped<ParcelImportBackgroundJob>();

        if (disableExternalInfrastructure)
        {
            services.AddSingleton<InMemoryFileStorageService>();
            services.AddSingleton<IFileStorageService>(provider => provider.GetRequiredService<InMemoryFileStorageService>());
        }
        else
        {
            services.AddSingleton<IFileStorageService, S3CompatibleFileStorageService>();
        }

        // Parcel registration geocoding and zone matching
        if (enableTestSupport)
        {
            services.AddScoped<IGeocodingService, TestSupportGeocodingService>();
        }
        else if (isInMemoryDatabase)
        {
            services.AddScoped<IGeocodingService, DeterministicGeocodingService>();
        }
        else
        {
            services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();
        }

        services.AddScoped<IZoneMatchingService, ZoneMatchingService>();

        if (disableExternalInfrastructure)
        {
            services.AddScoped<IUserAccountEmailJobScheduler, NoOpUserAccountEmailJobScheduler>();
            services.AddScoped<IParcelImportJobScheduler, ImmediateParcelImportJobScheduler>();
        }
        else
        {
            services.AddScoped<IUserAccountEmailJobScheduler, HangfireUserAccountEmailJobScheduler>();
            services.AddScoped<IParcelImportJobScheduler, HangfireParcelImportJobScheduler>();
        }

        services.AddScoped<IUserAccountEmailService, UserAccountEmailService>();
        services.AddScoped<UserAccountEmailBackgroundJob>();
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.Configure<FrontendOptions>(configuration.GetSection("Frontend"));
        services.Configure<TestingOptions>(configuration.GetSection("Testing"));

        var accessTokenMinutes = configuration.GetValue("Authentication:AccessTokenLifetimeMinutes", 60);
        var refreshTokenDays = configuration.GetValue("Authentication:RefreshTokenLifetimeDays", 14);
        var issuer = configuration.GetValue("Authentication:Issuer", "http://localhost");

        // Configure OpenIddict server (password + refresh token grant -> /connect/token)
        services.AddOpenIddict()
            .AddServer(options =>
            {
                // Set the issuer to the public URL
                options.SetIssuer(new Uri(issuer));

                // Enable password and refresh-token grant types
                options.AllowPasswordFlow()
                       .AllowRefreshTokenFlow();

                // Token endpoint
                options.SetTokenEndpointUris("/connect/token");

                // Accept anonymous clients (no client_id required for password flow)
                options.AcceptAnonymousClients();

                // Register scopes
                options.RegisterScopes(OpenIddictConstants.Scopes.OfflineAccess);

                // Token lifetimes
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(accessTokenMinutes));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(refreshTokenDays));

                // Use ASP.NET Core integration
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .DisableTransportSecurityRequirement();

                // Development certificates (persisted on disk, reused between restarts)
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate()
                       .DisableAccessTokenEncryption();
            })
            .AddValidation(options =>
            {
                // Validate tokens issued by the local OpenIddict server
                options.UseLocalServer();
                options.EnableAuthorizationEntryValidation();
                options.EnableTokenEntryValidation();
                options.UseAspNetCore();
            });

        return services;
    }
}
