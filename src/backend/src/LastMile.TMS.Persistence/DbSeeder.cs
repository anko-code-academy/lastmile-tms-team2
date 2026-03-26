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

    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (connectionString != "InMemory")
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
        if (await dbContext.Depots.AnyAsync(d => d.Id == TestDepotId, ct))
        {
            logger.LogDebug("Test depot already exists — skipping seed");
            return;
        }

        var address = new Address
        {
            Id = TestDepotAddressId,
            Street1 = "1 Test Street",
            City = "Test City",
            State = "TS",
            PostalCode = "00000",
            CountryCode = "AU",
            IsResidential = false,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder"
        };

        var depot = new Depot
        {
            Id = TestDepotId,
            Name = "Test Depot",
            AddressId = address.Id,
            Address = address,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder"
        };

        dbContext.Addresses.Add(address);
        dbContext.Depots.Add(depot);
        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Seeded test depot: {DepotId}", TestDepotId);
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
            new Coordinate(151.0, -33.0),
            new Coordinate(152.0, -33.0),
            new Coordinate(152.0, -34.0),
            new Coordinate(151.0, -34.0),
            new Coordinate(151.0, -33.0),
        };

        var zone = new Zone
        {
            Id = TestZoneId,
            Name = "Test Zone",
            Boundary = GeometryFactory.CreatePolygon(boundaryCoords),
            IsActive = true,
            DepotId = TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Seeder",
        };

        dbContext.Zones.Add(zone);
        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Seeded test zone: {ZoneId}", TestZoneId);
    }

    // ── Test parcels (shared addresses) ─────────────────────────────────────────

    private static readonly (Guid Id, string Tracking, decimal WeightKg)[] TestParcelSeeds =
    [
        (TestParcelId, "LMTESTSEED0001", 2.5m),
        (new Guid("00000000-0000-0000-0000-000000000010"), "LMTESTSEED0002", 1.2m),
        (new Guid("00000000-0000-0000-0000-000000000011"), "LMTESTSEED0003", 5.0m),
        (new Guid("00000000-0000-0000-0000-000000000012"), "LMTESTSEED0004", 0.5m),
        (new Guid("00000000-0000-0000-0000-000000000013"), "LMTESTSEED0005", 3.3m),
        (new Guid("00000000-0000-0000-0000-000000000014"), "LMTESTSEED0006", 2.0m),
        (new Guid("00000000-0000-0000-0000-000000000015"), "LMTESTSEED0007", 4.0m),
        (new Guid("00000000-0000-0000-0000-000000000016"), "LMTESTSEED0008", 1.8m),
        (new Guid("00000000-0000-0000-0000-000000000017"), "LMTESTSEED0009", 6.5m),
    ];

    private async Task SeedTestParcelsAsync(AppDbContext dbContext, CancellationToken ct)
    {
        if (!await dbContext.Zones.AnyAsync(z => z.Id == TestZoneId, ct))
        {
            logger.LogWarning("Test zone missing; cannot seed test parcels");
            return;
        }

        var now = DateTimeOffset.UtcNow;

        if (!await dbContext.Addresses.AnyAsync(a => a.Id == TestParcelShipperAddressId, ct))
        {
            dbContext.Addresses.Add(new Address
            {
                Id = TestParcelShipperAddressId,
                Street1 = "10 Shipper Lane",
                City = "Sydney",
                State = "NSW",
                PostalCode = "2000",
                CountryCode = "AU",
                IsResidential = true,
                CreatedAt = now,
                CreatedBy = "Seeder",
            });

            dbContext.Addresses.Add(new Address
            {
                Id = TestParcelRecipientAddressId,
                Street1 = "99 Recipient Rd",
                City = "Sydney",
                State = "NSW",
                PostalCode = "2001",
                CountryCode = "AU",
                IsResidential = false,
                CreatedAt = now,
                CreatedBy = "Seeder",
            });

            await dbContext.SaveChangesAsync(ct);
        }

        var added = 0;
        foreach (var (id, tracking, weightKg) in TestParcelSeeds)
        {
            if (await dbContext.Parcels.AnyAsync(p => p.Id == id, ct))
                continue;

            dbContext.Parcels.Add(new Parcel
            {
                Id = id,
                TrackingNumber = tracking,
                Description = "Seeded test parcel for development",
                ServiceType = ServiceType.Standard,
                Status = ParcelStatus.Sorted,
                ShipperAddressId = TestParcelShipperAddressId,
                RecipientAddressId = TestParcelRecipientAddressId,
                Weight = weightKg,
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
}
