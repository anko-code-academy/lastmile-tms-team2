using System.Threading.Tasks;
using FluentAssertions;
using LastMile.TMS.Api.Tests.GraphQL;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class RouteUpdateHubTests(CustomWebApplicationFactory factory)
    : GraphQLTestBase(factory), IAsyncLifetime
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    [Fact]
    public async Task RouteUpdated_IsPublishedToAssignedDriverSubscribers_WhenRouteAdjusts()
    {
        var adminToken = await GetAdminAccessTokenAsync();
        var driverToken = await GetAccessTokenAsync("driver.test@lastmile.local", "Driver@12345");
        var routeParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMHUBRT{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.OutForDelivery,
            DbSeeder.TestParcelRecipientAddressId);
        var candidateParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            $"LMHUBADD{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ParcelStatus.Staged,
            DbSeeder.TestParcelRecipientAddressId);
        var routeId = await SeedRouteWithStopsAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Dispatched,
            StagingArea.A,
            DateTimeOffset.UtcNow.AddMinutes(-20),
            [routeParcelId]);
        var trackingNumber = await GetTrackingNumberAsync(candidateParcelId);

        var updateTask = new TaskCompletionSource<RouteUpdateMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(Client.BaseAddress!, "/hubs/routes"),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(driverToken);
                })
            .Build();

        connection.On<RouteUpdateMessage>("RouteUpdated", update => updateTask.TrySetResult(update));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToMyRoutes");

        using var document = await PostGraphQLAsync(
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
                    reason = "Hub notification check",
                }
            },
            adminToken);

        document.RootElement.TryGetProperty("errors", out _)
            .Should()
            .BeFalse(document.RootElement.GetRawText());

        var update = await updateTask.Task.WaitAsync(TimeSpan.FromSeconds(10));

        update.RouteId.Should().Be(routeId);
        update.Action.Should().Be("Added");
        update.TrackingNumber.Should().Be(trackingNumber);
        update.Reason.Should().Be("Hub notification check");
        update.ChangedAt.Should().NotBeNull();

        await connection.DisposeAsync();
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

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
            Description = "Seeded route hub parcel",
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
            StartMileage = 100,
            Status = status,
            StagingArea = stagingArea,
            PlannedDistanceMeters = 12500,
            PlannedDurationSeconds = 2100,
            PlannedPath = GeometryFactory.CreateLineString(
                [
                    new Coordinate(151.2093, -33.8688),
                    new Coordinate(151.2124, -33.8704),
                    new Coordinate(151.2150, -33.8720)
                ]),
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

    private sealed record RouteUpdateMessage(
        Guid DriverUserId,
        Guid RouteId,
        string Action,
        string TrackingNumber,
        string Reason,
        DateTimeOffset? ChangedAt);
}
