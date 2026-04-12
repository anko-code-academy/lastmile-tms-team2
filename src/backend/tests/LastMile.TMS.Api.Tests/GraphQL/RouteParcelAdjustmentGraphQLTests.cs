using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class RouteParcelAdjustmentGraphQLTests : GraphQLTestBase, IAsyncLifetime
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public RouteParcelAdjustmentGraphQLTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AddParcelToDispatchedRoute_ReturnsUpdatedStopsAndLatestAdjustment()
    {
        var token = await GetAdminAccessTokenAsync();
        var routeParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMROUTEADD{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            ParcelStatus.OutForDelivery,
            DbSeeder.TestParcelRecipientAddressId);
        var candidateParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMROUTECAND{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            ParcelStatus.Staged,
            DbSeeder.TestParcelRecipientAddressId);
        var routeId = await SeedRouteWithStopsAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Dispatched,
            StagingArea.A,
            DateTimeOffset.UtcNow.AddMinutes(-30),
            [routeParcelId]);

        using var document = await PostGraphQLAsync(
            """
            mutation AddParcelToDispatchedRoute($id: UUID!, $input: AdjustRouteParcelInput!) {
              addParcelToDispatchedRoute(id: $id, input: $input) {
                id
                parcelCount
                stops {
                  sequence
                  parcels {
                    trackingNumber
                  }
                }
                latestParcelAdjustment {
                  action
                  trackingNumber
                  reason
                }
              }
            }
            """,
            new
            {
                id = routeId,
                input = new
                {
                    parcelId = candidateParcelId,
                    reason = "Late staged handoff",
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("addParcelToDispatchedRoute");

        route.GetProperty("id").GetString().Should().Be(routeId.ToString());
        route.GetProperty("parcelCount").GetInt32().Should().Be(2);
        route.GetProperty("stops")[0].GetProperty("parcels").GetArrayLength().Should().Be(2);
        route.GetProperty("latestParcelAdjustment").GetProperty("action").GetString().Should().Be("ADDED");
        route.GetProperty("latestParcelAdjustment").GetProperty("reason").GetString().Should().Be("Late staged handoff");

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedParcel = await dbContext.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == candidateParcelId);
        var auditEntry = await dbContext.RouteParcelAdjustmentAuditEntries
            .SingleAsync(candidate => candidate.RouteId == routeId && candidate.ParcelId == candidateParcelId);

        persistedParcel.Status.Should().Be(ParcelStatus.OutForDelivery);
        persistedParcel.ChangeHistory.Should().Contain(entry =>
            entry.FieldName == "Status"
            && entry.BeforeValue == "Staged"
            && entry.AfterValue == "Out For Delivery");
        auditEntry.Action.Should().Be(RouteParcelAdjustmentAction.Added);
        auditEntry.Reason.Should().Be("Late staged handoff");
    }

    [Fact]
    public async Task RemoveParcelFromDispatchedRoute_ReturnsUpdatedAuditAndStagesParcelAtDepot()
    {
        var token = await GetAdminAccessTokenAsync();
        var firstParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMROUTERM{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.OutForDelivery,
            DbSeeder.TestParcelRecipientAddressId);
        var secondAddressId = await SeedRecipientAddressAsync(
            "77 Return Street",
            151.245,
            -33.884);
        var secondParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMROUTERM2{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.OutForDelivery,
            secondAddressId);
        var routeId = await SeedRouteWithStopsAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Dispatched,
            StagingArea.A,
            DateTimeOffset.UtcNow.AddMinutes(-45),
            [firstParcelId, secondParcelId]);

        using var document = await PostGraphQLAsync(
            """
            mutation RemoveParcelFromDispatchedRoute($id: UUID!, $input: AdjustRouteParcelInput!) {
              removeParcelFromDispatchedRoute(id: $id, input: $input) {
                id
                parcelCount
                stops {
                  sequence
                  parcels {
                    trackingNumber
                  }
                }
                latestParcelAdjustment {
                  action
                  trackingNumber
                  reason
                }
              }
            }
            """,
            new
            {
                id = routeId,
                input = new
                {
                    parcelId = firstParcelId,
                    reason = "Customer cancelled at depot",
                }
            },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("removeParcelFromDispatchedRoute");

        route.GetProperty("parcelCount").GetInt32().Should().Be(1);
        route.GetProperty("stops").GetArrayLength().Should().Be(1);
        route.GetProperty("stops")[0].GetProperty("sequence").GetInt32().Should().Be(1);
        route.GetProperty("latestParcelAdjustment").GetProperty("action").GetString().Should().Be("REMOVED");
        route.GetProperty("latestParcelAdjustment").GetProperty("trackingNumber").GetString().Should().NotBeNullOrWhiteSpace();

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var removedParcel = await dbContext.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == firstParcelId);
        var auditEntry = await dbContext.RouteParcelAdjustmentAuditEntries
            .SingleAsync(candidate => candidate.RouteId == routeId && candidate.ParcelId == firstParcelId);

        removedParcel.Status.Should().Be(ParcelStatus.Staged);
        removedParcel.ChangeHistory.Should().Contain(entry =>
            entry.FieldName == "Status"
            && entry.BeforeValue == "Out For Delivery"
            && entry.AfterValue == "Staged");
        auditEntry.Action.Should().Be(RouteParcelAdjustmentAction.Removed);
        auditEntry.Reason.Should().Be("Customer cancelled at depot");
    }

    [Fact]
    public async Task DispatchedRouteParcelCandidates_ReturnsOnlyUnassignedStagedRouteCandidates()
    {
        var token = await GetAdminAccessTokenAsync();
        var routeParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMCANDRT{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.OutForDelivery,
            DbSeeder.TestParcelRecipientAddressId);
        var routeId = await SeedRouteWithStopsAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Dispatched,
            StagingArea.A,
            DateTimeOffset.UtcNow.AddMinutes(-20),
            [routeParcelId]);
        var availableParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMCANDOK{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.Staged,
            await SeedRecipientAddressAsync("15 Eligible Street", 151.231, -33.881));
        var wrongZoneId = await SeedZoneAsync($"Adjust Alt Zone {Guid.NewGuid():N}"[..20]);
        await SeedParcelAsync(
            wrongZoneId,
            $"LMCANDNO{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.Staged,
            await SeedRecipientAddressAsync("16 Wrong Zone Street", 151.241, -33.89));
        var assignedParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMCANDAS{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.Staged,
            await SeedRecipientAddressAsync("17 Assigned Street", 151.251, -33.891));
        await SeedRouteWithStopsAsync(
            await SeedVehicleAsync($"ALT-{Guid.NewGuid():N}"[..20]),
            DbSeeder.TestDriver2Id,
            RouteStatus.Draft,
            StagingArea.B,
            DateTimeOffset.UtcNow.AddHours(1),
            [assignedParcelId]);
        var expectedTrackingNumber = await GetTrackingNumberAsync(availableParcelId);

        using var document = await PostGraphQLAsync(
            """
            query GetDispatchedRouteParcelCandidates($routeId: UUID!) {
              dispatchedRouteParcelCandidates(routeId: $routeId) {
                trackingNumber
                status
              }
            }
            """,
            new { routeId },
            token);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var candidates = document.RootElement
            .GetProperty("data")
            .GetProperty("dispatchedRouteParcelCandidates")
            .EnumerateArray()
            .ToList();

        candidates.Select(candidate => candidate.GetProperty("trackingNumber").GetString())
            .Should()
            .ContainSingle(trackingNumber => trackingNumber == expectedTrackingNumber);
        candidates.Should().OnlyContain(candidate => candidate.GetProperty("status").GetString() == "STAGED");
    }

    [Fact]
    public async Task MyRoute_ReturnsLatestParcelAdjustmentForDriver()
    {
        var adminToken = await GetAdminAccessTokenAsync();
        var driverToken = await GetAccessTokenAsync("driver.test@lastmile.local", "Driver@12345");
        var routeParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMDRIVER{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.OutForDelivery,
            DbSeeder.TestParcelRecipientAddressId);
        var routeId = await SeedRouteWithStopsAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Dispatched,
            StagingArea.A,
            DateTimeOffset.UtcNow.AddMinutes(-15),
            [routeParcelId]);
        var candidateParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMDRVUPD{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.Staged,
            DbSeeder.TestParcelRecipientAddressId);

        using var mutationDocument = await PostGraphQLAsync(
            """
            mutation AddParcelToDispatchedRoute($id: UUID!, $input: AdjustRouteParcelInput!) {
              addParcelToDispatchedRoute(id: $id, input: $input) {
                id
              }
            }
            """,
            new
            {
                id = routeId,
                input = new
                {
                    parcelId = candidateParcelId,
                    reason = "Driver web refresh check",
                }
            },
            adminToken);

        mutationDocument.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(mutationDocument.RootElement.GetRawText());

        using var document = await PostGraphQLAsync(
            """
            query MyRoute($id: UUID!) {
              myRoute(id: $id) {
                id
                latestParcelAdjustment {
                  action
                  trackingNumber
                  reason
                }
                parcelAdjustmentAuditTrail {
                  action
                  reason
                }
              }
            }
            """,
            new { id = routeId },
            driverToken);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var route = document.RootElement
            .GetProperty("data")
            .GetProperty("myRoute");

        route.GetProperty("latestParcelAdjustment").GetProperty("action").GetString().Should().Be("ADDED");
        route.GetProperty("latestParcelAdjustment").GetProperty("reason").GetString().Should().Be("Driver web refresh check");
        route.GetProperty("parcelAdjustmentAuditTrail").GetArrayLength().Should().BeGreaterThan(0);
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
            Boundary = (Polygon)templateZone.Boundary.Copy(),
            DepotId = DbSeeder.TestDepotId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Zones.Add(zone);
        await dbContext.SaveChangesAsync();
        return zone.Id;
    }

    private async Task<Guid> SeedRecipientAddressAsync(
        string street1,
        double longitude,
        double latitude)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var address = new Address
        {
            Street1 = street1,
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
            IsResidential = true,
            ContactName = "Route Adjustment",
            GeoLocation = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude)),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Addresses.Add(address);
        await dbContext.SaveChangesAsync();
        return address.Id;
    }

    private async Task<string> GetTrackingNumberAsync(Guid parcelId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Parcels
            .Where(parcel => parcel.Id == parcelId)
            .Select(parcel => parcel.TrackingNumber)
            .SingleAsync();
    }

    private async Task<Guid> SeedParcelAsync(
        Guid zoneId,
        string trackingNumber,
        ParcelStatus status,
        Guid recipientAddressId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = new Parcel
        {
            TrackingNumber = trackingNumber,
            Description = "Seeded route parcel adjustment parcel",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddressId = DbSeeder.TestParcelShipperAddressId,
            RecipientAddressId = recipientAddressId,
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

    private async Task<Guid> SeedRouteWithStopsAsync(
        Guid vehicleId,
        Guid driverId,
        RouteStatus status,
        StagingArea stagingArea,
        DateTimeOffset startDate,
        IReadOnlyList<Guid> parcelIds)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcels = await dbContext.Parcels
            .Include(candidate => candidate.RecipientAddress)
            .Where(parcel => parcelIds.Contains(parcel.Id))
            .OrderBy(parcel => parcel.TrackingNumber)
            .ToListAsync();

        foreach (var parcel in parcels.Where(candidate => candidate.RecipientAddress.GeoLocation is null))
        {
            parcel.RecipientAddress.GeoLocation = GeometryFactory.CreatePoint(new Coordinate(151.215, -33.872));
        }

        var route = new Route
        {
            ZoneId = parcels[0].ZoneId,
            VehicleId = vehicleId,
            DriverId = driverId,
            StartDate = startDate,
            DispatchedAt = status is RouteStatus.Dispatched or RouteStatus.InProgress or RouteStatus.Completed
                ? startDate.AddMinutes(-20)
                : null,
            EndDate = status == RouteStatus.Completed ? startDate.AddHours(1) : null,
            StartMileage = 100,
            EndMileage = status == RouteStatus.Completed ? 126 : 0,
            PlannedDistanceMeters = 12500,
            PlannedDurationSeconds = 2100,
            PlannedPath = GeometryFactory.CreateLineString(
                [
                    new Coordinate(151.2093, -33.8688),
                    new Coordinate(151.2124, -33.8704),
                    new Coordinate(151.2150, -33.8720)
                ]),
            Status = status,
            StagingArea = stagingArea,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
            Parcels = parcels,
        };

        var sequence = 1;
        foreach (var parcel in parcels)
        {
            var location = parcel.RecipientAddress.GeoLocation
                ?? GeometryFactory.CreatePoint(new Coordinate(151.215, -33.872));

            route.Stops.Add(new RouteStop
            {
                Sequence = sequence++,
                RecipientLabel = parcel.RecipientAddress.ContactName ?? parcel.TrackingNumber,
                Street1 = parcel.RecipientAddress.Street1,
                Street2 = parcel.RecipientAddress.Street2,
                City = parcel.RecipientAddress.City,
                State = parcel.RecipientAddress.State,
                PostalCode = parcel.RecipientAddress.PostalCode,
                CountryCode = parcel.RecipientAddress.CountryCode,
                StopLocation = location.Copy() as Point ?? location,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "tests",
                Parcels = [parcel]
            });
        }

        dbContext.Routes.Add(route);
        await dbContext.SaveChangesAsync();
        return route.Id;
    }
}
