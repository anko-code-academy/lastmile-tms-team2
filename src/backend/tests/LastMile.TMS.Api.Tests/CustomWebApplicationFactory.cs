using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LastMile.TMS.Api.Tests;

/// <summary>
/// WebApplicationFactory that switches the DB to in-memory by overriding the
/// DefaultConnection string to "InMemory". AddPersistence in Persistence/DependencyInjection.cs
/// detects this value and calls UseInMemoryDatabase instead of UseNpgsql.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public TestUserAccountEmailService EmailService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "InMemory");
        builder.UseSetting("Testing:DisableExternalInfrastructure", "true");
        builder.UseSetting("Testing:EnableTestSupport", "true");
        builder.UseSetting("Testing:SupportKey", "integration-test-support-key");

        builder.ConfigureTestServices(services =>
        {
            // Remove all hosted services to prevent TaskCanceledException during teardown.
            // StackExchangeRedisCache registers a RedisCacheService and Hangfire registers
            // BackgroundJobServerHostedService; both hang on shutdown when Redis/Hangfire
            // backends are unreachable in the test environment.
            // Re-add DbSeeder because the tests rely on seeded admin data.
            var hostedServices = services.Where(d => d.ServiceType.IsAssignableTo(typeof(IHostedService))).ToList();
            foreach (var svc in hostedServices)
            {
                services.Remove(svc);
            }

            services.AddHostedService<DbSeeder>();

            services.RemoveAll<IUserAccountEmailService>();
            services.RemoveAll<IUserAccountEmailJobScheduler>();
            services.AddSingleton(EmailService);
            services.AddSingleton<IUserAccountEmailService>(EmailService);
            services.AddScoped<IUserAccountEmailJobScheduler, ImmediateUserAccountEmailJobScheduler>();
        });
    }
}
