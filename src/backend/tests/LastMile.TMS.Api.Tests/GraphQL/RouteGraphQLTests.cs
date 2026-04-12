using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class RouteGraphQLTests : GraphQLTestBase, IAsyncLifetime
{
    public RouteGraphQLTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

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
    public async Task CreateRoute_WithValidInput_ReturnsRouteAndStagesParcels()
    {
        var token = await GetAdminAccessTokenAsync();
        var startDate = DateTimeOffset.UtcNow.AddHours(2);

        using var document = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteInput!) {
              createRoute(input: $input) {
                id
                vehicleId
                vehiclePlate
                driverId
                stagingArea
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
                    zoneId = DbSeeder.TestZoneId,
                    vehicleId = DbSeeder.TestVehicleId,
                    driverId = DbSeeder.TestDriverId,
                    stagingArea = "A",
                    startDate,
                    startMileage = 250,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { DbSeeder.TestParcelId },
                    stops = Array.Empty<object>()
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
        route.GetProperty("stagingArea").GetString().Should().Be("A");
        route.GetProperty("status").GetString().Should().Be("DRAFT");
        route.GetProperty("parcelCount").GetInt32().Should().Be(1);
        route.GetProperty("parcelsDelivered").GetInt32().Should().Be(0);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vehicle = await dbContext.Vehicles.FindAsync(DbSeeder.TestVehicleId);
        vehicle.Should().NotBeNull();
        vehicle!.Status.Should().Be(VehicleStatus.InUse);

        var parcel = await dbContext.Parcels.FindAsync(DbSeeder.TestParcelId);
        parcel.Should().NotBeNull();
        parcel!.Status.Should().Be(ParcelStatus.Staged);

        var persistedRoute = await dbContext.Routes
            .Include(r => r.Parcels)
            .SingleAsync(r => r.Id == routeId);
        persistedRoute.Status.Should().Be(RouteStatus.Draft);
        persistedRoute.StagingArea.Should().Be(StagingArea.A);
        persistedRoute.ParcelCount.Should().Be(1);
    }

    [Fact]
    public async Task DispatchRoute_OnReadyDraftRoute_TransitionsAssignedParcelsToOutForDeliveryAndRecordsDispatchedAt()
    {
        var token = await GetAdminAccessTokenAsync();
        var zoneId = await SeedZoneAsync($"Dispatch Zone {Guid.NewGuid():N}"[..22]);
        var parcelId = await SeedParcelAsync(
            zoneId,
            $"LMDISP{Guid.NewGuid():N}"[..15].ToUpperInvariant(),
            ParcelStatus.Loaded);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            startMileage: 100,
            startDate: DateTimeOffset.UtcNow.AddHours(2),
            parcelIds: [parcelId]);

        using var document = await PostGraphQLAsync(
            """
            mutation DispatchRoute($id: UUID!) {
              dispatchRoute(id: $id) {
                id
                status
                updatedAt
                dispatchedAt
              }
            }
            """,
            new
            {
                id = routeId,
            },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("dispatchRoute");

        route.GetProperty("id").GetString().Should().Be(routeId.ToString());
        route.GetProperty("status").GetString().Should().Be("DISPATCHED");
        route.GetProperty("updatedAt").GetString().Should().NotBeNullOrWhiteSpace();
        route.GetProperty("dispatchedAt").GetString().Should().NotBeNullOrWhiteSpace();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = await dbContext.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == parcelId);
        var persistedRoute = await dbContext.Routes.SingleAsync(candidate => candidate.Id == routeId);

        persistedRoute.Status.Should().Be(RouteStatus.Dispatched);
        persistedRoute.DispatchedAt.Should().NotBeNull();
        parcel.Status.Should().Be(ParcelStatus.OutForDelivery);
        parcel.ZoneId.Should().Be(zoneId);
        parcel.ChangeHistory.Should().Contain(entry =>
            entry.FieldName == "Status"
            && entry.BeforeValue == "Loaded"
            && entry.AfterValue == "Out For Delivery");
    }

    [Fact]
    public async Task DispatchRoute_WithUnloadedParcel_ReturnsError()
    {
        var token = await GetAdminAccessTokenAsync();
        var parcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMBLOCK{Guid.NewGuid():N}"[..15].ToUpperInvariant(),
            ParcelStatus.Staged);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            startMileage: 100,
            startDate: DateTimeOffset.UtcNow.AddHours(2),
            parcelIds: [parcelId]);

        using var document = await PostGraphQLAsync(
            """
            mutation DispatchRoute($id: UUID!) {
              dispatchRoute(id: $id) {
                id
              }
            }
            """,
            new { id = routeId },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("must be loaded");
    }

    [Fact]
    public async Task StartRoute_OnDispatchedRoute_TransitionsOnlyRouteStatusAndKeepsParcelsUntouched()
    {
        var token = await GetAdminAccessTokenAsync();
        var zoneId = await SeedZoneAsync($"Lifecycle Zone {Guid.NewGuid():N}"[..24]);
        var parcelId = await SeedParcelAsync(
            zoneId,
            $"LMSTART{Guid.NewGuid():N}"[..15].ToUpperInvariant(),
            ParcelStatus.OutForDelivery);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Dispatched,
            StagingArea.A,
            startMileage: 100,
            startDate: DateTimeOffset.UtcNow.AddHours(-1),
            parcelIds: [parcelId]);
        await SetVehicleStatusAsync(DbSeeder.TestVehicleId, VehicleStatus.InUse);

        using var document = await PostGraphQLAsync(
            """
            mutation StartRoute($id: UUID!) {
              startRoute(id: $id) {
                id
                status
                updatedAt
              }
            }
            """,
            new
            {
                id = routeId,
            },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("startRoute");

        route.GetProperty("id").GetString().Should().Be(routeId.ToString());
        route.GetProperty("status").GetString().Should().Be("IN_PROGRESS");
        route.GetProperty("updatedAt").GetString().Should().NotBeNullOrWhiteSpace();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = await dbContext.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == parcelId);

        parcel.Status.Should().Be(ParcelStatus.OutForDelivery);
        parcel.ZoneId.Should().Be(zoneId);
        parcel.ChangeHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateRoute_WithNonUtcStartDate_NormalizesToUtc()
    {
        var token = await GetAdminAccessTokenAsync();
        var startDate = new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.FromHours(3));

        using var document = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteInput!) {
              createRoute(input: $input) {
                id
                startDate
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
                    stagingArea = "A",
                    startDate,
                    startMileage = 250,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { DbSeeder.TestParcelId },
                    stops = Array.Empty<object>()
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("createRoute");

        var routeId = route.GetProperty("id").GetGuid();
        route.GetProperty("startDate").GetDateTimeOffset().Should().Be(startDate.ToUniversalTime());

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedRoute = await dbContext.Routes.SingleAsync(candidate => candidate.Id == routeId);
        persistedRoute.StartDate.Should().Be(startDate.ToUniversalTime());
        persistedRoute.StartDate.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task CreateRoute_WithUnavailableVehicle_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();
        await SetVehicleStatusAsync(DbSeeder.TestVehicleId, VehicleStatus.Maintenance);

        using var document = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteInput!) {
              createRoute(input: $input) {
                id
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
                    stagingArea = "A",
                    startDate = DateTimeOffset.UtcNow.AddHours(1),
                    startMileage = 100,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { DbSeeder.TestParcelId },
                    stops = Array.Empty<object>()
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
            mutation CreateRoute($input: CreateRouteInput!) {
              createRoute(input: $input) {
                id
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
                    stagingArea = "A",
                    startDate = DateTimeOffset.UtcNow.AddHours(1),
                    startMileage = 100,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { Guid.NewGuid() },
                    stops = Array.Empty<object>()
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("One or more parcels not found");
    }

    [Fact]
    public async Task CreateRoute_WithParcelOutsideDriverZone_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();
        var alternateZoneId = await SeedZoneAsync("Route Alternate Zone");
        var alternateParcelId = await SeedParcelAsync(alternateZoneId, $"LMROUTE{Guid.NewGuid():N}"[..15].ToUpperInvariant());

        using var document = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteInput!) {
              createRoute(input: $input) {
                id
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
                    stagingArea = "A",
                    startDate = DateTimeOffset.UtcNow.AddHours(1),
                    startMileage = 100,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { alternateParcelId },
                    stops = Array.Empty<object>()
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("zone");
    }

    [Fact]
    public async Task CreateRoute_WithParcelAlreadyAssignedToAnotherActiveRoute_ReturnsInvalidOperationError()
    {
        var token = await GetAdminAccessTokenAsync();
        var alternateVehicleId = await SeedVehicleAsync($"ALT-{Guid.NewGuid():N}"[..20]);
        await SeedRouteAsync(
            alternateVehicleId,
            DbSeeder.TestDriver2Id,
            RouteStatus.Draft,
            StagingArea.B,
            startMileage: 50,
            parcelIds: DbSeeder.TestParcelId);

        using var document = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteInput!) {
              createRoute(input: $input) {
                id
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
                    stagingArea = "A",
                    startDate = DateTimeOffset.UtcNow.AddHours(1),
                    startMileage = 100,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { DbSeeder.TestParcelId },
                    stops = Array.Empty<object>()
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("active route");
    }

    [Fact]
    public async Task GetRoutes_WithStatusFilter_ReturnsOnlyMatchingRoutes()
    {
        var token = await GetAdminAccessTokenAsync();
        var availableVehicleId = DbSeeder.TestVehicleId;
        var completedVehicleId = await SeedVehicleAsync($"ROUTE-{Guid.NewGuid():N}"[..20]);
        var plannedRouteId = await SeedRouteAsync(
            availableVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            startMileage: 0);
        var completedRouteId = await SeedRouteAsync(
            completedVehicleId,
            DbSeeder.TestDriver2Id,
            RouteStatus.Completed,
            StagingArea.B,
            startMileage: 10,
            endMileage: 30);

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
        var vehicleARouteId = await SeedRouteAsync(
            vehicleAId,
            DbSeeder.TestDriverId,
            RouteStatus.Completed,
            StagingArea.A,
            startMileage: 50,
            endMileage: 75);
        await SeedRouteAsync(
            vehicleBId,
            DbSeeder.TestDriver2Id,
            RouteStatus.Completed,
            StagingArea.B,
            startMileage: 10,
            endMileage: 20);

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

    [Fact]
    public async Task Route_ById_ReturnsInitialAssignmentAuditTrail()
    {
        var token = await GetAdminAccessTokenAsync();
        var startDate = DateTimeOffset.UtcNow.AddHours(3);

        using var createDocument = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteInput!) {
              createRoute(input: $input) {
                id
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
                    stagingArea = "A",
                    startDate,
                    startMileage = 125,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { DbSeeder.TestParcelId },
                    stops = Array.Empty<object>()
                }
            },
            token);

        createDocument.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(createDocument.RootElement.GetRawText());

        var routeId = createDocument.RootElement
            .GetProperty("data")
            .GetProperty("createRoute")
            .GetProperty("id")
            .GetGuid();

        using var queryDocument = await PostGraphQLAsync(
            """
            query RouteById($id: UUID!) {
              route(id: $id) {
                id
                updatedAt
                assignmentAuditTrail {
                  action
                  previousDriverName
                  newDriverName
                  previousVehiclePlate
                  newVehiclePlate
                }
              }
            }
            """,
            new { id = routeId },
            token);

        queryDocument.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(queryDocument.RootElement.GetRawText());

        var route = queryDocument.RootElement
            .GetProperty("data")
            .GetProperty("route");

        route.GetProperty("id").GetString().Should().Be(routeId.ToString());
        route.GetProperty("updatedAt").ValueKind.Should().Be(JsonValueKind.Null);

        var auditTrail = route.GetProperty("assignmentAuditTrail").EnumerateArray().ToList();
        auditTrail.Should().ContainSingle();
        auditTrail[0].GetProperty("action").GetString().Should().Be("ASSIGNED");
        auditTrail[0].GetProperty("previousDriverName").ValueKind.Should().Be(JsonValueKind.Null);
        auditTrail[0].GetProperty("newDriverName").GetString().Should().Be("Test Driver");
        auditTrail[0].GetProperty("previousVehiclePlate").ValueKind.Should().Be(JsonValueKind.Null);
        auditTrail[0].GetProperty("newVehiclePlate").GetString().Should().Be("TEST-SEED-V001");
    }

    [Fact]
    public async Task RouteAssignmentCandidates_WhenEditing_IncludeCurrentAssignmentsAndFilterConflicts()
    {
        var token = await GetAdminAccessTokenAsync();
        var serviceDate = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(8), TimeSpan.Zero);
        var currentRouteId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            startMileage: 120,
            startDate: serviceDate);
        await SetVehicleStatusAsync(DbSeeder.TestVehicleId, VehicleStatus.InUse);

        var conflictingVehicleId = await SeedVehicleAsync($"BUSY-{Guid.NewGuid():N}"[..20]);
        await SeedRouteAsync(
            conflictingVehicleId,
            DbSeeder.TestDriver2Id,
            RouteStatus.InProgress,
            StagingArea.B,
            startMileage: 80,
            startDate: serviceDate.AddHours(1));
        await SetVehicleStatusAsync(conflictingVehicleId, VehicleStatus.InUse);

        var availableVehicleId = await SeedVehicleAsync($"OPEN-{Guid.NewGuid():N}"[..20]);
        await SeedRouteAsync(
            availableVehicleId,
            DbSeeder.TestDriver3Id,
            RouteStatus.Completed,
            StagingArea.A,
            startMileage: 10,
            endMileage: 35,
            startDate: serviceDate.AddHours(2));

        var maintenanceVehicleId = await SeedVehicleAsync($"MNT-{Guid.NewGuid():N}"[..20]);
        await SetVehicleStatusAsync(maintenanceVehicleId, VehicleStatus.Maintenance);
        await SetDriverAvailabilityAsync(DbSeeder.TestDriver4Id, serviceDate.DayOfWeek, isAvailable: false);

        using var document = await PostGraphQLAsync(
            """
            query Candidates($serviceDate: DateTime!, $zoneId: UUID!, $routeId: UUID) {
              routeAssignmentCandidates(serviceDate: $serviceDate, zoneId: $zoneId, routeId: $routeId) {
                vehicles {
                  id
                  registrationPlate
                  isCurrentAssignment
                }
                drivers {
                  id
                  displayName
                  workloadRoutes {
                    routeId
                    vehiclePlate
                    status
                  }
                }
              }
            }
            """,
            new
            {
                serviceDate,
                zoneId = DbSeeder.TestZoneId,
                routeId = currentRouteId
            },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var candidates = document.RootElement
            .GetProperty("data")
            .GetProperty("routeAssignmentCandidates");

        var vehicles = candidates.GetProperty("vehicles").EnumerateArray().ToList();
        vehicles.Select(vehicle => vehicle.GetProperty("id").GetString())
            .Should()
            .Contain(DbSeeder.TestVehicleId.ToString());
        vehicles.Select(vehicle => vehicle.GetProperty("id").GetString())
            .Should()
            .Contain(availableVehicleId.ToString());
        vehicles.Select(vehicle => vehicle.GetProperty("id").GetString())
            .Should()
            .NotContain(conflictingVehicleId.ToString());
        vehicles.Select(vehicle => vehicle.GetProperty("id").GetString())
            .Should()
            .NotContain(maintenanceVehicleId.ToString());

        var drivers = candidates.GetProperty("drivers").EnumerateArray().ToList();
        drivers.Select(driver => driver.GetProperty("id").GetString())
            .Should()
            .Contain(DbSeeder.TestDriverId.ToString());
        drivers.Select(driver => driver.GetProperty("id").GetString())
            .Should()
            .Contain(DbSeeder.TestDriver3Id.ToString());
        drivers.Select(driver => driver.GetProperty("id").GetString())
            .Should()
            .NotContain(DbSeeder.TestDriver2Id.ToString());
        drivers.Select(driver => driver.GetProperty("id").GetString())
            .Should()
            .NotContain(DbSeeder.TestDriver4Id.ToString());

        var driverWithCompletedWorkload = drivers.Single(
            driver => driver.GetProperty("id").GetString() == DbSeeder.TestDriver3Id.ToString());
        driverWithCompletedWorkload
            .GetProperty("workloadRoutes")
            .EnumerateArray()
            .ToList()
            .Should()
            .Contain(route => route.GetProperty("status").GetString() == "COMPLETED");
    }

    [Fact]
    public async Task UpdateRouteAssignment_OnPlannedRoute_ReassignsAndCreatesAuditTrail()
    {
        var token = await GetAdminAccessTokenAsync();
        var replacementVehicleId = await SeedVehicleAsync($"REASSIGN-{Guid.NewGuid():N}"[..20]);
        var startDate = DateTimeOffset.UtcNow.AddHours(4);

        using var createDocument = await PostGraphQLAsync(
            """
            mutation CreateRoute($input: CreateRouteInput!) {
              createRoute(input: $input) {
                id
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
                    stagingArea = "A",
                    startDate,
                    startMileage = 150,
                    assignmentMode = "MANUAL_PARCELS",
                    stopMode = "AUTO",
                    parcelIds = new[] { DbSeeder.TestParcelId },
                    stops = Array.Empty<object>()
                }
            },
            token);

        createDocument.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(createDocument.RootElement.GetRawText());

        var routeId = createDocument.RootElement
            .GetProperty("data")
            .GetProperty("createRoute")
            .GetProperty("id")
            .GetGuid();

        using var updateDocument = await PostGraphQLAsync(
            """
            mutation UpdateRouteAssignment($id: UUID!, $input: UpdateRouteAssignmentInput!) {
              updateRouteAssignment(id: $id, input: $input) {
                id
                driverId
                vehicleId
                updatedAt
              }
            }
            """,
            new
            {
                id = routeId,
                input = new
                {
                    vehicleId = replacementVehicleId,
                    driverId = DbSeeder.TestDriver2Id
                }
            },
            token);

        updateDocument.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(updateDocument.RootElement.GetRawText());

        var route = updateDocument.RootElement
            .GetProperty("data")
            .GetProperty("updateRouteAssignment");

        route.GetProperty("id").GetString().Should().Be(routeId.ToString());
        route.GetProperty("driverId").GetString().Should().Be(DbSeeder.TestDriver2Id.ToString());
        route.GetProperty("vehicleId").GetString().Should().Be(replacementVehicleId.ToString());
        route.GetProperty("updatedAt").GetString().Should().NotBeNullOrWhiteSpace();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedRoute = await dbContext.Routes
            .Include(r => r.AssignmentAuditTrail)
            .SingleAsync(r => r.Id == routeId);

        var originalVehicle = await dbContext.Vehicles.FindAsync(DbSeeder.TestVehicleId);
        var replacementVehicle = await dbContext.Vehicles.FindAsync(replacementVehicleId);
        originalVehicle.Should().NotBeNull();
        replacementVehicle.Should().NotBeNull();
        originalVehicle!.Status.Should().Be(VehicleStatus.Available);
        replacementVehicle!.Status.Should().Be(VehicleStatus.InUse);
        persistedRoute.AssignmentAuditTrail.Should().HaveCount(2);
        persistedRoute.AssignmentAuditTrail.Select(entry => entry.Action)
            .Should()
            .Contain([RouteAssignmentAuditAction.Assigned, RouteAssignmentAuditAction.Reassigned]);
    }

    [Fact]
    public async Task UpdateRouteAssignment_OnNonPlannedRoute_ReturnsError()
    {
        var token = await GetAdminAccessTokenAsync();
        var replacementVehicleId = await SeedVehicleAsync($"BLOCK-{Guid.NewGuid():N}"[..20]);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Completed,
            StagingArea.A,
            startMileage: 100,
            endMileage: 150,
            startDate: DateTimeOffset.UtcNow.AddHours(-6));

        using var document = await PostGraphQLAsync(
            """
            mutation UpdateRouteAssignment($id: UUID!, $input: UpdateRouteAssignmentInput!) {
              updateRouteAssignment(id: $id, input: $input) {
                id
              }
            }
            """,
            new
            {
                id = routeId,
                input = new
                {
                    vehicleId = replacementVehicleId,
                    driverId = DbSeeder.TestDriver2Id
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should()
            .Contain("Only draft routes can be reassigned before dispatch");
    }

    [Fact]
    public async Task CompleteRoute_OnInProgressRoute_MarksParcelsDeliveredAndReturnsVehicleToDepot()
    {
        var token = await GetAdminAccessTokenAsync();
        var zoneId = await SeedZoneAsync($"Delivery Zone {Guid.NewGuid():N}"[..23]);
        var parcelId = await SeedParcelAsync(
            zoneId,
            $"LMDONE{Guid.NewGuid():N}"[..15].ToUpperInvariant(),
            ParcelStatus.OutForDelivery);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.InProgress,
            StagingArea.A,
            startMileage: 100,
            startDate: DateTimeOffset.UtcNow.AddHours(-3),
            parcelIds: [parcelId]);
        await SetVehicleStatusAsync(DbSeeder.TestVehicleId, VehicleStatus.InUse);

        using var document = await PostGraphQLAsync(
            """
            mutation CompleteRoute($id: UUID!, $input: CompleteRouteInput!) {
              completeRoute(id: $id, input: $input) {
                id
                status
                endMileage
                endDate
                parcelsDelivered
                updatedAt
              }
            }
            """,
            new
            {
                id = routeId,
                input = new
                {
                    endMileage = 148,
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("completeRoute");

        route.GetProperty("id").GetString().Should().Be(routeId.ToString());
        route.GetProperty("status").GetString().Should().Be("COMPLETED");
        route.GetProperty("endMileage").GetInt32().Should().Be(148);
        route.GetProperty("endDate").GetString().Should().NotBeNullOrWhiteSpace();
        route.GetProperty("parcelsDelivered").GetInt32().Should().Be(1);
        route.GetProperty("updatedAt").GetString().Should().NotBeNullOrWhiteSpace();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedRoute = await dbContext.Routes.SingleAsync(candidate => candidate.Id == routeId);
        var vehicle = await dbContext.Vehicles.FindAsync(DbSeeder.TestVehicleId);
        var parcel = await dbContext.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .Include(candidate => candidate.TrackingEvents)
            .SingleAsync(candidate => candidate.Id == parcelId);

        persistedRoute.Status.Should().Be(RouteStatus.Completed);
        persistedRoute.EndMileage.Should().Be(148);
        vehicle.Should().NotBeNull();
        vehicle!.Status.Should().Be(VehicleStatus.Available);
        vehicle.DepotId.Should().Be(DbSeeder.TestDepotId);
        parcel.Status.Should().Be(ParcelStatus.Delivered);
        parcel.ZoneId.Should().Be(zoneId);
        parcel.ActualDeliveryDate.Should().NotBeNull();
        parcel.DeliveryAttempts.Should().Be(1);
        parcel.ChangeHistory.Should().Contain(entry =>
            entry.FieldName == "Status"
            && entry.BeforeValue == "Out For Delivery"
            && entry.AfterValue == "Delivered");
        parcel.TrackingEvents.Should().Contain(entry => entry.EventType == EventType.Delivered);
    }

    [Fact]
    public async Task CancelRoute_OnDispatchedRoute_ReturnsLoadedParcelsToSortedAndReleasesVehicle()
    {
        var token = await GetAdminAccessTokenAsync();
        var stagedParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMCAN{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            ParcelStatus.OutForDelivery);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Dispatched,
            StagingArea.A,
            startMileage: 100,
            startDate: DateTimeOffset.UtcNow.AddHours(3),
            parcelIds: [stagedParcelId]);
        await SetVehicleStatusAsync(DbSeeder.TestVehicleId, VehicleStatus.InUse);

        using var document = await PostGraphQLAsync(
            """
            mutation CancelRoute($id: UUID!, $input: CancelRouteInput!) {
              cancelRoute(id: $id, input: $input) {
                id
                status
                updatedAt
                cancellationReason
              }
            }
            """,
            new
            {
                id = routeId,
                input = new
                {
                    reason = "Depot closed because of weather",
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("cancelRoute");

        route.GetProperty("id").GetString().Should().Be(routeId.ToString());
        route.GetProperty("status").GetString().Should().Be("CANCELLED");
        route.GetProperty("updatedAt").GetString().Should().NotBeNullOrWhiteSpace();
        route.GetProperty("cancellationReason").GetString().Should().Be("Depot closed because of weather");

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedRoute = await dbContext.Routes.SingleAsync(candidate => candidate.Id == routeId);
        var vehicle = await dbContext.Vehicles.FindAsync(DbSeeder.TestVehicleId);
        var parcel = await dbContext.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .Include(candidate => candidate.TrackingEvents)
            .SingleAsync(candidate => candidate.Id == stagedParcelId);

        persistedRoute.Status.Should().Be(RouteStatus.Cancelled);
        persistedRoute.CancellationReason.Should().Be("Depot closed because of weather");
        vehicle.Should().NotBeNull();
        vehicle!.Status.Should().Be(VehicleStatus.Available);
        parcel.Status.Should().Be(ParcelStatus.Sorted);
        parcel.ChangeHistory.Should().Contain(entry =>
            entry.FieldName == "Status"
            && entry.BeforeValue == "Out For Delivery"
            && entry.AfterValue == "Sorted");
        parcel.TrackingEvents.Should().Contain(entry =>
            entry.Description.Contains("Depot closed because of weather"));
    }

    [Fact]
    public async Task Routes_WithDriverToken_ReturnsAuthorizationError()
    {
        var token = await GetAccessTokenAsync("driver.test@lastmile.local", "Driver@12345");

        using var document = await PostGraphQLAsync(
            """
            query {
              routes {
                id
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString().Should().Contain("authorized");
    }

    [Fact]
    public async Task MyRoutes_WithDriverToken_ReturnsOnlyAssignedRoutes()
    {
        var token = await GetAccessTokenAsync("driver.test@lastmile.local", "Driver@12345");
        var assignedRouteId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Dispatched,
            StagingArea.A,
            startMileage: 100,
            startDate: DateTimeOffset.UtcNow.AddHours(1));
        await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriver2Id,
            RouteStatus.Dispatched,
            StagingArea.B,
            startMileage: 120,
            startDate: DateTimeOffset.UtcNow.AddHours(2));

        using var document = await PostGraphQLAsync(
            """
            query {
              myRoutes(order: [{ startDate: ASC }]) {
                id
                driverId
                status
                dispatchedAt
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());

        var routes = document.RootElement
            .GetProperty("data")
            .GetProperty("myRoutes")
            .EnumerateArray()
            .ToList();

        routes.Should().Contain(route => route.GetProperty("id").GetString() == assignedRouteId.ToString());
        routes.Should().OnlyContain(route => route.GetProperty("driverId").GetString() == DbSeeder.TestDriverId.ToString());
        routes.Should().Contain(route =>
            route.GetProperty("id").GetString() == assignedRouteId.ToString()
            && route.GetProperty("dispatchedAt").ValueKind == JsonValueKind.String);
    }

    [Fact]
    public async Task MyRoute_WithDriverToken_DoesNotReturnOtherDriversRoute()
    {
        var token = await GetAccessTokenAsync("driver.test@lastmile.local", "Driver@12345");
        var otherRouteId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriver2Id,
            RouteStatus.Draft,
            StagingArea.A,
            startMileage: 100,
            startDate: DateTimeOffset.UtcNow.AddHours(3));

        using var document = await PostGraphQLAsync(
            """
            query MyRoute($id: UUID!) {
              myRoute(id: $id) {
                id
              }
            }
            """,
            new { id = otherRouteId },
            token);

        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse(document.RootElement.GetRawText());
        document.RootElement
            .GetProperty("data")
            .GetProperty("myRoute")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task CancelRoute_OnNonPlannedRoute_ReturnsError()
    {
        var token = await GetAdminAccessTokenAsync();
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Completed,
            StagingArea.A,
            startMileage: 100,
            endMileage: 130,
            startDate: DateTimeOffset.UtcNow.AddHours(-4));

        using var document = await PostGraphQLAsync(
            """
            mutation CancelRoute($id: UUID!, $input: CancelRouteInput!) {
              cancelRoute(id: $id, input: $input) {
                id
              }
            }
            """,
            new
            {
                id = routeId,
                input = new
                {
                    reason = "Completed manually",
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors[0].GetProperty("message").GetString()
            .Should()
            .Contain("Only draft or dispatched routes can be cancelled before route start");
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedVehicleAsync(string plate)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
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

    private async Task<Guid> SeedZoneAsync(string name)
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
            DepotId = DbSeeder.TestDepotId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Zones.Add(zone);
        await dbContext.SaveChangesAsync();
        return zone.Id;
    }

    private async Task<Guid> SeedParcelAsync(
        Guid zoneId,
        string trackingNumber,
        ParcelStatus status = ParcelStatus.Sorted)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = new Parcel
        {
            TrackingNumber = trackingNumber,
            Description = "Seeded route validation parcel",
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
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Parcels.Add(parcel);
        await dbContext.SaveChangesAsync();
        return parcel.Id;
    }

    private async Task<Guid> SeedRouteAsync(
        Guid vehicleId,
        Guid driverId,
        RouteStatus status,
        StagingArea stagingArea,
        int startMileage,
        int endMileage = 0,
        DateTimeOffset? startDate = null,
        params Guid[] parcelIds)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcels = parcelIds.Length == 0
            ? []
            : await dbContext.Parcels
                .Where(parcel => parcelIds.Contains(parcel.Id))
                .ToListAsync();
        var routeZoneId = parcels.FirstOrDefault()?.ZoneId
            ?? await dbContext.Drivers
                .Where(driver => driver.Id == driverId)
                .Select(driver => driver.ZoneId)
                .SingleAsync();

        var route = new Route
        {
            ZoneId = routeZoneId,
            VehicleId = vehicleId,
            DriverId = driverId,
            StartDate = startDate ?? DateTimeOffset.UtcNow.AddHours(-2),
            DispatchedAt = status is RouteStatus.Dispatched or RouteStatus.InProgress or RouteStatus.Completed
                ? (startDate ?? DateTimeOffset.UtcNow.AddHours(-2)).AddMinutes(-20)
                : null,
            EndDate = status == RouteStatus.Completed
                ? (startDate ?? DateTimeOffset.UtcNow.AddHours(-2)).AddHours(1)
                : null,
            StartMileage = startMileage,
            EndMileage = endMileage,
            Status = status,
            StagingArea = stagingArea,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
            Parcels = parcels
        };

        dbContext.Routes.Add(route);
        await dbContext.SaveChangesAsync();
        return route.Id;
    }

    private async Task SetVehicleStatusAsync(Guid vehicleId, VehicleStatus status)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vehicle = await dbContext.Vehicles.FindAsync(vehicleId);
        vehicle.Should().NotBeNull();
        vehicle!.Status = status;
        await dbContext.SaveChangesAsync();
    }

    private async Task SetDriverAvailabilityAsync(
        Guid driverId,
        DayOfWeek dayOfWeek,
        bool isAvailable)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingRows = await dbContext.DriverAvailabilities
            .Where(row => row.DriverId == driverId && row.DayOfWeek == dayOfWeek)
            .ToListAsync();
        dbContext.DriverAvailabilities.RemoveRange(existingRows);

        dbContext.DriverAvailabilities.Add(new DriverAvailability
        {
            DriverId = driverId,
            DayOfWeek = dayOfWeek,
            ShiftStart = new TimeOnly(8, 0),
            ShiftEnd = new TimeOnly(17, 0),
            IsAvailable = isAvailable,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        });

        await dbContext.SaveChangesAsync();
    }
}
