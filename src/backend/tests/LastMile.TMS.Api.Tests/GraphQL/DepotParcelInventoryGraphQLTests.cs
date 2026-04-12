using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class DepotParcelInventoryGraphQLTests(CustomWebApplicationFactory factory)
    : GraphQLTestBase(factory), IAsyncLifetime
{
    [Fact]
    public async Task DepotParcelInventory_ReturnsAssignedDepotSummary()
    {
        var fixture = await SeedInventoryDepotAsync();
        var zoneTwoId = await SeedZoneAsync(fixture.DepotId, "Inventory Zone Two");

        await SeedInventoryParcelAsync(fixture.PrimaryZoneId, "LM-DASH-GQL-001", ParcelStatus.ReceivedAtDepot, createdHoursAgo: 7, lastModifiedHoursAgo: 7);
        await SeedInventoryParcelAsync(fixture.PrimaryZoneId, "LM-DASH-GQL-002", ParcelStatus.Sorted, createdHoursAgo: 3, lastModifiedHoursAgo: 3);
        await SeedInventoryParcelAsync(zoneTwoId, "LM-DASH-GQL-003", ParcelStatus.Loaded, createdHoursAgo: 8, lastModifiedHoursAgo: 5);
        await SeedInventoryParcelAsync(zoneTwoId, "LM-DASH-GQL-004", ParcelStatus.Exception, createdHoursAgo: 6, lastModifiedHoursAgo: null);
        await SeedInventoryParcelAsync(fixture.PrimaryZoneId, "LM-DASH-GQL-005", ParcelStatus.Delivered, createdHoursAgo: 10, lastModifiedHoursAgo: 10);

        var token = await GetAccessTokenAsync(fixture.OperatorEmail, "Warehouse@12345");

        using var document = await PostGraphQLAsync(
            """
            query DepotParcelInventory($agingThresholdMinutes: Int!) {
              depotParcelInventory(agingThresholdMinutes: $agingThresholdMinutes) {
                depotName
                statusCounts {
                  status
                  count
                }
                zoneCounts {
                  zoneName
                  count
                }
                agingAlert {
                  thresholdMinutes
                  count
                }
              }
            }
            """,
            variables: new
            {
                agingThresholdMinutes = 240,
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("GraphQL errors: {0}", errors.ToString());

        var summary = document.RootElement
            .GetProperty("data")
            .GetProperty("depotParcelInventory");

        summary.GetProperty("depotName").GetString().Should().Be(fixture.DepotName);
        summary.GetProperty("statusCounts").EnumerateArray()
            .Select(node => (
                node.GetProperty("status").GetString(),
                node.GetProperty("count").GetInt32()))
            .Should().Equal(
                ("RECEIVED_AT_DEPOT", 1),
                ("SORTED", 1),
                ("STAGED", 0),
                ("LOADED", 1),
                ("EXCEPTION", 1));
        summary.GetProperty("zoneCounts").EnumerateArray()
            .Select(node => (
                node.GetProperty("zoneName").GetString(),
                node.GetProperty("count").GetInt32()))
            .Should().Equal(
                (fixture.PrimaryZoneName, 2),
                ("Inventory Zone Two", 2));
        summary.GetProperty("agingAlert").GetProperty("thresholdMinutes").GetInt32().Should().Be(240);
        summary.GetProperty("agingAlert").GetProperty("count").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task DepotParcelInventoryParcels_RespectsStatusZoneAndAgingFilters()
    {
        var fixture = await SeedInventoryDepotAsync();
        var zoneTwoId = await SeedZoneAsync(fixture.DepotId, "Inventory Zone Drilldown");

        await SeedInventoryParcelAsync(fixture.PrimaryZoneId, "LM-DASH-DRILL-001", ParcelStatus.ReceivedAtDepot, createdHoursAgo: 7, lastModifiedHoursAgo: 7);
        await SeedInventoryParcelAsync(zoneTwoId, "LM-DASH-DRILL-002", ParcelStatus.ReceivedAtDepot, createdHoursAgo: 2, lastModifiedHoursAgo: 2);
        await SeedInventoryParcelAsync(zoneTwoId, "LM-DASH-DRILL-003", ParcelStatus.Exception, createdHoursAgo: 9, lastModifiedHoursAgo: null);

        var token = await GetAccessTokenAsync(fixture.OperatorEmail, "Warehouse@12345");

        using var statusDocument = await PostGraphQLAsync(
            """
            query DepotParcelInventoryParcels(
              $agingThresholdMinutes: Int!
              $status: ParcelStatus
              $zoneId: UUID
              $agingOnly: Boolean!
              $first: Int!
              $after: String
            ) {
              depotParcelInventoryParcels(
                agingThresholdMinutes: $agingThresholdMinutes
                status: $status
                zoneId: $zoneId
                agingOnly: $agingOnly
                first: $first
                after: $after
              ) {
                totalCount
                nodes {
                  trackingNumber
                  status
                  zoneName
                }
              }
            }
            """,
            variables: new
            {
                agingThresholdMinutes = 240,
                status = "RECEIVED_AT_DEPOT",
                zoneId = zoneTwoId.ToString(),
                agingOnly = false,
                first = 10,
                after = (string?)null,
            },
            accessToken: token);

        statusDocument.RootElement.TryGetProperty("errors", out var statusErrors)
            .Should().BeFalse("GraphQL errors: {0}", statusErrors.ToString());

        var filtered = statusDocument.RootElement
            .GetProperty("data")
            .GetProperty("depotParcelInventoryParcels");

        filtered.GetProperty("totalCount").GetInt32().Should().Be(1);
        filtered.GetProperty("nodes").EnumerateArray()
            .Select(node => node.GetProperty("trackingNumber").GetString())
            .Should().Equal("LM-DASH-DRILL-002");

        using var agingDocument = await PostGraphQLAsync(
            """
            query DepotParcelInventoryParcels(
              $agingThresholdMinutes: Int!
              $status: ParcelStatus
              $zoneId: UUID
              $agingOnly: Boolean!
              $first: Int!
              $after: String
            ) {
              depotParcelInventoryParcels(
                agingThresholdMinutes: $agingThresholdMinutes
                status: $status
                zoneId: $zoneId
                agingOnly: $agingOnly
                first: $first
                after: $after
              ) {
                totalCount
                pageInfo {
                  hasNextPage
                  endCursor
                }
                nodes {
                  trackingNumber
                  status
                  ageMinutes
                }
              }
            }
            """,
            variables: new
            {
                agingThresholdMinutes = 240,
                status = (string?)null,
                zoneId = zoneTwoId.ToString(),
                agingOnly = true,
                first = 10,
                after = (string?)null,
            },
            accessToken: token);

        agingDocument.RootElement.TryGetProperty("errors", out var agingErrors)
            .Should().BeFalse("GraphQL errors: {0}", agingErrors.ToString());

        var aging = agingDocument.RootElement
            .GetProperty("data")
            .GetProperty("depotParcelInventoryParcels");

        aging.GetProperty("totalCount").GetInt32().Should().Be(1);
        var agingNode = aging.GetProperty("nodes").EnumerateArray().Single();
        agingNode.GetProperty("trackingNumber").GetString().Should().Be("LM-DASH-DRILL-003");
        agingNode.GetProperty("status").GetString().Should().Be("EXCEPTION");
        agingNode.GetProperty("ageMinutes").GetInt32().Should().BeGreaterThan(240);
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<InventoryDepotFixture> SeedInventoryDepotAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var depotId = Guid.NewGuid();
        var depotName = $"Inventory Depot {depotId.ToString("N")[..6]}";
        const string primaryZoneName = "Inventory Zone One";

        var templateZone = await dbContext.Zones
            .AsNoTracking()
            .SingleAsync(zone => zone.Id == DbSeeder.TestZoneId);

        var address = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "100 Inventory Way",
            City = "Chicago",
            State = "IL",
            PostalCode = "60654",
            CountryCode = "US",
            IsResidential = false,
            CompanyName = depotName,
            CreatedAt = now,
            CreatedBy = "tests",
        };

        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = primaryZoneName,
            Boundary = (NetTopologySuite.Geometries.Polygon)templateZone.Boundary.Copy(),
            DepotId = depotId,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "tests",
        };

        var depot = new Depot
        {
            Id = depotId,
            Name = depotName,
            AddressId = address.Id,
            Address = address,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "tests",
            Zones = [zone],
        };

        dbContext.Depots.Add(depot);
        await dbContext.SaveChangesAsync();

        var operatorEmail = await SeedWarehouseOperatorAsync(depotId, zone.Id);
        return new InventoryDepotFixture(operatorEmail, depotId, depotName, zone.Id, primaryZoneName);
    }

    private async Task<string> SeedWarehouseOperatorAsync(Guid depotId, Guid zoneId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"warehouse.inventory.{Guid.NewGuid():N}@lastmile.local";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "Warehouse",
            LastName = "Operator",
            DepotId = depotId,
            ZoneId = zoneId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        var createResult = await userManager.CreateAsync(user, "Warehouse@12345");
        createResult.Succeeded.Should().BeTrue(string.Join(", ", createResult.Errors.Select(error => error.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, PredefinedRole.WarehouseOperator.ToString());
        roleResult.Succeeded.Should().BeTrue(string.Join(", ", roleResult.Errors.Select(error => error.Description)));

        return email;
    }

    private async Task<Guid> SeedZoneAsync(Guid depotId, string name)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var templateZone = await dbContext.Zones
            .AsNoTracking()
            .SingleAsync(zone => zone.Id == DbSeeder.TestZoneId);

        var zone = new Zone
        {
            Name = name,
            Boundary = (NetTopologySuite.Geometries.Polygon)templateZone.Boundary.Copy(),
            DepotId = depotId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        dbContext.Zones.Add(zone);
        await dbContext.SaveChangesAsync();
        return zone.Id;
    }

    private async Task<Guid> SeedInventoryParcelAsync(
        Guid zoneId,
        string trackingNumber,
        ParcelStatus status,
        int createdHoursAgo,
        int? lastModifiedHoursAgo)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = new Parcel
        {
            TrackingNumber = trackingNumber,
            Description = "Depot inventory GraphQL test parcel",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddressId = DbSeeder.TestParcelShipperAddressId,
            RecipientAddressId = DbSeeder.TestParcelRecipientAddressId,
            Weight = 2.5m,
            WeightUnit = WeightUnit.Kg,
            Length = 30m,
            Width = 20m,
            Height = 10m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 100m,
            Currency = "USD",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2),
            DeliveryAttempts = 0,
            ZoneId = zoneId,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-createdHoursAgo),
            CreatedBy = "tests",
            LastModifiedAt = lastModifiedHoursAgo.HasValue
                ? DateTimeOffset.UtcNow.AddHours(-lastModifiedHoursAgo.Value)
                : null,
            LastModifiedBy = lastModifiedHoursAgo.HasValue ? "tests" : null,
        };

        dbContext.Parcels.Add(parcel);
        await dbContext.SaveChangesAsync();
        return parcel.Id;
    }

    private sealed record InventoryDepotFixture(
        string OperatorEmail,
        Guid DepotId,
        string DepotName,
        Guid PrimaryZoneId,
        string PrimaryZoneName);
}
