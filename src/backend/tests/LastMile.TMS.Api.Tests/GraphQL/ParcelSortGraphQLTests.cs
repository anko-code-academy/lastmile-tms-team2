using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class ParcelSortGraphQLTests(CustomWebApplicationFactory factory)
    : GraphQLTestBase(factory), IAsyncLifetime
{
    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ParcelSortInstruction_ReturnsCanSortAndBins_ForReceivedParcel()
    {
        var (parcelId, trackingNumber, binId) = await SeedSortScenarioAsync();
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            query SortInstruction($trackingNumber: String!, $depotId: UUID) {
              parcelSortInstruction(trackingNumber: $trackingNumber, depotId: $depotId) {
                parcelId
                trackingNumber
                status
                canSort
                deliveryZoneName
                depotName
                recommendedBinLocationId
                targetBins {
                  binLocationId
                  name
                  storagePath
                  isRecommended
                }
                blockReasonCode
              }
            }
            """,
            variables: new
            {
                trackingNumber,
                depotId = (string?)null,
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("GraphQL errors: {0}", errors.ToString());

        var row = document.RootElement.GetProperty("data").GetProperty("parcelSortInstruction");
        row.GetProperty("parcelId").GetString().Should().Be(parcelId.ToString());
        row.GetProperty("canSort").GetBoolean().Should().BeTrue();
        row.GetProperty("recommendedBinLocationId").GetString().Should().Be(binId.ToString());
        row.GetProperty("targetBins").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ConfirmParcelSort_UpdatesStatusToSorted()
    {
        var (parcelId, trackingNumber, binId) = await SeedSortScenarioAsync();
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation Confirm($input: ConfirmParcelSortInput!) {
              confirmParcelSort(input: $input) {
                id
                status
                trackingNumber
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId = parcelId.ToString(),
                    binLocationId = binId.ToString(),
                },
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("GraphQL errors: {0}", errors.ToString());

        var result = document.RootElement.GetProperty("data").GetProperty("confirmParcelSort");
        result.GetProperty("status").GetString().Should().Be("Sorted");
        result.GetProperty("trackingNumber").GetString().Should().Be(trackingNumber);
    }

    [Fact]
    public async Task ConfirmParcelSort_WrongZoneBin_ReturnsError()
    {
        var (parcelId, _, correctBinId) = await SeedSortScenarioAsync();
        var wrongBinId = await SeedWrongZoneBinAsync();
        wrongBinId.Should().NotBe(correctBinId);

        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation Confirm($input: ConfirmParcelSortInput!) {
              confirmParcelSort(input: $input) {
                id
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId = parcelId.ToString(),
                    binLocationId = wrongBinId.ToString(),
                },
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        var message = errors[0].GetProperty("message").GetString() ?? "";
        message.Should().Contain("Mis-sort");
    }

    private async Task<(Guid ParcelId, string TrackingNumber, Guid BinId)> SeedSortScenarioAsync()
    {
        await Factory.ResetDatabaseAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureTestParcelAddressesAsync(db);

        var trackingNumber = $"LMSORT{Guid.NewGuid():N}";
        var parcelId = await SeedParcelInternalAsync(db, trackingNumber, ParcelStatus.ReceivedAtDepot);
        var binId = await EnsureSortBinForTestZoneAsync(db);

        return (parcelId, trackingNumber, binId);
    }

    /// <summary>
    /// Seeder may skip PostGIS-backed data if connection string matched InMemory at seed time.
    /// Ensure the well-known parcel address rows exist before inserting parcels in this fixture.
    /// </summary>
    private static async Task EnsureTestParcelAddressesAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        if (!await db.Addresses.AnyAsync(a => a.Id == DbSeeder.TestParcelShipperAddressId))
        {
            db.Addresses.Add(
                new Address
                {
                    Id = DbSeeder.TestParcelShipperAddressId,
                    Street1 = "10 Shipper Lane",
                    City = "Sydney",
                    State = "NSW",
                    PostalCode = "2000",
                    CountryCode = "AU",
                    IsResidential = true,
                    CreatedAt = now,
                    CreatedBy = "ParcelSortGraphQLTests",
                });
        }

        if (!await db.Addresses.AnyAsync(a => a.Id == DbSeeder.TestParcelRecipientAddressId))
        {
            db.Addresses.Add(
                new Address
                {
                    Id = DbSeeder.TestParcelRecipientAddressId,
                    Street1 = "99 Recipient Rd",
                    City = "Sydney",
                    State = "NSW",
                    PostalCode = "2001",
                    CountryCode = "AU",
                    IsResidential = false,
                    CreatedAt = now,
                    CreatedBy = "ParcelSortGraphQLTests",
                });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Guid> SeedParcelInternalAsync(
        AppDbContext db,
        string trackingNumber,
        ParcelStatus status)
    {
        var parcel = new Parcel
        {
            TrackingNumber = trackingNumber,
            Description = "GraphQL sort test parcel",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddressId = DbSeeder.TestParcelShipperAddressId,
            RecipientAddressId = DbSeeder.TestParcelRecipientAddressId,
            Weight = 1m,
            WeightUnit = WeightUnit.Kg,
            Length = 10m,
            Width = 10m,
            Height = 10m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 10m,
            Currency = "USD",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(1),
            DeliveryAttempts = 0,
            ZoneId = DbSeeder.TestZoneId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        db.Parcels.Add(parcel);
        await db.SaveChangesAsync();
        return parcel.Id;
    }

    private static async Task<Guid> EnsureSortBinForTestZoneAsync(AppDbContext db)
    {
        var existing = await db.BinLocations
            .AsNoTracking()
            .Where(b =>
                b.IsActive
                && b.DeliveryZoneId == DbSeeder.TestZoneId)
            .Select(b => b.Id)
            .FirstOrDefaultAsync();

        if (existing != default)
        {
            return existing;
        }

        var zone = await db.Zones.SingleAsync(z => z.Id == DbSeeder.TestZoneId);

        var storageZone = new StorageZone
        {
            Name = "Sort Test Storage",
            NormalizedName = "SORT TEST STORAGE",
            DepotId = DbSeeder.TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        var aisle = new StorageAisle
        {
            Name = "Sort Aisle",
            NormalizedName = "SORT AISLE",
            StorageZone = storageZone,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        var bin = new BinLocation
        {
            Name = "Sort Bin GQL",
            NormalizedName = "SORT BIN GQL",
            IsActive = true,
            DeliveryZoneId = DbSeeder.TestZoneId,
            DeliveryZone = zone,
            StorageAisle = aisle,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        db.BinLocations.Add(bin);
        await db.SaveChangesAsync();
        return bin.Id;
    }

    [Fact]
    public async Task ParcelSortInstruction_RegisteredParcel_ReturnsCanSortFalseWithWrongStatus()
    {
        await Factory.ResetDatabaseAsync();
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureTestParcelAddressesAsync(db);

        var trackingNumber = $"LMSORT-REG{Guid.NewGuid():N}";
        await SeedParcelInternalAsync(db, trackingNumber, ParcelStatus.Registered);

        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            query SortInstruction($trackingNumber: String!, $depotId: UUID) {
              parcelSortInstruction(trackingNumber: $trackingNumber, depotId: $depotId) {
                canSort
                blockReasonCode
                blockReasonMessage
              }
            }
            """,
            variables: new
            {
                trackingNumber,
                depotId = (string?)null,
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("GraphQL errors: {0}", errors.ToString());

        var row = document.RootElement.GetProperty("data").GetProperty("parcelSortInstruction");
        row.GetProperty("canSort").GetBoolean().Should().BeFalse();
        row.GetProperty("blockReasonCode").GetString().Should().Be("WRONG_STATUS");
    }

    private async Task<Guid> SeedWrongZoneBinAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await EnsureTestParcelAddressesAsync(db);

        var templateZone = await db.Zones
            .AsNoTracking()
            .SingleAsync(z => z.Id == DbSeeder.TestZoneId);

        var otherZone = new Zone
        {
            Name = "Sort GraphQL Other Zone",
            Boundary = (NetTopologySuite.Geometries.Polygon)templateZone.Boundary.Copy(),
            DepotId = DbSeeder.TestDepotId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        db.Zones.Add(otherZone);
        await db.SaveChangesAsync();

        var storageZone = new StorageZone
        {
            Name = "Sort Other Storage",
            NormalizedName = "SORT OTHER STORAGE",
            DepotId = DbSeeder.TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        var aisle = new StorageAisle
        {
            Name = "Other Aisle",
            NormalizedName = "OTHER AISLE",
            StorageZone = storageZone,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        var bin = new BinLocation
        {
            Name = "Wrong Zone Bin",
            NormalizedName = "WRONG ZONE BIN",
            IsActive = true,
            DeliveryZoneId = otherZone.Id,
            DeliveryZone = otherZone,
            StorageAisle = aisle,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        db.BinLocations.Add(bin);
        await db.SaveChangesAsync();
        return bin.Id;
    }
}
