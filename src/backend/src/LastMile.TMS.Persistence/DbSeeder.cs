using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Persistence;

/// <summary>
/// Hosted service that seeds default roles, the admin user, and a test depot on first startup.
/// </summary>
public sealed class DbSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DbSeeder> logger,
    IConfiguration configuration) : IHostedService
{
    private const string DefaultAdminEmail = "admin@lastmile.com";
    private const string DefaultAdminPassword = "Admin@12345";
    private const string SeededTestDepotName = "Test Depot";
    private const string SeededTestZoneName = "Test Zone";

    /// <summary>Well-known depot ID used by integration tests and development.</summary>
    public static readonly Guid TestDepotId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>Address ID for the test depot.</summary>
    public static readonly Guid TestDepotAddressId = new("00000000-0000-0000-0000-000000000002");

    /// <summary>Zone ID for seeded parcels (PostGIS polygon; only seeded for real Postgres).</summary>
    public static readonly Guid TestZoneId = new("00000000-0000-0000-0000-000000000003");

    /// <summary>Shipper address for <see cref="TestParcelId"/>.</summary>
    public static readonly Guid TestParcelShipperAddressId = new("00000000-0000-0000-0000-000000000004");

    /// <summary>Recipient address for <see cref="TestParcelId"/>.</summary>
    public static readonly Guid TestParcelRecipientAddressId = new("00000000-0000-0000-0000-000000000005");

    /// <summary>First seeded parcel ID (see also <see cref="TestParcelSeedCount"/>).</summary>
    public static readonly Guid TestParcelId = new("00000000-0000-0000-0000-000000000006");

    /// <summary>Number of test parcels seeded for Postgres (shared shipper/recipient addresses).</summary>
    public const int TestParcelSeedCount = 9;

    /// <summary>Identity user ID for <see cref="TestDriverId"/>.</summary>
    public static readonly Guid TestDriverUserId = new("00000000-0000-0000-0000-000000000007");

    /// <summary>Well-known driver ID for development and manual testing.</summary>
    public static readonly Guid TestDriverId = new("00000000-0000-0000-0000-000000000008");

    /// <summary>Identity user ID for <see cref="TestDriver2Id"/>.</summary>
    public static readonly Guid TestDriver2UserId = new("00000000-0000-0000-0000-000000000018");

    /// <summary>Second seeded test driver (same depot/zone as <see cref="TestDriverId"/>).</summary>
    public static readonly Guid TestDriver2Id = new("00000000-0000-0000-0000-000000000019");

    /// <summary>Identity user ID for <see cref="TestDriver3Id"/>.</summary>
    public static readonly Guid TestDriver3UserId = new("00000000-0000-0000-0000-00000000001a");

    /// <summary>Third seeded test driver.</summary>
    public static readonly Guid TestDriver3Id = new("00000000-0000-0000-0000-00000000001b");

    /// <summary>Identity user ID for <see cref="TestDriver4Id"/>.</summary>
    public static readonly Guid TestDriver4UserId = new("00000000-0000-0000-0000-00000000001c");

    /// <summary>Fourth seeded test driver.</summary>
    public static readonly Guid TestDriver4Id = new("00000000-0000-0000-0000-00000000001d");

    /// <summary>Well-known vehicle ID for development and manual testing.</summary>
    public static readonly Guid TestVehicleId = new("00000000-0000-0000-0000-000000000009");

    private static readonly Guid TestVehicle2Id = new("00000000-0000-0000-0000-00000000001e");
    private static readonly Guid TestVehicle3Id = new("00000000-0000-0000-0000-00000000001f");
    private static readonly Guid TestVehicle4Id = new("00000000-0000-0000-0000-000000000020");

    private static readonly Guid DevelopmentDraftRouteId = new("00000000-0000-0000-0000-000000000301");
    private static readonly Guid DevelopmentDispatchedRouteId = new("00000000-0000-0000-0000-000000000302");
    private static readonly Guid DevelopmentInProgressRouteId = new("00000000-0000-0000-0000-000000000303");
    private static readonly Guid DevelopmentCompletedRouteId = new("00000000-0000-0000-0000-000000000304");

    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private sealed record AddressSeed(
        Guid Id,
        string Street1,
        string? Street2,
        string City,
        string State,
        string PostalCode,
        string CountryCode,
        bool IsResidential,
        string? ContactName,
        string? CompanyName,
        string? Phone,
        string? Email,
        double Longitude,
        double Latitude);

    private sealed record ParcelSeed(
        Guid Id,
        string TrackingNumber,
        decimal WeightKg,
        ParcelStatus Status,
        AddressSeed RecipientAddress,
        string Description,
        string? ParcelType = null,
        ServiceType ServiceType = ServiceType.Standard);

    private sealed record VehicleSeed(
        Guid Id,
        string RegistrationPlate,
        VehicleType Type,
        int ParcelCapacity,
        decimal WeightCapacity,
        VehicleStatus Status);

    private sealed record DemoRouteSeed(
        Guid Id,
        Guid VehicleId,
        Guid DriverId,
        RouteStatus Status,
        StagingArea StagingArea,
        DateTimeOffset StartDate,
        int StartMileage,
        int EndMileage,
        IReadOnlyList<Guid> ParcelIds);

    private static readonly AddressSeed TestDepotAddressSeed = new(
        TestDepotAddressId,
        "600 W Chicago Ave",
        null,
        "Chicago",
        "IL",
        "60654",
        "US",
        false,
        null,
        "Last Mile Chicago Depot",
        "+13125550000",
        "depot@lastmile.local",
        -87.6437911,
        41.8975826);

    private static readonly AddressSeed TestParcelShipperAddressSeed = new(
        TestParcelShipperAddressId,
        "500 W Madison St",
        null,
        "Chicago",
        "IL",
        "60661",
        "US",
        false,
        "Dispatch Desk",
        "Acme Fulfillment Chicago",
        "+13125550010",
        "dock@acme.local",
        -87.6405304,
        41.8838270);

    public Task StartAsync(CancellationToken cancellationToken) =>
        SeedAsync(cancellationToken);

    public Task SeedAsync(CancellationToken cancellationToken) =>
        SeedAsync(runMigrations: true, cancellationToken);

    public async Task SeedAsync(bool runMigrations, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var enableTestSupport = configuration.GetValue("Testing:EnableTestSupport", false);

        if (runMigrations && connectionString != "InMemory")
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager, cancellationToken);

        if (!await ShouldSeedDevelopmentDataAsync(
                dbContext,
                connectionString,
                enableTestSupport,
                cancellationToken))
        {
            return;
        }

        await SeedTestDepotAsync(dbContext, cancellationToken);

        // Zone + parcel use PostGIS geometry — skip for InMemory test databases.
        if (connectionString == "InMemory")
        {
            return;
        }

        await SeedTestZoneAsync(dbContext, cancellationToken);
        await SeedTestDriverAsync(userManager, dbContext, cancellationToken);
        await SeedTestVehicleAsync(dbContext, cancellationToken);
        await SeedTestParcelsAsync(dbContext, cancellationToken);

        if (!enableTestSupport)
        {
            await SeedDevelopmentRoutesAsync(dbContext, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<bool> ShouldSeedDevelopmentDataAsync(
        AppDbContext dbContext,
        string? connectionString,
        bool enableTestSupport,
        CancellationToken ct)
    {
        if (connectionString == "InMemory")
        {
            logger.LogInformation("Running development seed against the in-memory database.");
            return true;
        }

        if (!await HasAnyOperationalDataAsync(dbContext, ct))
        {
            logger.LogInformation("Database is empty; running initial development seed.");
            return true;
        }

        if (await HasCompletedDevelopmentSeedAsync(dbContext, enableTestSupport, ct))
        {
            logger.LogInformation("Development seed already completed; skipping startup seed.");
            return false;
        }

        if (await HasAnySeededDevelopmentDataAsync(dbContext, ct))
        {
            logger.LogInformation("Found partially seeded development records; resuming idempotent seed.");
            return true;
        }

        logger.LogInformation("Skipping development seed because the database already contains operational data.");
        return false;
    }

    private static async Task<bool> HasAnyOperationalDataAsync(AppDbContext dbContext, CancellationToken ct)
    {
        return await dbContext.Depots.AnyAsync(ct)
            || await dbContext.Zones.AnyAsync(ct)
            || await dbContext.Drivers.AnyAsync(ct)
            || await dbContext.Vehicles.AnyAsync(ct)
            || await dbContext.Parcels.AnyAsync(ct)
            || await dbContext.Routes.AnyAsync(ct);
    }

    private async Task<bool> HasAnySeededDevelopmentDataAsync(AppDbContext dbContext, CancellationToken ct)
    {
        var seededAddressIds = new[]
        {
            TestDepotAddressId,
            TestParcelShipperAddressId,
            TestParcelRecipientAddressId,
        };

        var seededDriverIds = TestDriverSeeds
            .Select(seed => seed.DriverId)
            .ToArray();
        var seededVehicleIds = VehicleSeeds
            .Select(seed => seed.Id)
            .ToArray();
        var seededParcelIds = TestParcelSeeds
            .Select(seed => seed.Id)
            .Concat(DevelopmentRouteParcelSeeds.Select(seed => seed.Id))
            .ToArray();
        var seededRouteIds = BuildDevelopmentRouteSeeds()
            .Select(seed => seed.Id)
            .ToArray();

        return await dbContext.Depots.AnyAsync(depot => depot.Id == TestDepotId, ct)
            || await dbContext.Addresses.AnyAsync(address => seededAddressIds.Contains(address.Id), ct)
            || await dbContext.Zones.AnyAsync(zone => zone.Id == TestZoneId, ct)
            || await dbContext.Drivers.AnyAsync(driver => seededDriverIds.Contains(driver.Id), ct)
            || await dbContext.Vehicles.AnyAsync(vehicle => seededVehicleIds.Contains(vehicle.Id), ct)
            || await dbContext.Parcels.AnyAsync(parcel => seededParcelIds.Contains(parcel.Id), ct)
            || await dbContext.Routes.AnyAsync(route => seededRouteIds.Contains(route.Id), ct);
    }

    private async Task<bool> HasCompletedDevelopmentSeedAsync(
        AppDbContext dbContext,
        bool enableTestSupport,
        CancellationToken ct)
    {
        var seededDriverIds = TestDriverSeeds
            .Select(seed => seed.DriverId)
            .ToArray();
        var seededVehicleIds = VehicleSeeds
            .Select(seed => seed.Id)
            .ToArray();
        var seededParcelIds = TestParcelSeeds
            .Select(seed => seed.Id)
            .ToList();

        if (!enableTestSupport)
        {
            seededParcelIds.AddRange(DevelopmentRouteParcelSeeds.Select(seed => seed.Id));
        }

        var seededRouteIds = enableTestSupport
            ? Array.Empty<Guid>()
            : BuildDevelopmentRouteSeeds().Select(seed => seed.Id).ToArray();

        return await dbContext.Depots.AnyAsync(depot => depot.Id == TestDepotId, ct)
            && await dbContext.Zones.AnyAsync(zone => zone.Id == TestZoneId, ct)
            && (await dbContext.Drivers.CountAsync(driver => seededDriverIds.Contains(driver.Id), ct)) == seededDriverIds.Length
            && (await dbContext.Vehicles.CountAsync(vehicle => seededVehicleIds.Contains(vehicle.Id), ct)) == seededVehicleIds.Length
            && (await dbContext.Parcels.CountAsync(parcel => seededParcelIds.Contains(parcel.Id), ct)) == seededParcelIds.Count
            && (seededRouteIds.Length == 0
                || (await dbContext.Routes.CountAsync(route => seededRouteIds.Contains(route.Id), ct)) == seededRouteIds.Length);
    }

    private async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = Enum.GetValues<PredefinedRole>();

        foreach (var role in roles)
        {
            var name = role.ToString();
            if (await roleManager.RoleExistsAsync(name))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new ApplicationRole
            {
                Name = name,
                IsDefault = role == PredefinedRole.Driver
            });

            if (result.Succeeded)
            {
                logger.LogInformation("Seeded role: {Role}", name);
            }
            else
            {
                logger.LogError(
                    "Failed to seed role {Role}: {Errors}",
                    name,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, CancellationToken cancellationToken)
    {
        var email = configuration["AdminCredentials:Email"] ?? DefaultAdminEmail;
        var password = configuration["AdminCredentials:Password"] ?? DefaultAdminPassword;

        var existingAdmins = await userManager.GetUsersInRoleAsync(PredefinedRole.Admin.ToString());
        if (!existingAdmins.Any())
        {
            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "System",
                LastName = "Admin",
                IsActive = true,
                IsSystemAdmin = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "Seeder"
            };

            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Failed to create admin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            var roleResult = await userManager.AddToRoleAsync(admin, PredefinedRole.Admin.ToString());
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Seeded admin user: {Email}", email);
            }
            else
            {
                logger.LogError(
                    "Failed to assign Admin role to seeded user: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogDebug("Admin user already exists - skipping seed");
        }

        await EnsureSystemAdminMarkerAsync(userManager, email, cancellationToken);
    }

    private async Task EnsureSystemAdminMarkerAsync(
        UserManager<ApplicationUser> userManager,
        string configuredAdminEmail,
        CancellationToken cancellationToken)
    {
        var protectedAdmins = await userManager.Users
            .Where(user => user.IsSystemAdmin)
            .ToListAsync(cancellationToken);

        if (protectedAdmins.Count == 1)
        {
            return;
        }

        if (protectedAdmins.Count > 1)
        {
            logger.LogWarning(
                "Multiple protected system admin accounts were found: {Emails}",
                string.Join(", ", protectedAdmins.Select(user => user.Email)));
            return;
        }

        var seededCandidates = await userManager.Users
            .Where(user => user.CreatedBy == "Seeder")
            .ToListAsync(cancellationToken);

        if (seededCandidates.Count == 1 &&
            await userManager.IsInRoleAsync(seededCandidates[0], PredefinedRole.Admin.ToString()))
        {
            await MarkSystemAdminAsync(userManager, seededCandidates[0]);
            return;
        }

        if (seededCandidates.Count > 1)
        {
            logger.LogWarning(
                "Unable to identify the legacy system admin because multiple Seeder-created users were found: {Emails}",
                string.Join(", ", seededCandidates.Select(user => user.Email)));
            return;
        }

        var configuredUser = await userManager.FindByEmailAsync(configuredAdminEmail);
        if (configuredUser is not null &&
            await userManager.IsInRoleAsync(configuredUser, PredefinedRole.Admin.ToString()))
        {
            await MarkSystemAdminAsync(userManager, configuredUser);
            return;
        }

        logger.LogWarning(
            "Unable to identify the legacy system admin for protection backfill. Checked CreatedBy='Seeder' and configured admin email {Email}.",
            configuredAdminEmail);
    }

    private async Task MarkSystemAdminAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        user.IsSystemAdmin = true;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            logger.LogInformation("Marked user {Email} as the protected system admin.", user.Email);
            return;
        }

        logger.LogError(
            "Failed to mark user {Email} as the protected system admin: {Errors}",
            user.Email,
            string.Join(", ", result.Errors.Select(error => error.Description)));
    }

    // ── Test Depot ─────────────────────────────────────────────────────────────

    private async Task SeedTestDepotAsync(AppDbContext dbContext, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (await dbContext.Depots.AnyAsync(d => d.Id == TestDepotId, ct))
        {
            logger.LogDebug("Test depot already exists — skipping seed");
            return;
        }

        var address = await dbContext.Addresses
            .FirstOrDefaultAsync(candidate => candidate.Id == TestDepotAddressId, ct)
            ?? new Address
        {
            Id = TestDepotAddressId,
            Street1 = TestDepotAddressSeed.Street1,
            City = TestDepotAddressSeed.City,
            State = TestDepotAddressSeed.State,
            PostalCode = TestDepotAddressSeed.PostalCode,
            CountryCode = TestDepotAddressSeed.CountryCode,
            IsResidential = false,
            CompanyName = TestDepotAddressSeed.CompanyName,
            Phone = TestDepotAddressSeed.Phone,
            Email = TestDepotAddressSeed.Email,
            GeoLocation = GeometryFactory.CreatePoint(
                new Coordinate(TestDepotAddressSeed.Longitude, TestDepotAddressSeed.Latitude)),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder"
        };

        ApplyAddressSeed(address, TestDepotAddressSeed, now);

        var depot = await dbContext.Depots
            .FirstOrDefaultAsync(candidate => candidate.Id == TestDepotId, ct)
            ?? new Depot
        {
            Id = TestDepotId,
            Name = SeededTestDepotName,
            AddressId = address.Id,
            Address = address,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder"
        };

        depot.Name = SeededTestDepotName;
        depot.AddressId = address.Id;
        depot.Address = address;
        depot.IsActive = true;
        depot.LastModifiedAt = now;
        depot.LastModifiedBy = "Seeder";

        if (dbContext.Entry(address).State == EntityState.Detached)
        {
            dbContext.Addresses.Add(address);
        }

        if (dbContext.Entry(depot).State == EntityState.Detached)
        {
            dbContext.Depots.Add(depot);
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Seeded or updated test depot: {DepotId}", TestDepotId);
    }

    // ── Test zone (PostGIS) ───────────────────────────────────────────────────

    private async Task SeedTestZoneAsync(AppDbContext dbContext, CancellationToken ct)
    {
        if (await dbContext.Zones.AnyAsync(z => z.Id == TestZoneId, ct))
        {
            logger.LogDebug("Test zone already exists — skipping seed");
            return;
        }

        if (!await dbContext.Depots.AnyAsync(d => d.Id == TestDepotId, ct))
        {
            logger.LogWarning("Test depot missing; cannot seed test zone");
            return;
        }

        var boundaryCoords = new[]
        {
            new Coordinate(-87.6460, 41.8745),
            new Coordinate(-87.6180, 41.8745),
            new Coordinate(-87.6180, 41.8995),
            new Coordinate(-87.6460, 41.8995),
            new Coordinate(-87.6460, 41.8745),
        };

        var now = DateTimeOffset.UtcNow;
        var zone = await dbContext.Zones
            .FirstOrDefaultAsync(candidate => candidate.Id == TestZoneId, ct)
            ?? new Zone
        {
            Id = TestZoneId,
            Name = SeededTestZoneName,
            Boundary = GeometryFactory.CreatePolygon(boundaryCoords),
            IsActive = true,
            DepotId = TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder",
        };

        zone.Name = SeededTestZoneName;
        zone.Boundary = GeometryFactory.CreatePolygon(boundaryCoords);
        zone.IsActive = true;
        zone.DepotId = TestDepotId;
        zone.LastModifiedAt = now;
        zone.LastModifiedBy = "Seeder";

        if (dbContext.Entry(zone).State == EntityState.Detached)
        {
            dbContext.Zones.Add(zone);
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Seeded or updated test zone: {ZoneId}", TestZoneId);
    }

    // ── Test parcels (individual recipient addresses) ───────────────────────────

    private static readonly ParcelSeed[] TestParcelSeeds =
    [
        new(
            TestParcelId,
            "LMTESTSEED0001",
            2.5m,
            ParcelStatus.Sorted,
            new AddressSeed(
                TestParcelRecipientAddressId,
                "111 S Michigan Ave",
                null,
                "Chicago",
                "IL",
                "60603",
                "US",
                false,
                "Olivia Hart",
                "Michigan Avenue Intake",
                "+13125550101",
                "olivia.hart@example.com",
                -87.6221380,
                41.8798388),
            "Priority documents for downtown delivery",
            "Satchel"),
        new(
            new Guid("00000000-0000-0000-0000-000000000010"),
            "LMTESTSEED0002",
            1.2m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000101"),
                "78 E Washington St",
                null,
                "Chicago",
                "IL",
                "60602",
                "US",
                false,
                "Mason Lee",
                "Washington Legal",
                "+13125550102",
                "mason.lee@example.com",
                -87.6248023,
                41.8838628),
            "Small legal packet"),
        new(
            new Guid("00000000-0000-0000-0000-000000000011"),
            "LMTESTSEED0003",
            5.0m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000102"),
                "400 S State St",
                null,
                "Chicago",
                "IL",
                "60605",
                "US",
                false,
                "Ava Chen",
                "State Street Clinic",
                "+13125550103",
                "ava.chen@example.com",
                -87.6282117,
                41.8762833),
            "Medical supplies"),
        new(
            new Guid("00000000-0000-0000-0000-000000000012"),
            "LMTESTSEED0004",
            0.5m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000103"),
                "121 N LaSalle St",
                null,
                "Chicago",
                "IL",
                "60602",
                "US",
                false,
                "Noah Wilson",
                "LaSalle Advisory",
                "+13125550104",
                "noah.wilson@example.com",
                -87.6319503,
                41.8838293),
            "Replacement phone case"),
        new(
            new Guid("00000000-0000-0000-0000-000000000013"),
            "LMTESTSEED0005",
            3.3m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000104"),
                "233 S Wacker Dr",
                null,
                "Chicago",
                "IL",
                "60606",
                "US",
                false,
                "Emma Davis",
                "Wacker Cafe Supply",
                "+13125550105",
                "emma.davis@example.com",
                -87.6359612,
                41.8787381),
            "Cafe supplies"),
        new(
            new Guid("00000000-0000-0000-0000-000000000014"),
            "LMTESTSEED0006",
            2.0m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000105"),
                "20 N Wacker Dr",
                null,
                "Chicago",
                "IL",
                "60606",
                "US",
                false,
                "Liam Brown",
                "Wacker Operations",
                "+13125550106",
                "liam.brown@example.com",
                -87.6374724,
                41.8827112),
            "Retail inventory restock"),
        new(
            new Guid("00000000-0000-0000-0000-000000000015"),
            "LMTESTSEED0007",
            4.0m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000106"),
                "200 E Randolph St",
                null,
                "Chicago",
                "IL",
                "60601",
                "US",
                false,
                "Sophia Martin",
                "Randolph Finance",
                "+13125550107",
                "sophia.martin@example.com",
                -87.6215489,
                41.8852857),
            "Printed finance reports"),
        new(
            new Guid("00000000-0000-0000-0000-000000000016"),
            "LMTESTSEED0008",
            1.8m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000107"),
                "180 N Stetson Ave",
                null,
                "Chicago",
                "IL",
                "60601",
                "US",
                false,
                "Jack Turner",
                "Stetson Legal",
                "+13125550108",
                "jack.turner@example.com",
                -87.6227225,
                41.8855410),
            "Contract packet"),
        new(
            new Guid("00000000-0000-0000-0000-000000000017"),
            "LMTESTSEED0009",
            6.5m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000108"),
                "433 W Van Buren St",
                null,
                "Chicago",
                "IL",
                "60607",
                "US",
                false,
                "Lucas Green",
                "Van Buren Retail",
                "+13125550109",
                "lucas.green@example.com",
                -87.6387645,
                41.8759856),
            "Home appliance part",
            "Box"),
    ];

    private static readonly VehicleSeed[] VehicleSeeds =
    [
        new(TestVehicleId, "TEST-SEED-V001", VehicleType.Van, 50, 500m, VehicleStatus.Available),
        new(TestVehicle2Id, "TEST-SEED-V002", VehicleType.Van, 45, 450m, VehicleStatus.Available),
        new(TestVehicle3Id, "TEST-SEED-V003", VehicleType.Van, 40, 420m, VehicleStatus.Available),
        new(TestVehicle4Id, "TEST-SEED-V004", VehicleType.Van, 80, 900m, VehicleStatus.Available),
    ];

    private static readonly ParcelSeed[] DevelopmentRouteParcelSeeds =
    [
        new(
            new Guid("00000000-0000-0000-0000-000000000201"),
            "LMDEVROUTE0001",
            2.1m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000211"),
                "444 W Lake St",
                null,
                "Chicago",
                "IL",
                "60606",
                "US",
                false,
                "Harper Scott",
                "River Point Office",
                "+13125550201",
                "harper.scott@example.com",
                -87.6394541,
                41.8861713),
            "Draft route stop 1"),
        new(
            new Guid("00000000-0000-0000-0000-000000000202"),
            "LMDEVROUTE0002",
            3.0m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000212"),
                "222 W Merchandise Mart Plaza",
                null,
                "Chicago",
                "IL",
                "60654",
                "US",
                false,
                "Ethan Brooks",
                "Merchandise Mart Studio",
                "+13125550202",
                "ethan.brooks@example.com",
                -87.6344719,
                41.8888053),
            "Draft route stop 2"),
        new(
            new Guid("00000000-0000-0000-0000-000000000203"),
            "LMDEVROUTE0003",
            1.7m,
            ParcelStatus.Staged,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000213"),
                "70 W Madison St",
                null,
                "Chicago",
                "IL",
                "60602",
                "US",
                false,
                "Isla Cooper",
                "Madison Advisory",
                "+13125550203",
                "isla.cooper@example.com",
                -87.6300019,
                41.8823349),
            "Dispatched route stop 1"),
        new(
            new Guid("00000000-0000-0000-0000-000000000204"),
            "LMDEVROUTE0004",
            2.4m,
            ParcelStatus.Staged,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000214"),
                "30 N LaSalle St",
                null,
                "Chicago",
                "IL",
                "60602",
                "US",
                false,
                "Mila Foster",
                "LaSalle Tower",
                "+13125550204",
                "mila.foster@example.com",
                -87.6329140,
                41.8828459),
            "Dispatched route stop 2"),
        new(
            new Guid("00000000-0000-0000-0000-000000000205"),
            "LMDEVROUTE0005",
            4.2m,
            ParcelStatus.Loaded,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000215"),
                "190 S LaSalle St",
                null,
                "Chicago",
                "IL",
                "60603",
                "US",
                false,
                "Leo Simmons",
                "Financial District Office",
                "+13125550205",
                "leo.simmons@example.com",
                -87.6326495,
                41.8797583),
            "In-progress route stop 1"),
        new(
            new Guid("00000000-0000-0000-0000-000000000206"),
            "LMDEVROUTE0006",
            3.5m,
            ParcelStatus.OutForDelivery,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000216"),
                "175 W Jackson Blvd",
                null,
                "Chicago",
                "IL",
                "60604",
                "US",
                false,
                "Grace Howard",
                "Jackson Dental",
                "+13125550206",
                "grace.howard@example.com",
                -87.6332110,
                41.8776429),
            "In-progress route stop 2"),
        new(
            new Guid("00000000-0000-0000-0000-000000000207"),
            "LMDEVROUTE0007",
            2.8m,
            ParcelStatus.Delivered,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000217"),
                "35 E Wacker Dr",
                null,
                "Chicago",
                "IL",
                "60601",
                "US",
                false,
                "Zoe Perry",
                "Wacker Chambers",
                "+13125550207",
                "zoe.perry@example.com",
                -87.6267925,
                41.8864833),
            "Completed route stop 1"),
        new(
            new Guid("00000000-0000-0000-0000-000000000208"),
            "LMDEVROUTE0008",
            1.9m,
            ParcelStatus.Delivered,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000218"),
                "455 N Cityfront Plaza Dr",
                null,
                "Chicago",
                "IL",
                "60611",
                "US",
                false,
                "Nina Ward",
                "Cityfront Office",
                "+13125550208",
                "nina.ward@example.com",
                -87.6210753,
                41.8900569),
            "Completed route stop 2"),
    ];

    private async Task SeedTestParcelsAsync(AppDbContext dbContext, CancellationToken ct)
    {
        if (!await dbContext.Zones.AnyAsync(z => z.Id == TestZoneId, ct))
        {
            logger.LogWarning("Test zone missing; cannot seed test parcels");
            return;
        }

        await UpsertParcelSeedsAsync(dbContext, TestParcelSeeds, ct);
    }

    // ── Test drivers (Identity + Driver row; same password for local dev) ───

    /// <summary>All seeded drivers use password <c>Driver@12345</c> (see seeder).</summary>
    private sealed record TestDriverSeed(
        Guid UserId,
        Guid DriverId,
        string Email,
        string FirstName,
        string LastName,
        string Phone,
        string LicenseNumber);

    private static readonly TestDriverSeed[] TestDriverSeeds =
    [
        new(
            TestDriverUserId,
            TestDriverId,
            "driver.test@lastmile.local",
            "Test",
            "Driver",
            "+13125551001",
            "TEST-LIC-SEED-001"),
        new(
            TestDriver2UserId,
            TestDriver2Id,
            "driver2.test@lastmile.local",
            "Alex",
            "Nguyen",
            "+13125551002",
            "TEST-LIC-SEED-002"),
        new(
            TestDriver3UserId,
            TestDriver3Id,
            "driver3.test@lastmile.local",
            "Sam",
            "Reyes",
            "+13125551003",
            "TEST-LIC-SEED-003"),
        new(
            TestDriver4UserId,
            TestDriver4Id,
            "driver4.test@lastmile.local",
            "Jordan",
            "Park",
            "+13125551004",
            "TEST-LIC-SEED-004"),
    ];

    private async Task SeedTestDriverAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        CancellationToken ct)
    {
        if (!await dbContext.Zones.AnyAsync(z => z.Id == TestZoneId, ct))
        {
            logger.LogWarning("Test zone missing; cannot seed test drivers");
            return;
        }

        const string password = "Driver@12345";
        var licenseExpiry = DateTimeOffset.UtcNow.AddYears(3);

        foreach (var seed in TestDriverSeeds)
        {
            if (await dbContext.Drivers.AnyAsync(d => d.Id == seed.DriverId, ct))
                continue;

            var user = await userManager.FindByIdAsync(seed.UserId.ToString());
            if (user is null)
            {
                var emailTaken = await userManager.FindByEmailAsync(seed.Email);
                if (emailTaken is not null)
                {
                    logger.LogWarning(
                        "Cannot seed driver {DriverId}: email {Email} is already registered with another account",
                        seed.DriverId,
                        seed.Email);
                    continue;
                }

                user = new ApplicationUser
                {
                    Id = seed.UserId,
                    UserName = seed.Email,
                    Email = seed.Email,
                    FirstName = seed.FirstName,
                    LastName = seed.LastName,
                    ZoneId = TestZoneId,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "Seeder",
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    logger.LogError(
                        "Failed to create test driver user {Email}: {Errors}",
                        seed.Email,
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }
            }

            if (!await userManager.IsInRoleAsync(user, PredefinedRole.Driver.ToString()))
            {
                var roleResult = await userManager.AddToRoleAsync(user, PredefinedRole.Driver.ToString());
                if (!roleResult.Succeeded)
                {
                    logger.LogError(
                        "Failed to assign Driver role to {Email}: {Errors}",
                        seed.Email,
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    continue;
                }
            }

            var driver = new Driver
            {
                Id = seed.DriverId,
                FirstName = seed.FirstName,
                LastName = seed.LastName,
                Phone = seed.Phone,
                Email = seed.Email,
                LicenseNumber = seed.LicenseNumber,
                LicenseExpiryDate = licenseExpiry,
                ZoneId = TestZoneId,
                DepotId = TestDepotId,
                UserId = user.Id,
                Status = DriverStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "Seeder",
            };

            dbContext.Drivers.Add(driver);
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Seeded test driver: {DriverId} ({Email})", seed.DriverId, seed.Email);
        }
    }

    // ── Test vehicle ───────────────────────────────────────────────────────────

    private async Task SeedTestVehicleAsync(AppDbContext dbContext, CancellationToken ct)
    {
        if (await dbContext.Vehicles.AnyAsync(v => v.Id == TestVehicleId, ct))
        {
            logger.LogDebug("Test vehicle already exists — skipping seed");
            return;
        }

        if (!await dbContext.Depots.AnyAsync(d => d.Id == TestDepotId, ct))
        {
            logger.LogWarning("Test depot missing; cannot seed test vehicle");
            return;
        }

        await UpsertVehicleSeedsAsync(dbContext, VehicleSeeds, ct);

    }

    private async Task UpsertVehicleSeedsAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<VehicleSeed> seeds,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var vehicleIds = seeds.Select(seed => seed.Id).ToHashSet();
        var existingVehicles = await dbContext.Vehicles
            .Where(vehicle => vehicleIds.Contains(vehicle.Id))
            .ToDictionaryAsync(vehicle => vehicle.Id, ct);

        foreach (var seed in seeds)
        {
            if (!existingVehicles.TryGetValue(seed.Id, out var vehicle))
            {
                vehicle = new Vehicle
                {
                    Id = seed.Id,
                    CreatedAt = now,
                    CreatedBy = "Seeder",
                };
                existingVehicles[seed.Id] = vehicle;
                dbContext.Vehicles.Add(vehicle);
            }

            vehicle.RegistrationPlate = seed.RegistrationPlate;
            vehicle.Type = seed.Type;
            vehicle.ParcelCapacity = seed.ParcelCapacity;
            vehicle.WeightCapacity = seed.WeightCapacity;
            vehicle.Status = seed.Status;
            vehicle.DepotId = TestDepotId;
            vehicle.LastModifiedAt = now;
            vehicle.LastModifiedBy = "Seeder";
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Seeded or updated {Count} vehicle(s)", seeds.Count);
    }

    private async Task UpsertParcelSeedsAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<ParcelSeed> seeds,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var addressSeeds = seeds
            .Select(seed => seed.RecipientAddress)
            .Append(TestParcelShipperAddressSeed)
            .DistinctBy(seed => seed.Id)
            .ToList();
        var addressIds = addressSeeds.Select(seed => seed.Id).ToHashSet();
        var existingAddresses = await dbContext.Addresses
            .Where(address => addressIds.Contains(address.Id))
            .ToDictionaryAsync(address => address.Id, ct);

        foreach (var addressSeed in addressSeeds)
        {
            if (!existingAddresses.TryGetValue(addressSeed.Id, out var address))
            {
                address = new Address
                {
                    Id = addressSeed.Id,
                    CreatedAt = now,
                    CreatedBy = "Seeder",
                };
                existingAddresses[addressSeed.Id] = address;
                dbContext.Addresses.Add(address);
            }

            ApplyAddressSeed(address, addressSeed, now);
        }

        var parcelIds = seeds.Select(seed => seed.Id).ToHashSet();
        var existingParcels = await dbContext.Parcels
            .Where(parcel => parcelIds.Contains(parcel.Id))
            .ToDictionaryAsync(parcel => parcel.Id, ct);

        foreach (var seed in seeds)
        {
            if (!existingParcels.TryGetValue(seed.Id, out var parcel))
            {
                parcel = new Parcel
                {
                    Id = seed.Id,
                    CreatedAt = now,
                    CreatedBy = "Seeder",
                };
                existingParcels[seed.Id] = parcel;
                dbContext.Parcels.Add(parcel);
            }

            parcel.TrackingNumber = seed.TrackingNumber;
            parcel.Description = seed.Description;
            parcel.ServiceType = seed.ServiceType;
            parcel.Status = seed.Status;
            parcel.CancellationReason = null;
            parcel.ShipperAddressId = TestParcelShipperAddressId;
            parcel.RecipientAddressId = seed.RecipientAddress.Id;
            parcel.Weight = seed.WeightKg;
            parcel.WeightUnit = WeightUnit.Kg;
            parcel.Length = 30;
            parcel.Width = 20;
            parcel.Height = 10;
            parcel.DimensionUnit = DimensionUnit.Cm;
            parcel.DeclaredValue = 100m;
            parcel.Currency = "USD";
            parcel.EstimatedDeliveryDate = now.AddDays(seed.Status == ParcelStatus.Delivered ? -1 : 1);
            parcel.ActualDeliveryDate = seed.Status == ParcelStatus.Delivered ? now.AddHours(-2) : null;
            parcel.DeliveryAttempts = seed.Status == ParcelStatus.Delivered ? 1 : 0;
            parcel.ParcelType = seed.ParcelType;
            parcel.ZoneId = TestZoneId;
            parcel.LastModifiedAt = now;
            parcel.LastModifiedBy = "Seeder";
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Seeded or updated {Count} parcel(s)", seeds.Count);
    }

    private async Task SeedDevelopmentRoutesAsync(AppDbContext dbContext, CancellationToken ct)
    {
        if (!await dbContext.Zones.AnyAsync(zone => zone.Id == TestZoneId, ct))
        {
            logger.LogWarning("Test zone missing; cannot seed development routes");
            return;
        }

        await UpsertParcelSeedsAsync(dbContext, DevelopmentRouteParcelSeeds, ct);

        var depotAddress = await dbContext.Addresses
            .FirstOrDefaultAsync(address => address.Id == TestDepotAddressId, ct);
        if (depotAddress?.GeoLocation is null)
        {
            logger.LogWarning("Depot address geo-location missing; cannot seed development routes");
            return;
        }

        var routeSeeds = BuildDevelopmentRouteSeeds();
        var parcelIds = routeSeeds.SelectMany(seed => seed.ParcelIds).ToHashSet();
        var vehicleIds = routeSeeds.Select(seed => seed.VehicleId).ToHashSet();
        var driverIds = routeSeeds.Select(seed => seed.DriverId).ToHashSet();

        var parcels = await dbContext.Parcels
            .Include(parcel => parcel.RecipientAddress)
            .Where(parcel => parcelIds.Contains(parcel.Id))
            .ToDictionaryAsync(parcel => parcel.Id, ct);
        var vehicles = await dbContext.Vehicles
            .Where(vehicle => vehicleIds.Contains(vehicle.Id))
            .ToDictionaryAsync(vehicle => vehicle.Id, ct);
        var drivers = await dbContext.Drivers
            .Where(driver => driverIds.Contains(driver.Id))
            .ToDictionaryAsync(driver => driver.Id, ct);

        var pendingStopParcelAssignments = new List<(RouteStop Stop, Parcel Parcel)>();

        foreach (var seed in routeSeeds)
        {
            if (!vehicles.ContainsKey(seed.VehicleId) || !drivers.ContainsKey(seed.DriverId))
            {
                logger.LogWarning(
                    "Skipping development route {RouteId}; missing seeded driver or vehicle",
                    seed.Id);
                continue;
            }

            var route = await dbContext.Routes
                .Include(candidate => candidate.Parcels)
                .Include(candidate => candidate.Stops)
                .ThenInclude(stop => stop.Parcels)
                .Include(candidate => candidate.AssignmentAuditTrail)
                .FirstOrDefaultAsync(candidate => candidate.Id == seed.Id, ct);
            var routeAlreadyExists = route is not null;
            route ??= new Route
                {
                    Id = seed.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "Seeder",
                };

            if (!routeAlreadyExists)
            {
                dbContext.Routes.Add(route);
            }
            else
            {
                route.Parcels.Clear();

                if (route.Stops.Count > 0)
                {
                    dbContext.RouteStops.RemoveRange(route.Stops.ToList());
                }

                if (route.AssignmentAuditTrail.Count > 0)
                {
                    dbContext.RouteAssignmentAuditEntries.RemoveRange(route.AssignmentAuditTrail.ToList());
                }

                await dbContext.SaveChangesAsync(ct);
                route.Stops = new List<RouteStop>();
                route.AssignmentAuditTrail = new List<RouteAssignmentAuditEntry>();
            }

            route.ZoneId = TestZoneId;
            route.VehicleId = seed.VehicleId;
            route.DriverId = seed.DriverId;
            route.StartDate = seed.StartDate;
            route.DispatchedAt = seed.Status is RouteStatus.Dispatched or RouteStatus.InProgress or RouteStatus.Completed
                ? seed.StartDate.AddMinutes(-20)
                : null;
            route.EndDate = seed.Status == RouteStatus.Completed ? seed.StartDate.AddHours(5) : null;
            route.StartMileage = seed.StartMileage;
            route.EndMileage = seed.Status == RouteStatus.Completed ? seed.EndMileage : 0;
            route.StagingArea = seed.StagingArea;
            route.Status = seed.Status;
            route.CancellationReason = null;
            route.LastModifiedAt = DateTimeOffset.UtcNow;
            route.LastModifiedBy = "Seeder";

            route.Parcels.Clear();
            foreach (var parcelId in seed.ParcelIds)
            {
                if (parcels.TryGetValue(parcelId, out var parcel))
                {
                    parcel.EstimatedDeliveryDate = seed.StartDate.AddHours(6);
                    parcel.ActualDeliveryDate = seed.Status == RouteStatus.Completed
                        ? seed.StartDate.AddHours(5)
                        : null;
                    parcel.DeliveryAttempts = seed.Status == RouteStatus.Completed ? 1 : 0;
                    route.Parcels.Add(parcel);
                }
            }

            var orderedParcels = seed.ParcelIds
                .Where(parcels.ContainsKey)
                .Select(parcelId => parcels[parcelId])
                .ToList();
            var stopPoints = orderedParcels
                .Select(parcel => parcel.RecipientAddress.GeoLocation)
                .OfType<Point>()
                .ToList();
            var metrics = BuildRouteMetrics(depotAddress.GeoLocation, stopPoints);

            route.PlannedDistanceMeters = metrics.DistanceMeters;
            route.PlannedDurationSeconds = metrics.DurationSeconds;
            route.PlannedPath = metrics.Path;

            for (var index = 0; index < orderedParcels.Count; index++)
            {
                var parcel = orderedParcels[index];
                var address = parcel.RecipientAddress;
                if (address.GeoLocation is null)
                {
                    continue;
                }

                var stop = new RouteStop
                {
                    Id = Guid.NewGuid(),
                    Route = route,
                    Sequence = index + 1,
                    RecipientLabel = BuildRecipientLabel(address),
                    Street1 = address.Street1,
                    Street2 = address.Street2,
                    City = address.City,
                    State = address.State,
                    PostalCode = address.PostalCode,
                    CountryCode = address.CountryCode,
                    StopLocation = address.GeoLocation,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "Seeder",
                };
                route.Stops.Add(stop);
                pendingStopParcelAssignments.Add((stop, parcel));
            }

            var driver = drivers[seed.DriverId];
            var vehicle = vehicles[seed.VehicleId];
            route.AssignmentAuditTrail.Add(new RouteAssignmentAuditEntry
            {
                Id = Guid.NewGuid(),
                RouteId = route.Id,
                Action = RouteAssignmentAuditAction.Assigned,
                NewDriverId = driver.Id,
                NewDriverName = $"{driver.FirstName} {driver.LastName}".Trim(),
                NewVehicleId = vehicle.Id,
                NewVehiclePlate = vehicle.RegistrationPlate,
                ChangedAt = seed.StartDate.AddMinutes(-20),
                ChangedBy = "Seeder",
            });
        }

        var activeVehicleIds = routeSeeds
            .Where(seed => seed.Status is RouteStatus.Draft or RouteStatus.Dispatched or RouteStatus.InProgress)
            .Select(seed => seed.VehicleId)
            .ToHashSet();

        foreach (var seed in VehicleSeeds)
        {
            if (!vehicles.TryGetValue(seed.Id, out var vehicle))
            {
                continue;
            }

            vehicle.Status = activeVehicleIds.Contains(vehicle.Id)
                ? VehicleStatus.InUse
                : seed.Status;
            vehicle.LastModifiedAt = DateTimeOffset.UtcNow;
            vehicle.LastModifiedBy = "Seeder";
        }

        await dbContext.SaveChangesAsync(ct);

        foreach (var (stop, parcel) in pendingStopParcelAssignments)
        {
            stop.Parcels.Add(parcel);
        }

        if (pendingStopParcelAssignments.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }

        logger.LogInformation("Seeded or updated {Count} development route(s)", routeSeeds.Count);
    }

    private static IReadOnlyList<DemoRouteSeed> BuildDevelopmentRouteSeeds()
    {
        var localNow = DateTimeOffset.Now;
        var today = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, localNow.Offset);
        var utcToday = today.ToUniversalTime();

        return
        [
            new(
                DevelopmentDraftRouteId,
                TestVehicleId,
                TestDriverId,
                RouteStatus.Draft,
                StagingArea.A,
                utcToday.AddHours(8),
                18240,
                0,
                [new Guid("00000000-0000-0000-0000-000000000201"), new Guid("00000000-0000-0000-0000-000000000202")]),
            new(
                DevelopmentDispatchedRouteId,
                TestVehicle2Id,
                TestDriver2Id,
                RouteStatus.Dispatched,
                StagingArea.B,
                utcToday.AddHours(9),
                18288,
                0,
                [new Guid("00000000-0000-0000-0000-000000000203"), new Guid("00000000-0000-0000-0000-000000000204")]),
            new(
                DevelopmentInProgressRouteId,
                TestVehicle3Id,
                TestDriver3Id,
                RouteStatus.InProgress,
                StagingArea.A,
                utcToday.AddHours(10),
                18340,
                0,
                [new Guid("00000000-0000-0000-0000-000000000205"), new Guid("00000000-0000-0000-0000-000000000206")]),
            new(
                DevelopmentCompletedRouteId,
                TestVehicle4Id,
                TestDriver4Id,
                RouteStatus.Completed,
                StagingArea.B,
                utcToday.AddDays(-1).AddHours(8),
                18110,
                18178,
                [new Guid("00000000-0000-0000-0000-000000000207"), new Guid("00000000-0000-0000-0000-000000000208")]),
        ];
    }

    private static void ApplyAddressSeed(Address address, AddressSeed seed, DateTimeOffset now)
    {
        address.Street1 = seed.Street1;
        address.Street2 = seed.Street2;
        address.City = seed.City;
        address.State = seed.State;
        address.PostalCode = seed.PostalCode;
        address.CountryCode = seed.CountryCode;
        address.IsResidential = seed.IsResidential;
        address.ContactName = seed.ContactName;
        address.CompanyName = seed.CompanyName;
        address.Phone = seed.Phone;
        address.Email = seed.Email;
        address.GeoLocation = GeometryFactory.CreatePoint(new Coordinate(seed.Longitude, seed.Latitude));
        address.LastModifiedAt = now;
        address.LastModifiedBy = "Seeder";
    }

    private static string BuildRecipientLabel(Address address)
    {
        if (!string.IsNullOrWhiteSpace(address.ContactName))
        {
            return address.ContactName;
        }

        if (!string.IsNullOrWhiteSpace(address.CompanyName))
        {
            return address.CompanyName;
        }

        return address.Street1;
    }

    private static (int DistanceMeters, int DurationSeconds, LineString? Path) BuildRouteMetrics(
        Point depotPoint,
        IReadOnlyList<Point> stopPoints)
    {
        if (stopPoints.Count == 0)
        {
            return (0, 0, null);
        }

        var orderedPoints = new List<Point>(stopPoints.Count + 2) { depotPoint };
        orderedPoints.AddRange(stopPoints);
        orderedPoints.Add(depotPoint);

        var distanceMeters = 0d;
        for (var index = 0; index < orderedPoints.Count - 1; index++)
        {
            distanceMeters += HaversineMeters(orderedPoints[index], orderedPoints[index + 1]);
        }

        var path = GeometryFactory.CreateLineString(orderedPoints.Select(point => point.Coordinate).ToArray());
        var durationSeconds = (int)Math.Round(distanceMeters / 13.89d);
        return ((int)Math.Round(distanceMeters), durationSeconds, path);
    }

    private static double HaversineMeters(Point origin, Point destination)
    {
        const double earthRadius = 6_371_000d;
        var dLat = DegreesToRadians(destination.Y - origin.Y);
        var dLon = DegreesToRadians(destination.X - origin.X);
        var lat1 = DegreesToRadians(origin.Y);
        var lat2 = DegreesToRadians(destination.Y);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2)
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
