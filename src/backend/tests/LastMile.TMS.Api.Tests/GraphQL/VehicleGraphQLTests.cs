using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class VehicleGraphQLTests : GraphQLTestBase, IAsyncLifetime
{
    public VehicleGraphQLTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Vehicles_WithoutToken_ReturnsAuthorizationError()
    {
        using var document = await PostGraphQLAsync(
            """
            query {
              vehicles {
                id
              }
            }
            """);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("authorized");
    }

    [Fact]
    public async Task CreateVehicle_WithoutToken_ReturnsAuthorizationError()
    {
        using var document = await PostGraphQLAsync(
            """
            mutation CreateVehicle($input: CreateVehicleInput!) {
              createVehicle(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    registrationPlate = "AUTH-TEST",
                    type = "VAN",
                    parcelCapacity = 10,
                    weightCapacity = 100,
                    status = "AVAILABLE",
                    depotId = DbSeeder.TestDepotId
                }
            });

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("authorized");
    }

    [Fact]
    public async Task CreateVehicle_WithValidInput_ReturnsVehicle()
    {
        var token = await GetAdminAccessTokenAsync();
        var plate = $"GQL-{Guid.NewGuid():N}"[..20];

        using var document = await PostGraphQLAsync(
            """
            mutation CreateVehicle($input: CreateVehicleInput!) {
              createVehicle(input: $input) {
                id
                registrationPlate
                status
                depotId
                depotName
                createdAt
                updatedAt
              }
            }
            """,
            new
            {
                input = new
                {
                    registrationPlate = plate,
                    type = "VAN",
                    parcelCapacity = 10,
                    weightCapacity = 100,
                    status = "AVAILABLE",
                    depotId = DbSeeder.TestDepotId
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var vehicle = document.RootElement
            .GetProperty("data")
            .GetProperty("createVehicle");

        vehicle.GetProperty("registrationPlate").GetString().Should().Be(plate);
        vehicle.GetProperty("status").GetString().Should().Be("AVAILABLE");
        vehicle.GetProperty("depotId").GetString().Should().Be(DbSeeder.TestDepotId.ToString());
        vehicle.GetProperty("depotName").GetString().Should().Be("Test Depot");
        vehicle.GetProperty("createdAt").GetString().Should().NotBeNullOrWhiteSpace();
        vehicle.GetProperty("updatedAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task CreateVehicle_WithDuplicatePlate_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();
        var plate = $"DUP-{Guid.NewGuid():N}"[..20];
        var variables = new
        {
            input = new
            {
                registrationPlate = plate,
                type = "VAN",
                parcelCapacity = 10,
                weightCapacity = 100,
                status = "AVAILABLE",
                depotId = DbSeeder.TestDepotId
            }
        };

        using var firstDocument = await PostGraphQLAsync(
            """
            mutation CreateVehicle($input: CreateVehicleInput!) {
              createVehicle(input: $input) {
                id
              }
            }
            """,
            variables,
            token);
        firstDocument.RootElement.TryGetProperty("errors", out _).Should().BeFalse();

        using var duplicateDocument = await PostGraphQLAsync(
            """
            mutation CreateVehicle($input: CreateVehicleInput!) {
              createVehicle(input: $input) {
                id
              }
            }
            """,
            variables,
            token);

        duplicateDocument.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateVehicle_WithInvalidInput_ReturnsValidationError()
    {
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation CreateVehicle($input: CreateVehicleInput!) {
              createVehicle(input: $input) {
                id
              }
            }
            """,
            new
            {
                input = new
                {
                    registrationPlate = "",
                    type = "VAN",
                    parcelCapacity = 0,
                    weightCapacity = -1,
                    status = "IN_USE",
                    depotId = DbSeeder.TestDepotId
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("New vehicles must be created with status Available");
    }

    [Fact]
    public async Task Vehicles_WithStatusFilter_ReturnsOnlyMatchingItems()
    {
        var token = await GetAdminAccessTokenAsync();
        var inUseVehicleId = await SeedVehicleAsync($"INUSE-{Guid.NewGuid():N}"[..20], VehicleStatus.InUse);

        using var document = await PostGraphQLAsync(
            """
            query GetVehicles {
              vehicles(where: { status: { eq: AVAILABLE } }) {
                id
                status
              }
            }
            """,
            accessToken: token);

        var vehicles = document.RootElement
            .GetProperty("data")
            .GetProperty("vehicles")
            .EnumerateArray()
            .ToList();

        vehicles.Should().NotBeEmpty();
        vehicles.Should().OnlyContain(v => v.GetProperty("status").GetString() == "AVAILABLE");
        vehicles.Select(v => v.GetProperty("id").GetString()).Should().NotContain(inUseVehicleId.ToString());
    }

    [Fact]
    public async Task UpdateVehicle_WithDuplicatePlate_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();
        var vehicleAId = await SeedVehicleAsync($"VEH-A-{Guid.NewGuid():N}"[..20], VehicleStatus.Available);
        var vehicleBId = await SeedVehicleAsync($"VEH-B-{Guid.NewGuid():N}"[..20], VehicleStatus.Available);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vehicleA = await dbContext.Vehicles.FindAsync(vehicleAId);
        vehicleA.Should().NotBeNull();

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateVehicle($id: UUID!, $input: UpdateVehicleInput!) {
              updateVehicle(id: $id, input: $input) {
                id
              }
            }
            """,
            new
            {
                id = vehicleBId,
                input = new
                {
                    registrationPlate = vehicleA!.RegistrationPlate,
                    type = "VAN",
                    parcelCapacity = 10,
                    weightCapacity = 100,
                    status = "AVAILABLE",
                    depotId = DbSeeder.TestDepotId
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateVehicle_ToAvailable_WithActivePlannedRoute_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();
        var vehicleId = await SeedVehicleAsync($"ACTIVE-{Guid.NewGuid():N}"[..20], VehicleStatus.Available);
        await SeedRouteAsync(vehicleId, RouteStatus.Draft, startMileage: 0);

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateVehicle($id: UUID!, $input: UpdateVehicleInput!) {
              updateVehicle(id: $id, input: $input) {
                id
              }
            }
            """,
            new
            {
                id = vehicleId,
                input = new
                {
                    registrationPlate = "ACTIVE-UPDATED",
                    type = "VAN",
                    parcelCapacity = 10,
                    weightCapacity = 100,
                    status = "AVAILABLE",
                    depotId = DbSeeder.TestDepotId
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("active route");
    }

    [Fact]
    public async Task UpdateVehicle_ToAvailable_WithOnlyCompletedRoute_ReturnsUpdatedVehicleWithStats()
    {
        var token = await GetAdminAccessTokenAsync();
        var vehicleId = await SeedVehicleAsync($"DONE-{Guid.NewGuid():N}"[..20], VehicleStatus.Available);
        await SeedRouteAsync(vehicleId, RouteStatus.Completed, startMileage: 100, endMileage: 220);

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateVehicle($id: UUID!, $input: UpdateVehicleInput!) {
              updateVehicle(id: $id, input: $input) {
                id
                registrationPlate
                status
                totalRoutes
                routesCompleted
                totalMileage
                updatedAt
              }
            }
            """,
            new
            {
                id = vehicleId,
                input = new
                {
                    registrationPlate = "DONE-UPDATED",
                    type = "VAN",
                    parcelCapacity = 12,
                    weightCapacity = 150,
                    status = "AVAILABLE",
                    depotId = DbSeeder.TestDepotId
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var updatedVehicle = document.RootElement
            .GetProperty("data")
            .GetProperty("updateVehicle");

        updatedVehicle.GetProperty("registrationPlate").GetString().Should().Be("DONE-UPDATED");
        updatedVehicle.GetProperty("status").GetString().Should().Be("AVAILABLE");
        updatedVehicle.GetProperty("totalRoutes").GetInt32().Should().Be(1);
        updatedVehicle.GetProperty("routesCompleted").GetInt32().Should().Be(1);
        updatedVehicle.GetProperty("totalMileage").GetInt32().Should().Be(120);
        updatedVehicle.GetProperty("updatedAt").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DeleteVehicle_WithUnknownId_ReturnsFalse()
    {
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation DeleteVehicle($id: UUID!) {
              deleteVehicle(id: $id)
            }
            """,
            new
            {
                id = Guid.NewGuid()
            },
            token);

        document.RootElement
            .GetProperty("data")
            .GetProperty("deleteVehicle")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task DeleteVehicle_WithActiveRoutes_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();
        var vehicleId = await SeedVehicleAsync($"DEL-{Guid.NewGuid():N}"[..20], VehicleStatus.Available);
        await SeedRouteAsync(vehicleId, RouteStatus.Draft, startMileage: 0);

        using var document = await PostGraphQLAsync(
            """
            mutation DeleteVehicle($id: UUID!) {
              deleteVehicle(id: $id)
            }
            """,
            new
            {
                id = vehicleId
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("active routes");
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedVehicleAsync(string plate, VehicleStatus status)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vehicle = new Vehicle
        {
            RegistrationPlate = plate,
            Type = VehicleType.Van,
            ParcelCapacity = 10,
            WeightCapacity = 100m,
            Status = status,
            DepotId = DbSeeder.TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync();
        return vehicle.Id;
    }

    private async Task SeedRouteAsync(Guid vehicleId, RouteStatus status, int startMileage, int endMileage = 0)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var route = new Route
        {
            ZoneId = DbSeeder.TestZoneId,
            VehicleId = vehicleId,
            DriverId = DbSeeder.TestDriverId,
            StartDate = DateTimeOffset.UtcNow.AddHours(-1),
            EndDate = status == RouteStatus.Completed ? DateTimeOffset.UtcNow : null,
            StartMileage = startMileage,
            EndMileage = endMileage,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Routes.Add(route);
        await dbContext.SaveChangesAsync();
    }
}
