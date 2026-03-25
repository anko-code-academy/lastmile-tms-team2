using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace LastMile.TMS.Api.Tests;

/// <summary>
/// WebApplicationFactory that switches the DB to in-memory by overriding the
/// DefaultConnection string to "InMemory". AddPersistence in Persistence/DependencyInjection.cs
/// detects this value and calls UseInMemoryDatabase instead of UseNpgsql.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", "InMemory");

        builder.ConfigureTestServices(services =>
        {
            // Remove all hosted services to prevent TaskCanceledException during teardown.
            // StackExchangeRedisCache registers a RedisCacheService and Hangfire registers
            // BackgroundJobServerHostedService — both hang on shutdown when Redis/Hangfire
            // backends are unreachable in the test environment.
            // Only DbSeeder is needed (for seeded test data), so re-add it.
            var hostedServices = services.Where(d => d.ServiceType.IsAssignableTo(typeof(Microsoft.Extensions.Hosting.IHostedService))).ToList();
            foreach (var svc in hostedServices)
                services.Remove(svc);

            services.AddHostedService<DbSeeder>();
        });
    }
}
