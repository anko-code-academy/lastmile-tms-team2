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
public class DepotGraphQLTests(CustomWebApplicationFactory factory)
    : GraphQLTestBase(factory), IAsyncLifetime
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

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

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private string GetCapturedSql()
    {
        Factory.SqlCapture.Commands.Should().NotBeEmpty();
        return string.Join(Environment.NewLine + Environment.NewLine, Factory.SqlCapture.Commands);
    }

    private async Task<Guid> SeedDepotAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
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
            OperatingHours =
            [
                new OperatingHours
                {
                    DayOfWeek = DayOfWeek.Monday,
                    OpenTime = new TimeOnly(8, 0),
                    ClosedTime = new TimeOnly(17, 0),
                    IsClosed = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "tests"
                }
            ],
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Depots.Add(depot);
        await dbContext.SaveChangesAsync();

        return depot.Id;
    }
}
