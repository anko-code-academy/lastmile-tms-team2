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
/// The database schema is migrated once, then each reset truncates all user
/// tables and reseeds baseline data to keep integration tests isolated while
/// still exercising the real relational provider.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string DefaultTestConnection =
        "Host=localhost;Port=5432;Database=lastmile_tms_test;Username=postgres;Password=postgres";

    private static string TestConnection =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") ?? DefaultTestConnection;

    private readonly SemaphoreSlim _resetLock = new(1, 1);
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private IReadOnlyList<string>? _resetTableNames;
    private bool _databaseInitialized;

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
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("Serilog:MinimumLevel:Override:Microsoft", "Warning");
        builder.UseSetting("Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore", "Warning");
        builder.UseSetting("Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command", "Warning");

        builder.ConfigureTestServices(services =>
        {
            // Remove all hosted services to prevent TaskCanceledException during teardown.
            // StackExchangeRedisCache registers a RedisCacheService and Hangfire registers
            // BackgroundJobServerHostedService; both hang on shutdown when Redis/Hangfire
            // backends are unreachable in the test environment.
            // Keep DbSeeder as a singleton and invoke it manually from ResetDatabaseAsync
            // so the database is seeded exactly once per reset instead of on every host start.
            var hostedServices = services.Where(d => d.ServiceType.IsAssignableTo(typeof(IHostedService))).ToList();
            foreach (var svc in hostedServices)
            {
                services.Remove(svc);
            }

            services.AddSingleton<DbSeeder>();

            services.AddSingleton(SqlCapture);

            // Replace the real Nominatim geocoding service with a deterministic in-memory stub
            // that always returns a point inside the seeded PostGIS zone polygon.
            // This exercises the full PostGIS ST_Covers zone-matching path without any
            // external HTTP calls or network dependency.
            services.RemoveAll<LastMile.TMS.Application.Parcels.Services.IGeocodingService>();
            services.AddScoped<LastMile.TMS.Application.Parcels.Services.IGeocodingService, TestGeocodingService>();

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
        var host = base.CreateHost(builder);
        EnsureDatabaseInitializedAsync(host.Services).GetAwaiter().GetResult();
        return host;
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _resetLock.WaitAsync(cancellationToken);
        try
        {
            _ = Services;
            await EnsureDatabaseInitializedAsync(Services, cancellationToken);
            EmailService.Clear();
            SqlCapture.Clear();

            await TruncateUserTablesAsync(cancellationToken);

            await using (var scope = Services.CreateAsyncScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
                await seeder.SeedAsync(runMigrations: false, cancellationToken);
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

    private async Task EnsureDatabaseInitializedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        if (_databaseInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_databaseInitialized)
            {
                return;
            }

            await using var scope = services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);

            _resetTableNames = await LoadResetTableNamesAsync(cancellationToken);
            _databaseInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task<IReadOnlyList<string>> LoadResetTableNamesAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(TestConnection);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT tablename
            FROM pg_tables
            WHERE schemaname = 'public'
              AND tablename NOT IN ('__EFMigrationsHistory', 'spatial_ref_sys')
            ORDER BY tablename
            """,
            connection);

        var tableNames = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }

    private async Task TruncateUserTablesAsync(CancellationToken cancellationToken)
    {
        if (_resetTableNames is null || _resetTableNames.Count == 0)
        {
            return;
        }

        var tables = string.Join(
            ", ",
            _resetTableNames.Select(tableName => $"public.\"{tableName.Replace("\"", "\"\"")}\""));

        await using var connection = new NpgsqlConnection(TestConnection);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            $"TRUNCATE TABLE {tables} RESTART IDENTITY CASCADE;",
            connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
