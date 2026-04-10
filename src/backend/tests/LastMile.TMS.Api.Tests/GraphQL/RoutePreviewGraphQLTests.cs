using FluentAssertions;
using LastMile.TMS.Persistence;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class RoutePreviewGraphQLTests : GraphQLTestBase, IAsyncLifetime
{
    public RoutePreviewGraphQLTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RoutePlanPreview_IncludesDepotAndReturnsPathBackToDepot()
    {
        var token = await GetAdminAccessTokenAsync();
        var startDate = DateTimeOffset.UtcNow.AddHours(2);

        using var document = await PostGraphQLAsync(
            """
            query PreviewRoute($input: RoutePlanPreviewInput!) {
              routePlanPreview(input: $input) {
                depotId
                depotName
                depotAddressLine
                depotLongitude
                depotLatitude
                path {
                  longitude
                  latitude
                }
                stops {
                  sequence
                }
              }
            }
            """,
            new
            {
                input = new
                {
                    zoneId = DbSeeder.TestZoneId,
                    vehicleId = DbSeeder.TestVehicleId,
                    driverId = DbSeeder.TestDriverId,
                    startDate,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { DbSeeder.TestParcelId },
                    stops = Array.Empty<object>(),
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var preview = document.RootElement
            .GetProperty("data")
            .GetProperty("routePlanPreview");

        preview.GetProperty("depotId").GetString().Should().Be(DbSeeder.TestDepotId.ToString());
        preview.GetProperty("depotName").GetString().Should().Be("Test Depot");
        preview.GetProperty("depotAddressLine").GetString().Should().NotBeNullOrWhiteSpace();

        var depotLongitude = preview.GetProperty("depotLongitude").GetDouble();
        var depotLatitude = preview.GetProperty("depotLatitude").GetDouble();
        var path = preview.GetProperty("path").EnumerateArray().ToList();

        path.Should().NotBeEmpty();
        path[0].GetProperty("longitude").GetDouble().Should().BeApproximately(depotLongitude, 0.000001d);
        path[0].GetProperty("latitude").GetDouble().Should().BeApproximately(depotLatitude, 0.000001d);
        path[^1].GetProperty("longitude").GetDouble().Should().BeApproximately(depotLongitude, 0.000001d);
        path[^1].GetProperty("latitude").GetDouble().Should().BeApproximately(depotLatitude, 0.000001d);
        preview.GetProperty("stops").GetArrayLength().Should().BeGreaterThan(0);
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
