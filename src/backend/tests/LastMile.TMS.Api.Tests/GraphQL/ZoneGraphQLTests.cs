using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Persistence;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class ZoneGraphQLTests(CustomWebApplicationFactory factory)
    : GraphQLTestBase(factory), IAsyncLifetime
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    [Fact]
    public async Task Zones_WithAdminToken_ReturnsBoundaryGeoJson()
    {
        var zoneId = await SeedZoneAsync();
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            query ($id: UUID!) {
              zone(id: $id) {
                id
                name
                boundary
                boundaryGeoJson
                isActive
                depotId
                depotName
                createdAt
                updatedAt
              }
            }
            """,
            variables: new { id = zoneId },
            accessToken: token);

        var hasErrors = document.RootElement.TryGetProperty("errors", out var errors);
        if (hasErrors)
        {
            // Dump full error details for diagnosis
            var errorText = errors[0].GetRawText();
            Assert.Fail($"GraphQL errors: {errorText}");
        }

        var zone = document.RootElement
            .GetProperty("data")
            .GetProperty("zone");

        zone.GetProperty("id").GetString().Should().Be(zoneId.ToString());
        zone.GetProperty("boundary").GetString().Should().StartWith("POLYGON");

        using var boundaryGeoJson = JsonDocument.Parse(zone.GetProperty("boundaryGeoJson").GetString()!);
        boundaryGeoJson.RootElement.GetProperty("type").GetString().Should().Be("Polygon");
        boundaryGeoJson.RootElement.GetProperty("coordinates")[0].GetArrayLength().Should().BeGreaterThanOrEqualTo(4);
    }

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedZoneAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var depot = new Depot
        {
            Name = $"Zone Depot {Guid.NewGuid():N}",
            Address = new Address
            {
                Street1 = "200 Collins Street",
                City = "Melbourne",
                State = "VIC",
                PostalCode = "3000",
                CountryCode = "AU",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "tests"
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        var boundary = GeometryFactory.CreatePolygon(
            [
                new Coordinate(144.95, -37.82),
                new Coordinate(144.98, -37.82),
                new Coordinate(144.98, -37.79),
                new Coordinate(144.95, -37.79),
                new Coordinate(144.95, -37.82),
            ]);
        boundary.SRID = 4326;

        var zone = new Zone
        {
            Name = $"Zone GraphQL {Guid.NewGuid():N}",
            Boundary = boundary,
            IsActive = true,
            Depot = depot,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Zones.Add(zone);
        await dbContext.SaveChangesAsync();

        return zone.Id;
    }
}
