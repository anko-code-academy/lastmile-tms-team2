using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class DepotGraphQLTests : GraphQLTestBase, IAsyncLifetime
{
    public DepotGraphQLTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    [Fact]
    public async Task CreateDepot_WithValidOperatingHours_CreatesDepot()
    {
        var token = await GetAdminAccessTokenAsync();
        var depotName = $"Depot GraphQL Create {Guid.NewGuid():N}";

        using var document = await PostGraphQLAsync(
            """
            mutation CreateDepot($input: CreateDepotInput!) {
              createDepot(input: $input) {
                id
                name
                isActive
                address {
                  city
                  countryCode
                }
                operatingHours {
                  dayOfWeek
                  openTime
                  closedTime
                  isClosed
                }
              }
            }
            """,
            new
            {
                input = new
                {
                    name = depotName,
                    isActive = true,
                    address = new
                    {
                        street1 = "500 Collins Street",
                        city = "Melbourne",
                        state = "VIC",
                        postalCode = "3000",
                        countryCode = "au",
                        isResidential = false
                    },
                    operatingHours = new[]
                    {
                        new
                        {
                            dayOfWeek = "MONDAY",
                            openTime = "08:00:00",
                            closedTime = "17:00:00",
                            isClosed = false
                        },
                        new
                        {
                            dayOfWeek = "TUESDAY",
                            openTime = "09:00:00",
                            closedTime = "18:00:00",
                            isClosed = false
                        }
                    }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var depot = document.RootElement
            .GetProperty("data")
            .GetProperty("createDepot");

        depot.GetProperty("name").GetString().Should().Be(depotName);
        depot.GetProperty("isActive").GetBoolean().Should().BeTrue();
        depot.GetProperty("address").GetProperty("city").GetString().Should().Be("Melbourne");
        depot.GetProperty("address").GetProperty("countryCode").GetString().Should().Be("AU");
        depot.GetProperty("operatingHours").GetArrayLength().Should().Be(2);

        var depotId = depot.GetProperty("id").GetGuid();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedDepot = await dbContext.Depots
            .Include(d => d.Address)
            .Include(d => d.OperatingHours)
            .SingleAsync(d => d.Id == depotId);

        persistedDepot.Name.Should().Be(depotName);
        persistedDepot.Address.CountryCode.Should().Be("AU");
        persistedDepot.OperatingHours.Should().HaveCount(2);
        persistedDepot.OperatingHours.Should().ContainSingle(x => x.DayOfWeek == DayOfWeek.Monday);
        persistedDepot.OperatingHours.Should().ContainSingle(x => x.DayOfWeek == DayOfWeek.Tuesday);
    }

    [Fact]
    public async Task CreateDepot_WithDuplicateOperatingHoursDays_ReturnsValidationError()
    {
        var token = await GetAdminAccessTokenAsync();
        var depotName = $"Depot GraphQL Duplicate Create {Guid.NewGuid():N}";

        using var document = await PostGraphQLAsync(
            """
            mutation CreateDepot($input: CreateDepotInput!) {
              createDepot(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    name = depotName,
                    isActive = true,
                    address = new
                    {
                        street1 = "500 Collins Street",
                        city = "Melbourne",
                        state = "VIC",
                        postalCode = "3000",
                        countryCode = "AU",
                        isResidential = false
                    },
                    operatingHours = new[]
                    {
                        new
                        {
                            dayOfWeek = "MONDAY",
                            openTime = "08:00:00",
                            closedTime = "17:00:00",
                            isClosed = false
                        },
                        new
                        {
                            dayOfWeek = "MONDAY",
                            openTime = "09:00:00",
                            closedTime = "18:00:00",
                            isClosed = false
                        }
                    }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("duplicate days");

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.Depots.AnyAsync(d => d.Name == depotName)).Should().BeFalse();
    }

    [Fact]
    public async Task Depots_WithAdminToken_ReturnsFullDepotFields()
    {
        var depotId = await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            query {
              depots {
                id
                name
                isActive
                createdAt
                updatedAt
                address {
                  street1
                  city
                  countryCode
                  geoLocation {
                    latitude
                    longitude
                  }
                }
                operatingHours {
                  openTime
                  closedTime
                  isClosed
                }
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse();

        var depots = document.RootElement
            .GetProperty("data")
            .GetProperty("depots")
            .EnumerateArray()
            .ToList();

        var depot = depots.Single(x => x.GetProperty("id").GetString() == depotId.ToString());

        depot.GetProperty("name").GetString().Should().StartWith("Depot GraphQL");
        depot.GetProperty("isActive").GetBoolean().Should().BeFalse();
        depot.GetProperty("createdAt").GetString().Should().NotBeNullOrWhiteSpace();
        depot.GetProperty("updatedAt").ValueKind.Should().Be(JsonValueKind.Null);
        depot.GetProperty("address").GetProperty("city").GetString().Should().Be("Melbourne");
        depot.GetProperty("address").GetProperty("countryCode").GetString().Should().Be("AU");
        depot.GetProperty("address").GetProperty("geoLocation").GetProperty("latitude").GetDouble().Should().BeApproximately(-37.8136, 0.0001);
        depot.GetProperty("address").GetProperty("geoLocation").GetProperty("longitude").GetDouble().Should().BeApproximately(144.9631, 0.0001);
        depot.GetProperty("operatingHours").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Depots_Projection_SelectsOnlyRequestedAddressColumns()
    {
        await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        Factory.SqlCapture.Clear();

        using var document = await PostGraphQLAsync(
            """
            query {
              depots {
                id
                name
                address {
                  city
                }
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var sql = GetCapturedSql();
        sql.Should().NotContain("SELECT TRUE");
        sql.Should().Contain("\"City\"");
        sql.Should().NotContain("\"Street1\"");
        sql.Should().NotContain("\"Street2\"");
        sql.Should().NotContain("\"State\"");
        sql.Should().NotContain("\"PostalCode\"");
        sql.Should().NotContain("\"CountryCode\"");
        sql.Should().NotContain("\"ContactName\"");
        sql.Should().NotContain("\"CompanyName\"");
        sql.Should().NotContain("\"Phone\"");
        sql.Should().NotContain("\"Email\"");
    }

    [Fact]
    public async Task Depots_Projection_WithoutAddress_DoesNotJoinAddresses()
    {
        await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        Factory.SqlCapture.Clear();

        using var document = await PostGraphQLAsync(
            """
            query {
              depots {
                id
                name
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var sql = GetCapturedSql();
        sql.Should().NotContain("SELECT TRUE");
        sql.Should().NotContain("JOIN \"Addresses\"");
        sql.Should().NotContain("\"Street1\"");
        sql.Should().NotContain("\"City\"");
        sql.Should().NotContain("\"PostalCode\"");
    }

    [Fact]
    public async Task Depots_Projection_SelectsOnlyRequestedNestedColumns()
    {
        await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        Factory.SqlCapture.Clear();

        using var document = await PostGraphQLAsync(
            """
            query {
              depots {
                id
                address {
                  city
                  postalCode
                }
                operatingHours {
                  dayOfWeek
                }
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var sql = GetCapturedSql();
        sql.Should().Contain("\"City\"");
        sql.Should().Contain("\"PostalCode\"");
        sql.Should().Contain("\"DayOfWeek\"");
        sql.Should().NotContain("\"Street1\"");
        sql.Should().NotContain("\"State\"");
        sql.Should().NotContain("\"OpenTime\"");
        sql.Should().NotContain("\"ClosedTime\"");
        sql.Should().NotContain("\"IsClosed\"");
    }

    [Fact]
    public async Task UpdateDepot_WithExistingOperatingHoursDay_UpdatesWithoutUniqueConstraintErrors()
    {
        var depotId = await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        await using var initialScope = Factory.Services.CreateAsyncScope();
        var initialDbContext = initialScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var originalHours = await initialDbContext.Depots
            .Include(d => d.OperatingHours)
            .Where(d => d.Id == depotId)
            .SelectMany(d => d.OperatingHours)
            .SingleAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateDepot($id: UUID!, $input: UpdateDepotInput!) {
              updateDepot(id: $id, input: $input) {
                id
                name
                isActive
                operatingHours {
                  dayOfWeek
                  openTime
                  closedTime
                  isClosed
                }
              }
            }
            """,
            new
            {
                id = depotId,
                input = new
                {
                    name = "Depot GraphQL Updated",
                    isActive = true,
                    operatingHours = new[]
                    {
                        new
                        {
                            dayOfWeek = "MONDAY",
                            openTime = "09:30:00",
                            closedTime = "18:15:00",
                            isClosed = false
                        }
                    }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var depot = document.RootElement
            .GetProperty("data")
            .GetProperty("updateDepot");

        depot.GetProperty("id").GetString().Should().Be(depotId.ToString());
        depot.GetProperty("name").GetString().Should().Be("Depot GraphQL Updated");
        depot.GetProperty("isActive").GetBoolean().Should().BeTrue();

        var operatingHours = depot.GetProperty("operatingHours").EnumerateArray().ToList();
        operatingHours.Should().HaveCount(1);
        operatingHours[0].GetProperty("dayOfWeek").GetString().Should().Be("MONDAY");
        operatingHours[0].GetProperty("openTime").GetString().Should().Be("PT9H30M");
        operatingHours[0].GetProperty("closedTime").GetString().Should().Be("PT18H15M");
        operatingHours[0].GetProperty("isClosed").GetBoolean().Should().BeFalse();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedDepot = await dbContext.Depots
            .Include(d => d.OperatingHours)
            .SingleAsync(d => d.Id == depotId);

        persistedDepot.OperatingHours.Should().HaveCount(1);
        var persistedHours = persistedDepot.OperatingHours.Single();
        persistedHours.Id.Should().Be(originalHours.Id);
        persistedHours.CreatedAt.Should().Be(originalHours.CreatedAt);
        persistedHours.CreatedBy.Should().Be(originalHours.CreatedBy);
        persistedHours.DayOfWeek.Should().Be(DayOfWeek.Monday);
        persistedHours.OpenTime.Should().Be(new TimeOnly(9, 30));
        persistedHours.ClosedTime.Should().Be(new TimeOnly(18, 15));
        persistedHours.IsClosed.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDepot_WithMixedOperatingHoursPayload_UpsertsAndRemovesDays()
    {
        var depotId = await SeedDepotAsync([
            CreateOperatingHoursEntity(DayOfWeek.Monday, 8, 0, 17, 0),
            CreateOperatingHoursEntity(DayOfWeek.Tuesday, 9, 0, 18, 0)
        ]);
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateDepot($id: UUID!, $input: UpdateDepotInput!) {
              updateDepot(id: $id, input: $input) {
                id
                operatingHours {
                  dayOfWeek
                  openTime
                  closedTime
                  isClosed
                }
              }
            }
            """,
            new
            {
                id = depotId,
                input = new
                {
                    name = "Depot GraphQL Mixed Update",
                    isActive = true,
                    operatingHours = new[]
                    {
                        new
                        {
                            dayOfWeek = "TUESDAY",
                            openTime = "10:15:00",
                            closedTime = "19:45:00",
                            isClosed = false
                        },
                        new
                        {
                            dayOfWeek = "WEDNESDAY",
                            openTime = "07:30:00",
                            closedTime = "16:00:00",
                            isClosed = false
                        }
                    }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedDepot = await dbContext.Depots
            .Include(d => d.OperatingHours)
            .SingleAsync(d => d.Id == depotId);

        persistedDepot.OperatingHours.Should().HaveCount(2);
        persistedDepot.OperatingHours.Should().NotContain(x => x.DayOfWeek == DayOfWeek.Monday);
        persistedDepot.OperatingHours.Should().ContainSingle(x =>
            x.DayOfWeek == DayOfWeek.Tuesday &&
            x.OpenTime == new TimeOnly(10, 15) &&
            x.ClosedTime == new TimeOnly(19, 45) &&
            !x.IsClosed);
        persistedDepot.OperatingHours.Should().ContainSingle(x =>
            x.DayOfWeek == DayOfWeek.Wednesday &&
            x.OpenTime == new TimeOnly(7, 30) &&
            x.ClosedTime == new TimeOnly(16, 0) &&
            !x.IsClosed);
    }

    [Fact]
    public async Task UpdateDepot_WithAddressInput_UpdatesAddressAndReturnsGeoLocation()
    {
        var depotId = await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateDepot($id: UUID!, $input: UpdateDepotInput!) {
              updateDepot(id: $id, input: $input) {
                id
                name
                isActive
                address {
                  street1
                  city
                  state
                  postalCode
                  countryCode
                  geoLocation {
                    latitude
                    longitude
                  }
                }
              }
            }
            """,
            new
            {
                id = depotId,
                input = new
                {
                    name = "Depot GraphQL Address Update",
                    isActive = true,
                    address = new
                    {
                        street1 = "123 Market Street",
                        city = "Sydney",
                        state = "NSW",
                        postalCode = "2000",
                        countryCode = "au",
                        isResidential = false
                    }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var depot = document.RootElement
            .GetProperty("data")
            .GetProperty("updateDepot");

        depot.GetProperty("id").GetString().Should().Be(depotId.ToString());
        depot.GetProperty("name").GetString().Should().Be("Depot GraphQL Address Update");
        depot.GetProperty("isActive").GetBoolean().Should().BeTrue();
        depot.GetProperty("address").GetProperty("street1").GetString().Should().Be("123 Market Street");
        depot.GetProperty("address").GetProperty("city").GetString().Should().Be("Sydney");
        depot.GetProperty("address").GetProperty("state").GetString().Should().Be("NSW");
        depot.GetProperty("address").GetProperty("postalCode").GetString().Should().Be("2000");
        depot.GetProperty("address").GetProperty("countryCode").GetString().Should().Be("AU");
        depot.GetProperty("address").GetProperty("geoLocation").GetProperty("latitude").GetDouble().Should().BeApproximately(-33.5, 0.0001);
        depot.GetProperty("address").GetProperty("geoLocation").GetProperty("longitude").GetDouble().Should().BeApproximately(151.5, 0.0001);
    }

    [Fact]
    public async Task UpdateDepot_WithIsoDurationOperatingHoursInput_AcceptsTimeSpanPayload()
    {
        var depotId = await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateDepot($id: UUID!, $input: UpdateDepotInput!) {
              updateDepot(id: $id, input: $input) {
                id
                operatingHours {
                  dayOfWeek
                  openTime
                  closedTime
                  isClosed
                }
              }
            }
            """,
            new
            {
                id = depotId,
                input = new
                {
                    name = "Depot GraphQL ISO Duration Update",
                    isActive = true,
                    operatingHours = new[]
                    {
                        new
                        {
                            dayOfWeek = "MONDAY",
                            openTime = "PT8H",
                            closedTime = "PT17H30M",
                            isClosed = false
                        }
                    }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedDepot = await dbContext.Depots
            .Include(d => d.OperatingHours)
            .SingleAsync(d => d.Id == depotId);

        persistedDepot.OperatingHours.Should().ContainSingle(x =>
            x.DayOfWeek == DayOfWeek.Monday &&
            x.OpenTime == new TimeOnly(8, 0) &&
            x.ClosedTime == new TimeOnly(17, 30) &&
            !x.IsClosed);
    }

    [Fact]
    public async Task UpdateDepot_WithDuplicateOperatingHoursDays_ReturnsValidationError()
    {
        var depotId = await SeedDepotAsync();
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateDepot($id: UUID!, $input: UpdateDepotInput!) {
              updateDepot(id: $id, input: $input) {
                id
              }
            }
            """,
            new
            {
                id = depotId,
                input = new
                {
                    name = "Depot GraphQL Duplicate Hours",
                    isActive = true,
                    operatingHours = new[]
                    {
                        new
                        {
                            dayOfWeek = "MONDAY",
                            openTime = "08:00:00",
                            closedTime = "17:00:00",
                            isClosed = false
                        },
                        new
                        {
                            dayOfWeek = "MONDAY",
                            openTime = "09:00:00",
                            closedTime = "18:00:00",
                            isClosed = false
                        }
                    }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("duplicate days");
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private string GetCapturedSql()
    {
        Factory.SqlCapture.Commands.Should().NotBeEmpty();
        return string.Join(Environment.NewLine + Environment.NewLine, Factory.SqlCapture.Commands);
    }

    private async Task<Guid> SeedDepotAsync(IEnumerable<OperatingHours>? operatingHours = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var depot = new Depot
        {
            Name = $"Depot GraphQL {Guid.NewGuid():N}",
            IsActive = false,
            Address = new Address
            {
                Street1 = "101 Market Street",
                City = "Melbourne",
                State = "VIC",
                PostalCode = "3000",
                CountryCode = "AU",
                GeoLocation = GeometryFactory.CreatePoint(new Coordinate(144.9631, -37.8136)),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "tests"
            },
            OperatingHours = operatingHours?.ToList() ??
            [
                CreateOperatingHoursEntity(DayOfWeek.Monday, 8, 0, 17, 0)
            ],
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Depots.Add(depot);
        await dbContext.SaveChangesAsync();

        return depot.Id;
    }

    private static OperatingHours CreateOperatingHoursEntity(
        DayOfWeek dayOfWeek,
        int openHour,
        int openMinute,
        int closeHour,
        int closeMinute) =>
        new()
        {
            DayOfWeek = dayOfWeek,
            OpenTime = new TimeOnly(openHour, openMinute),
            ClosedTime = new TimeOnly(closeHour, closeMinute),
            IsClosed = false,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };
}
