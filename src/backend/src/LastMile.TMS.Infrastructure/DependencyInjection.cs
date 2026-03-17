using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace LastMile.TMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        var accessTokenMinutes = configuration.GetValue("Authentication:AccessTokenLifetimeMinutes", 60);
        var refreshTokenDays = configuration.GetValue("Authentication:RefreshTokenLifetimeDays", 14);

        // Configure OpenIddict server (password + refresh token grant → /connect/token)
        services.AddOpenIddict()
            .AddServer(options =>
            {
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

                // Ephemeral signing/encryption keys (dev-only; swap for real certs in prod)
                options.AddEphemeralEncryptionKey()
                       .AddEphemeralSigningKey()
                       .DisableAccessTokenEncryption(); // plain JWT (not encrypted JWE)
            })
            .AddValidation(options =>
            {
                // Validate tokens issued by the local OpenIddict server
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
