using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Infrastructure.Options;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Api.Controllers;

[ApiController]
[Route("api/test-support/user-management")]
public sealed class TestSupportController(
    AppDbContext dbContext,
    RoleManager<ApplicationRole> roleManager,
    IOptions<TestingOptions> testingOptions,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    private const string FixtureDepotName = "E2E Depot";
    private const string FixtureZoneName = "E2E Zone";

    [HttpPost("reset-and-seed")]
    public async Task<ActionResult<TestSupportFixtureResponse>> ResetAndSeedAsync(
        CancellationToken cancellationToken)
    {
        if (!IsTestSupportEnabled())
        {
            return NotFound();
        }

        if (!HasValidSupportKey())
        {
            return Unauthorized();
        }

        var fixtureSuffix = Guid.NewGuid().ToString("N")[..8];
        var depotName = $"{FixtureDepotName} {fixtureSuffix}";
        var zoneName = $"{FixtureZoneName} {fixtureSuffix}";

        await SeedFixtureAsync(depotName, zoneName, cancellationToken);

        return Ok(new TestSupportFixtureResponse(
            configuration["AdminCredentials:Email"] ?? "admin@lastmile.com",
            configuration["AdminCredentials:Password"] ?? "Admin@12345",
            depotName,
            zoneName));
    }

    private async Task SeedFixtureAsync(
        string depotName,
        string zoneName,
        CancellationToken cancellationToken)
    {
        if (!await roleManager.RoleExistsAsync(nameof(PredefinedRole.Admin)))
        {
            throw new InvalidOperationException("Admin role must exist before seeding test support fixtures.");
        }

        var address = new Address
        {
            Street1 = "1 E2E Street",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-support"
        };

        var depot = new Depot
        {
            Name = depotName,
            Address = address,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-support"
        };

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var zone = new Zone
        {
            Name = zoneName,
            Depot = depot,
            Boundary = geometryFactory.CreatePolygon([
                new Coordinate(151.0, -34.0),
                new Coordinate(151.0, -33.0),
                new Coordinate(152.0, -33.0),
                new Coordinate(152.0, -34.0),
                new Coordinate(151.0, -34.0)
            ]),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-support"
        };

        dbContext.Addresses.Add(address);
        dbContext.Depots.Add(depot);
        dbContext.Zones.Add(zone);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public sealed record TestSupportFixtureResponse(
        string AdminEmail,
        string AdminPassword,
        string DepotName,
        string ZoneName);

    private bool IsTestSupportEnabled() =>
        testingOptions.Value.EnableTestSupport && !environment.IsProduction();

    private bool HasValidSupportKey()
    {
        var supportKey = testingOptions.Value.SupportKey;
        if (string.IsNullOrWhiteSpace(supportKey))
        {
            return false;
        }

        return Request.Headers.TryGetValue("X-Test-Support-Key", out var providedKey) &&
            StringComparer.Ordinal.Equals(providedKey.ToString(), supportKey);
    }
}
