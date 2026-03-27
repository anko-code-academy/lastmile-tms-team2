using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests.GraphQL;

public class RouteGraphQLTests(CustomWebApplicationFactory factory)
    : GraphQLTestBase(factory), IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    [Fact]
    public async Task Routes_WithoutToken_ReturnsAuthorizationError()
    {
        using var document = await PostGraphQLAsync(
            """
            query {
              routes {
                id
              }
            }
            """);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("authorized");
    }

    [Fact]
    public async Task CreateRoute_WithValidInput_ReturnsRouteAndUpdatesVehicleState()
    {
        var token = await GetAdminAccessTokenAsync();
        var startDate = DateTimeOffset.UtcNow.AddHours(2);

        using var document = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteDtoInput!) {
              createRoute(input: $input) {
                id
                vehicleId
                vehiclePlate
                driverId
                status
                parcelCount
                parcelsDelivered
                startDate
              }
            }
            """,
            new
            {
                input = new
                {
                    vehicleId = DbSeeder.TestVehicleId,
                    driverId = DbSeeder.TestDriverId,
                    startDate,
                    startMileage = 250,
                    parcelIds = new[] { DbSeeder.TestParcelId }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("createRoute");

        var routeId = route.GetProperty("id").GetGuid();
        route.GetProperty("vehicleId").GetString().Should().Be(DbSeeder.TestVehicleId.ToString());
        route.GetProperty("driverId").GetString().Should().Be(DbSeeder.TestDriverId.ToString());
        route.GetProperty("status").GetString().Should().Be("PLANNED");
        route.GetProperty("parcelCount").GetInt32().Should().Be(1);
        route.GetProperty("parcelsDelivered").GetInt32().Should().Be(0);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vehicle = await dbContext.Vehicles.FindAsync(DbSeeder.TestVehicleId);
        vehicle.Should().NotBeNull();
        vehicle!.Status.Should().Be(VehicleStatus.InUse);

        var parcel = await dbContext.Parcels.FindAsync(DbSeeder.TestParcelId);
        parcel.Should().NotBeNull();
        parcel!.Status.Should().Be(ParcelStatus.Loaded);

        var persistedRoute = await dbContext.Routes
            .Include(r => r.Parcels)
            .SingleAsync(r => r.Id == routeId);
        persistedRoute.Status.Should().Be(RouteStatus.Planned);
        persistedRoute.ParcelCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateRoute_WithUnavailableVehicle_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();
        await SetVehicleStatusAsync(DbSeeder.TestVehicleId, VehicleStatus.InUse);

        using var document = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteDtoInput!) {
              createRoute(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    vehicleId = DbSeeder.TestVehicleId,
                    driverId = DbSeeder.TestDriverId,
                    startDate = DateTimeOffset.UtcNow.AddHours(1),
                    startMileage = 100,
                    parcelIds = new[] { DbSeeder.TestParcelId }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("Vehicle is not available");
    }

    [Fact]
    public async Task CreateRoute_WithMissingParcels_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteDtoInput!) {
              createRoute(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    vehicleId = DbSeeder.TestVehicleId,
                    driverId = DbSeeder.TestDriverId,
                    startDate = DateTimeOffset.UtcNow.AddHours(1),
                    startMileage = 100,
                    parcelIds = new[] { Guid.NewGuid() }
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("One or more parcels not found");
    }

    [Fact]
    public async Task GetRoutes_WithStatusFilter_ReturnsOnlyMatchingRoutes()
    {
        var token = await GetAdminAccessTokenAsync();
        var availableVehicleId = DbSeeder.TestVehicleId;
        var completedVehicleId = await SeedVehicleAsync($"ROUTE-{Guid.NewGuid():N}"[..20]);
        var plannedRouteId = await SeedRouteAsync(availableVehicleId, RouteStatus.Planned, startMileage: 0);
        var completedRouteId = await SeedRouteAsync(completedVehicleId, RouteStatus.Completed, startMileage: 10, endMileage: 30);

        using var document = await PostGraphQLAsync(
            """
            query GetRoutes {
              routes(where: { status: { eq: COMPLETED } }) {
                id
                status
              }
            }
            """,
            accessToken: token);

        var routes = document.RootElement
            .GetProperty("data")
            .GetProperty("routes")
            .EnumerateArray()
            .ToList();

        routes.Should().NotBeEmpty();
        routes.Should().OnlyContain(r => r.GetProperty("status").GetString() == "COMPLETED");
        routes.Select(r => r.GetProperty("id").GetString()).Should().Contain(completedRouteId.ToString());
        routes.Select(r => r.GetProperty("id").GetString()).Should().NotContain(plannedRouteId.ToString());
    }

    [Fact]
    public async Task GetRoutes_WithVehicleIdFilter_ReturnsOnlyMatchingRoutes()
    {
        var token = await GetAdminAccessTokenAsync();
        var vehicleAId = DbSeeder.TestVehicleId;
        var vehicleBId = await SeedVehicleAsync($"HIST-{Guid.NewGuid():N}"[..20]);
        var vehicleARouteId = await SeedRouteAsync(vehicleAId, RouteStatus.Completed, startMileage: 50, endMileage: 75);
        await SeedRouteAsync(vehicleBId, RouteStatus.Completed, startMileage: 10, endMileage: 20);

        using var document = await PostGraphQLAsync(
            """
            query VehicleRoutes($vehicleId: UUID!) {
              routes(where: { vehicleId: { eq: $vehicleId } }) {
                id
                vehicleId
              }
            }
            """,
            new
            {
                vehicleId = vehicleAId
            },
            token);

        var routes = document.RootElement
            .GetProperty("data")
            .GetProperty("routes")
            .EnumerateArray()
            .ToList();

        routes.Should().Contain(r => r.GetProperty("id").GetString() == vehicleARouteId.ToString());
        routes.Should().OnlyContain(r => r.GetProperty("vehicleId").GetString() == vehicleAId.ToString());
    }

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedVehicleAsync(string plate)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vehicle = new Vehicle
        {
            RegistrationPlate = plate,
            Type = VehicleType.Van,
            ParcelCapacity = 25,
            WeightCapacity = 250m,
            Status = VehicleStatus.Available,
            DepotId = DbSeeder.TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync();
        return vehicle.Id;
    }

    private async Task<Guid> SeedRouteAsync(Guid vehicleId, RouteStatus status, int startMileage, int endMileage = 0)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var route = new Route
        {
            VehicleId = vehicleId,
            DriverId = DbSeeder.TestDriverId,
            StartDate = DateTimeOffset.UtcNow.AddHours(-2),
            EndDate = status == RouteStatus.Completed ? DateTimeOffset.UtcNow.AddHours(-1) : null,
            StartMileage = startMileage,
            EndMileage = endMileage,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Routes.Add(route);
        await dbContext.SaveChangesAsync();
        return route.Id;
    }

    private async Task SetVehicleStatusAsync(Guid vehicleId, VehicleStatus status)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vehicle = await dbContext.Vehicles.FindAsync(vehicleId);
        vehicle.Should().NotBeNull();
        vehicle!.Status = status;
        await dbContext.SaveChangesAsync();
    }
}
