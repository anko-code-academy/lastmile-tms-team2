using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace LastMile.TMS.Api.Tests;

/// <summary>
/// WebApplicationFactory backed by a dedicated PostgreSQL test database.
/// The database is recreated and reseeded between tests to keep integration
/// tests isolated while still exercising the real relational provider.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string DefaultTestConnection =
        "Host=localhost;Port=5432;Database=lastmile_tms_test;Username=postgres;Password=postgres";

    private static string TestConnection =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") ?? DefaultTestConnection;

    private readonly SemaphoreSlim _resetLock = new(1, 1);

    public TestUserAccountEmailService EmailService { get; } = new();
    public SqlCommandCaptureInterceptor SqlCapture { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnection);
        builder.UseSetting("ConnectionStrings:HangfireConnection", TestConnection);
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

            services.AddSingleton<DbSeeder>();
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<DbSeeder>());

            services.AddSingleton(SqlCapture);

            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IAppDbContext>();
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(
                    TestConnection,
                    npgsql =>
                    {
                        npgsql.UseNetTopologySuite();
                        npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    });
                options.UseOpenIddict();
                options.EnableSensitiveDataLogging();
                options.AddInterceptors(serviceProvider.GetRequiredService<SqlCommandCaptureInterceptor>());
            });
            services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            services.RemoveAll<IUserAccountEmailService>();
            services.RemoveAll<IUserAccountEmailJobScheduler>();
            services.AddSingleton(EmailService);
            services.AddSingleton<IUserAccountEmailService>(EmailService);
            services.AddScoped<IUserAccountEmailJobScheduler, ImmediateUserAccountEmailJobScheduler>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        EnsureDatabaseExistsAsync(TestConnection).GetAwaiter().GetResult();
        return base.CreateHost(builder);
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _resetLock.WaitAsync(cancellationToken);
        try
        {
            _ = Services;
            EmailService.Clear();
            SqlCapture.Clear();

            NpgsqlConnection.ClearAllPools();

            await using (var scope = Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.EnsureDeletedAsync(cancellationToken);
            }

            NpgsqlConnection.ClearAllPools();
            await EnsureDatabaseExistsAsync(TestConnection, cancellationToken);

            await using (var scope = Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync(cancellationToken);

                var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
                await seeder.SeedAsync(cancellationToken);
            }

            SqlCapture.Clear();
        }
        finally
        {
            _resetLock.Release();
        }
    }

    private static async Task EnsureDatabaseExistsAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The test connection string must include a database name.");
        }

        var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres",
            Pooling = false
        };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @databaseName",
            connection);
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);

        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;
        if (exists)
        {
            return;
        }

        var quotedDatabaseName = $"\"{databaseName.Replace("\"", "\"\"")}\"";
        await using var createCommand = new NpgsqlCommand(
            $"CREATE DATABASE {quotedDatabaseName}",
            connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
