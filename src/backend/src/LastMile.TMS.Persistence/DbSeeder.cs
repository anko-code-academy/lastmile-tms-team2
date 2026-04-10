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
        "201 Sussex Street",
        null,
        "Sydney",
        "NSW",
        "2000",
        "AU",
        false,
        null,
        "Last Mile Central Depot",
        "+61290000000",
        "depot@lastmile.local",
        151.20440,
        -33.87273);

    private static readonly AddressSeed TestParcelShipperAddressSeed = new(
        TestParcelShipperAddressId,
        "388 George Street",
        null,
        "Sydney",
        "NSW",
        "2000",
        "AU",
        false,
        "Dispatch Desk",
        "Acme Fulfillment",
        "+61290000010",
        "dock@acme.local",
        151.20742,
        -33.86938);

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
        await SeedTestDepotAsync(dbContext, cancellationToken);

        // Zone + parcel use PostGIS geometry — skip for InMemory test databases.
        if (connectionString != "InMemory")
        {
            await SeedTestZoneAsync(dbContext, cancellationToken);
            await SeedTestDriverAsync(userManager, dbContext, cancellationToken);
            await SeedTestVehicleAsync(dbContext, cancellationToken);
            await SeedTestParcelsAsync(dbContext, cancellationToken);

            if (!enableTestSupport)
            {
                await SeedDevelopmentRoutesAsync(dbContext, cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
        if (false && await dbContext.Depots.AnyAsync(d => d.Id == TestDepotId, ct))
        {
            logger.LogDebug("Test depot already exists — skipping seed");
            return;
        }

        var address = await dbContext.Addresses
            .FirstOrDefaultAsync(candidate => candidate.Id == TestDepotAddressId, ct)
            ?? new Address
        {
            Id = TestDepotAddressId,
            Street1 = "201 Sussex Street",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
            IsResidential = false,
            CompanyName = "Last Mile Central Depot",
            Phone = "+61290000000",
            Email = "depot@lastmile.local",
            GeoLocation = GeometryFactory.CreatePoint(new Coordinate(151.20440, -33.87273)),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder"
        };

        ApplyAddressSeed(address, TestDepotAddressSeed, now);

        var depot = await dbContext.Depots
            .FirstOrDefaultAsync(candidate => candidate.Id == TestDepotId, ct)
            ?? new Depot
        {
            Id = TestDepotId,
            Name = "Test Depot",
            AddressId = address.Id,
            Address = address,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder"
        };

        depot.Name = "Test Depot";
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
        if (false && await dbContext.Zones.AnyAsync(z => z.Id == TestZoneId, ct))
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
            new Coordinate(151.0, -33.0),
            new Coordinate(152.0, -33.0),
            new Coordinate(152.0, -34.0),
            new Coordinate(151.0, -34.0),
            new Coordinate(151.0, -33.0),
        };

        var now = DateTimeOffset.UtcNow;
        var zone = await dbContext.Zones
            .FirstOrDefaultAsync(candidate => candidate.Id == TestZoneId, ct)
            ?? new Zone
        {
            Id = TestZoneId,
            Name = "Test Zone",
            Boundary = GeometryFactory.CreatePolygon(boundaryCoords),
            IsActive = true,
            DepotId = TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder",
        };

        zone.Name = "Test Zone";
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
                "1 Market Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Olivia Hart",
                null,
                "+61290000101",
                "olivia.hart@example.com",
                151.20576,
                -33.87135),
            "Priority documents for CBD delivery",
            "Satchel"),
        new(
            new Guid("00000000-0000-0000-0000-000000000010"),
            "LMTESTSEED0002",
            1.2m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000101"),
                "20 Bridge Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Mason Lee",
                "Bridge Legal",
                "+61290000102",
                "mason.lee@example.com",
                151.21066,
                -33.86351),
            "Small legal packet"),
        new(
            new Guid("00000000-0000-0000-0000-000000000011"),
            "LMTESTSEED0003",
            5.0m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000102"),
                "5 Martin Place",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Ava Chen",
                "Martin Place Clinic",
                "+61290000103",
                "ava.chen@example.com",
                151.20926,
                -33.86772),
            "Medical supplies"),
        new(
            new Guid("00000000-0000-0000-0000-000000000012"),
            "LMTESTSEED0004",
            0.5m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000103"),
                "68 Pitt Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                true,
                "Noah Wilson",
                null,
                "+61290000104",
                "noah.wilson@example.com",
                151.20766,
                -33.86701),
            "Replacement phone case"),
        new(
            new Guid("00000000-0000-0000-0000-000000000013"),
            "LMTESTSEED0005",
            3.3m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000104"),
                "200 George Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Emma Davis",
                "Rocks Cafe",
                "+61290000105",
                "emma.davis@example.com",
                151.20618,
                -33.86095),
            "Cafe supplies"),
        new(
            new Guid("00000000-0000-0000-0000-000000000014"),
            "LMTESTSEED0006",
            2.0m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000105"),
                "201 George Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Liam Brown",
                "Quay Retail",
                "+61290000106",
                "liam.brown@example.com",
                151.20665,
                -33.86108),
            "Retail inventory restock"),
        new(
            new Guid("00000000-0000-0000-0000-000000000015"),
            "LMTESTSEED0007",
            4.0m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000106"),
                "8 Spring Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Sophia Martin",
                "Spring Finance",
                "+61290000107",
                "sophia.martin@example.com",
                151.21189,
                -33.86560),
            "Printed finance reports"),
        new(
            new Guid("00000000-0000-0000-0000-000000000016"),
            "LMTESTSEED0008",
            1.8m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000107"),
                "151 Clarence Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Jack Turner",
                "Clarence Legal",
                "+61290000108",
                "jack.turner@example.com",
                151.20418,
                -33.87031),
            "Contract packet"),
        new(
            new Guid("00000000-0000-0000-0000-000000000017"),
            "LMTESTSEED0009",
            6.5m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000108"),
                "2 Park Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                true,
                "Lucas Green",
                null,
                "+61290000109",
                "lucas.green@example.com",
                151.20864,
                -33.87325),
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
                "60 Margaret Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Harper Scott",
                "Northpoint Office",
                "+61290000201",
                "harper.scott@example.com",
                151.20525,
                -33.86501),
            "Draft route stop 1"),
        new(
            new Guid("00000000-0000-0000-0000-000000000202"),
            "LMDEVROUTE0002",
            3.0m,
            ParcelStatus.Sorted,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000212"),
                "135 King Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Ethan Brooks",
                "King Street Studio",
                "+61290000202",
                "ethan.brooks@example.com",
                151.20728,
                -33.86996),
            "Draft route stop 2"),
        new(
            new Guid("00000000-0000-0000-0000-000000000203"),
            "LMDEVROUTE0003",
            1.7m,
            ParcelStatus.Staged,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000213"),
                "126 Phillip Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Isla Cooper",
                "Phillip Advisory",
                "+61290000203",
                "isla.cooper@example.com",
                151.21207,
                -33.86803),
            "Dispatched route stop 1"),
        new(
            new Guid("00000000-0000-0000-0000-000000000204"),
            "LMDEVROUTE0004",
            2.4m,
            ParcelStatus.Staged,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000214"),
                "1 Bligh Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Mila Foster",
                "Bligh Tower",
                "+61290000204",
                "mila.foster@example.com",
                151.21071,
                -33.86548),
            "Dispatched route stop 2"),
        new(
            new Guid("00000000-0000-0000-0000-000000000205"),
            "LMDEVROUTE0005",
            4.2m,
            ParcelStatus.Loaded,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000215"),
                "25 Martin Place",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Leo Simmons",
                "City Bank",
                "+61290000205",
                "leo.simmons@example.com",
                151.21012,
                -33.86737),
            "In-progress route stop 1"),
        new(
            new Guid("00000000-0000-0000-0000-000000000206"),
            "LMDEVROUTE0006",
            3.5m,
            ParcelStatus.OutForDelivery,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000216"),
                "48 York Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Grace Howard",
                "York Street Dental",
                "+61290000206",
                "grace.howard@example.com",
                151.20511,
                -33.87091),
            "In-progress route stop 2"),
        new(
            new Guid("00000000-0000-0000-0000-000000000207"),
            "LMDEVROUTE0007",
            2.8m,
            ParcelStatus.Delivered,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000217"),
                "50 Bridge Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                false,
                "Zoe Perry",
                "Bridge Street Chambers",
                "+61290000207",
                "zoe.perry@example.com",
                151.20992,
                -33.86358),
            "Completed route stop 1"),
        new(
            new Guid("00000000-0000-0000-0000-000000000208"),
            "LMDEVROUTE0008",
            1.9m,
            ParcelStatus.Delivered,
            new AddressSeed(
                new Guid("00000000-0000-0000-0000-000000000218"),
                "227 Elizabeth Street",
                null,
                "Sydney",
                "NSW",
                "2000",
                "AU",
                true,
                "Nina Ward",
                null,
                "+61290000208",
                "nina.ward@example.com",
                151.20975,
                -33.87601),
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
        return;

        var now = DateTimeOffset.UtcNow;

        // Shared shipper address for all parcels.
        if (!await dbContext.Addresses.AnyAsync(a => a.Id == TestParcelShipperAddressId, ct))
        {
            dbContext.Addresses.Add(new Address
            {
                Id = TestParcelShipperAddressId,
                Street1 = "388 George Street",
                City = "Sydney",
                State = "NSW",
                PostalCode = "2000",
                CountryCode = "AU",
                IsResidential = false,
                ContactName = "Dispatch Desk",
                CompanyName = "Acme Fulfillment",
                Phone = "+61290000010",
                Email = "dock@acme.local",
                GeoLocation = GeometryFactory.CreatePoint(new Coordinate(151.20742, -33.86938)),
                CreatedAt = now,
                CreatedBy = "Seeder",
            });

            await dbContext.SaveChangesAsync(ct);
        }

        // Seed each parcel with its own individual recipient address.
        var added = 0;
        foreach (var seed in TestParcelSeeds)
        {
            if (await dbContext.Parcels.AnyAsync(p => p.Id == seed.Id, ct))
                continue;

            var ra = seed.RecipientAddress;
            if (!await dbContext.Addresses.AnyAsync(a => a.Id == ra.Id, ct))
            {
                dbContext.Addresses.Add(new Address
                {
                    Id = ra.Id,
                    Street1 = ra.Street1,
                    Street2 = ra.Street2,
                    City = ra.City,
                    State = ra.State,
                    PostalCode = ra.PostalCode,
                    CountryCode = ra.CountryCode,
                    IsResidential = ra.IsResidential,
                    ContactName = ra.ContactName,
                    CompanyName = ra.CompanyName,
                    Phone = ra.Phone,
                    Email = ra.Email,
                    GeoLocation = GeometryFactory.CreatePoint(new Coordinate(ra.Longitude, ra.Latitude)),
                    CreatedAt = now,
                    CreatedBy = "Seeder",
                });
            }

            dbContext.Parcels.Add(new Parcel
            {
                Id = seed.Id,
                TrackingNumber = seed.TrackingNumber,
                Description = "Seeded test parcel for development",
                ServiceType = ServiceType.Standard,
                Status = ParcelStatus.Sorted,
                ShipperAddressId = TestParcelShipperAddressId,
                RecipientAddressId = ra.Id,
                Weight = seed.WeightKg,
                WeightUnit = WeightUnit.Kg,
                Length = 30,
                Width = 20,
                Height = 10,
                DimensionUnit = DimensionUnit.Cm,
                DeclaredValue = 100m,
                Currency = "USD",
                EstimatedDeliveryDate = now.AddDays(7),
                DeliveryAttempts = 0,
                ZoneId = TestZoneId,
                CreatedAt = now,
                CreatedBy = "Seeder",
            });
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} test parcel(s)", added);
        }
        else
        {
            logger.LogDebug("All test parcels already exist — skipping parcel seed");
        }
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
            "+61000000001",
            "TEST-LIC-SEED-001"),
        new(
            TestDriver2UserId,
            TestDriver2Id,
            "driver2.test@lastmile.local",
            "Alex",
            "Nguyen",
            "+61000000002",
            "TEST-LIC-SEED-002"),
        new(
            TestDriver3UserId,
            TestDriver3Id,
            "driver3.test@lastmile.local",
            "Sam",
            "Reyes",
            "+61000000003",
            "TEST-LIC-SEED-003"),
        new(
            TestDriver4UserId,
            TestDriver4Id,
            "driver4.test@lastmile.local",
            "Jordan",
            "Park",
            "+61000000004",
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
        if (false && await dbContext.Vehicles.AnyAsync(v => v.Id == TestVehicleId, ct))
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
        return;

        var vehicle = new Vehicle
        {
            Id = TestVehicleId,
            RegistrationPlate = "TEST-SEED-V001",
            Type = VehicleType.Van,
            ParcelCapacity = 50,
            WeightCapacity = 500m,
            Status = VehicleStatus.Available,
            DepotId = TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder",
        };

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Seeded test vehicle: {VehicleId} ({Plate})", TestVehicleId, vehicle.RegistrationPlate);
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
            parcel.Currency = "AUD";
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
                .FirstOrDefaultAsync(candidate => candidate.Id == seed.Id, ct)
                ?? new Route
                {
                    Id = seed.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "Seeder",
                };

            if (dbContext.Entry(route).State == EntityState.Detached)
            {
                dbContext.Routes.Add(route);
            }

            route.ZoneId = TestZoneId;
            route.VehicleId = seed.VehicleId;
            route.DriverId = seed.DriverId;
            route.StartDate = seed.StartDate;
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

            if (route.Stops.Count > 0)
            {
                dbContext.RouteStops.RemoveRange(route.Stops.ToList());
                route.Stops.Clear();
            }

            if (route.AssignmentAuditTrail.Count > 0)
            {
                dbContext.RouteAssignmentAuditEntries.RemoveRange(route.AssignmentAuditTrail.ToList());
                route.AssignmentAuditTrail.Clear();
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
                    RouteId = route.Id,
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
                stop.Parcels.Add(parcel);
                route.Stops.Add(stop);
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
