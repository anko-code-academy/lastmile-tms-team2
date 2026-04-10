using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Queries;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Parcels;

public class RouteLoadOutQueryHandlerTests
{
    private static AppDbContext MakeDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static ICurrentUserService CreateCurrentUser(ApplicationUser user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id.ToString());
        currentUser.UserName.Returns(user.UserName);
        currentUser.Roles.Returns(["WarehouseOperator"]);
        return currentUser;
    }

    [Fact]
    public async Task GetLoadOutRoutes_NoDepotId_ReturnsEmptyList()
    {
        await using var db = MakeDbContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            DepotId = null,
            ZoneId = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var currentUser = CreateCurrentUser(user);
        var handler = new GetLoadOutRoutesQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetLoadOutRoutesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLoadOutRoutes_OtherDepot_ReturnsEmptyList()
    {
        await using var db = MakeDbContext();
        var (fixture, currentUser) = await SeedTwoDepotsAsync(db);

        var handler = new GetLoadOutRoutesQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetLoadOutRoutesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLoadOutRoutes_OwnDepotWithPlannedRouteAndParcels_ReturnsRoute()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, depotId: null);
        var currentUser = CreateCurrentUser(fixture.Operator);

        var handler = new GetLoadOutRoutesQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetLoadOutRoutesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(fixture.Route.Id);
        result[0].VehiclePlate.Should().Be(fixture.Vehicle.RegistrationPlate);
        result[0].DriverName.Should().Be($"{fixture.Driver.FirstName} {fixture.Driver.LastName}".Trim());
        result[0].Status.Should().Be(RouteStatus.Planned);
        result[0].ExpectedParcelCount.Should().Be(2);
        result[0].LoadedParcelCount.Should().Be(2);
        result[0].RemainingParcelCount.Should().Be(0);
    }

    [Fact]
    public async Task GetLoadOutRoutes_RouteInWrongStatus_NotReturned()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, depotId: null);
        fixture.Route.Status = RouteStatus.InProgress;
        await db.SaveChangesAsync();
        var currentUser = CreateCurrentUser(fixture.Operator);

        var handler = new GetLoadOutRoutesQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetLoadOutRoutesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLoadOutRoutes_RouteWithNoParcels_NotReturned()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, depotId: null);
        fixture.Route.Parcels.Clear();
        await db.SaveChangesAsync();
        var currentUser = CreateCurrentUser(fixture.Operator);

        var handler = new GetLoadOutRoutesQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetLoadOutRoutesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRouteLoadOutBoard_NoDepotId_ReturnsNull()
    {
        await using var db = MakeDbContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            DepotId = null,
            ZoneId = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var currentUser = CreateCurrentUser(user);
        var handler = new GetRouteLoadOutBoardQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetRouteLoadOutBoardQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRouteLoadOutBoard_ValidRoute_ReturnsBoard()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, depotId: null);
        var currentUser = CreateCurrentUser(fixture.Operator);

        var handler = new GetRouteLoadOutBoardQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetRouteLoadOutBoardQuery(fixture.Route.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(fixture.Route.Id);
        result.VehiclePlate.Should().Be(fixture.Vehicle.RegistrationPlate);
        result.DriverName.Should().Be($"{fixture.Driver.FirstName} {fixture.Driver.LastName}".Trim());
        result.Status.Should().Be(RouteStatus.Planned);
        result.ExpectedParcelCount.Should().Be(2);
        result.LoadedParcelCount.Should().Be(2);
        result.RemainingParcelCount.Should().Be(0);
        result.ExpectedParcels.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRouteLoadOutBoard_InProgressRoute_ReturnsBoard()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, depotId: null);
        fixture.Route.Status = RouteStatus.InProgress;
        await db.SaveChangesAsync();
        var currentUser = CreateCurrentUser(fixture.Operator);

        var handler = new GetRouteLoadOutBoardQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetRouteLoadOutBoardQuery(fixture.Route.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be(RouteStatus.InProgress);
    }

    [Fact]
    public async Task GetRouteLoadOutBoard_WrongDepot_ReturnsNull()
    {
        await using var db = MakeDbContext();
        var (otherDepotFixture, _) = await SeedTwoDepotsAsync(db);
        var currentUser = CreateCurrentUser(otherDepotFixture.Operator);

        var handler = new GetRouteLoadOutBoardQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetRouteLoadOutBoardQuery(otherDepotFixture.Route.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRouteLoadOutBoard_NonExistentRoute_ReturnsNull()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, depotId: null);
        var currentUser = CreateCurrentUser(fixture.Operator);

        var handler = new GetRouteLoadOutBoardQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetRouteLoadOutBoardQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    private static async Task<QueryFixture> SeedFixtureAsync(AppDbContext db, Guid? depotId)
    {
        var address = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "1 Test Street",
            City = "TestCity",
            State = "TS",
            PostalCode = "12345",
            CountryCode = "US",
        };

        var depot = new Depot
        {
            Id = depotId ?? Guid.NewGuid(),
            Name = "Test Depot",
            AddressId = address.Id,
            Address = address,
            IsActive = true,
        };

        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "Test Zone",
            Boundary = TestsPolygonFactory.CreateDefault(),
            DepotId = depot.Id,
            Depot = depot,
            IsActive = true,
        };

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationPlate = "TEST-001",
            DepotId = depot.Id,
            Depot = depot,
            Status = VehicleStatus.InUse,
            ParcelCapacity = 100,
            WeightCapacity = 1000,
        };

        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            DepotId = depot.Id,
            Depot = depot,
            ZoneId = zone.Id,
            Zone = zone,
            Status = DriverStatus.Active,
            LicenseNumber = "LIC-001",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            UserId = Guid.NewGuid(),
        };

        var operatorUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "operator@test.com",
            Email = "operator@test.com",
            FirstName = "Operator",
            LastName = "User",
            IsActive = true,
            DepotId = depot.Id,
            ZoneId = zone.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var shipper = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "10 Shipper Lane",
            City = "TestCity",
            State = "TS",
            PostalCode = "12346",
            CountryCode = "US",
        };

        var recipient = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "20 Recipient Road",
            City = "TestCity",
            State = "TS",
            PostalCode = "12347",
            CountryCode = "US",
            ContactName = "Recipient",
        };

        var parcel1 = CreateParcel("LM-ROUTEQ-001", ParcelStatus.Loaded, shipper, recipient, zone);
        var parcel2 = CreateParcel("LM-ROUTEQ-002", ParcelStatus.Loaded, shipper, recipient, zone);

        var route = new Route
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            DriverId = driver.Id,
            Driver = driver,
            StartDate = DateTimeOffset.UtcNow,
            StagingArea = StagingArea.A,
            Status = RouteStatus.Planned,
            Parcels = [parcel1, parcel2],
        };

        db.AddRange(address, depot, zone, vehicle, driver, operatorUser, shipper, recipient, parcel1, parcel2, route);
        await db.SaveChangesAsync();

        return new QueryFixture(depot, operatorUser, route, vehicle, driver);
    }

    private static async Task<(QueryFixture OtherDepotFixture, ICurrentUserService CurrentUser)> SeedTwoDepotsAsync(AppDbContext db)
    {
        var address1 = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "1 Depot1 Street",
            City = "City1",
            State = "S1",
            PostalCode = "11111",
            CountryCode = "US",
        };

        var depot1 = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "Depot One",
            AddressId = address1.Id,
            Address = address1,
            IsActive = true,
        };

        var zone1 = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "Zone One",
            Boundary = TestsPolygonFactory.CreateDefault(),
            DepotId = depot1.Id,
            Depot = depot1,
            IsActive = true,
        };

        var vehicle1 = new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationPlate = "DEPOT1-001",
            DepotId = depot1.Id,
            Depot = depot1,
            Status = VehicleStatus.InUse,
            ParcelCapacity = 100,
            WeightCapacity = 1000,
        };

        var driver1 = new Driver
        {
            Id = Guid.NewGuid(),
            FirstName = "Driver",
            LastName = "One",
            DepotId = depot1.Id,
            Depot = depot1,
            ZoneId = zone1.Id,
            Zone = zone1,
            Status = DriverStatus.Active,
            LicenseNumber = "LIC-D1",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            UserId = Guid.NewGuid(),
        };

        var operatorUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "operator@depot2.com",
            Email = "operator@depot2.com",
            FirstName = "Op",
            LastName = "Two",
            IsActive = true,
            DepotId = depot1.Id,
            ZoneId = zone1.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var address2 = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "2 Depot2 Street",
            City = "City2",
            State = "S2",
            PostalCode = "22222",
            CountryCode = "US",
        };

        var depot2 = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "Depot Two",
            AddressId = address2.Id,
            Address = address2,
            IsActive = true,
        };

        var zone2 = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "Zone Two",
            Boundary = TestsPolygonFactory.CreateDefault(),
            DepotId = depot2.Id,
            Depot = depot2,
            IsActive = true,
        };

        var vehicle2 = new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationPlate = "DEPOT2-001",
            DepotId = depot2.Id,
            Depot = depot2,
            Status = VehicleStatus.InUse,
            ParcelCapacity = 100,
            WeightCapacity = 1000,
        };

        var driver2 = new Driver
        {
            Id = Guid.NewGuid(),
            FirstName = "Driver",
            LastName = "Two",
            DepotId = depot2.Id,
            Depot = depot2,
            ZoneId = zone2.Id,
            Zone = zone2,
            Status = DriverStatus.Active,
            LicenseNumber = "LIC-D2",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            UserId = Guid.NewGuid(),
        };

        var shipper = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "10 Shipper Lane",
            City = "City2",
            State = "S2",
            PostalCode = "22223",
            CountryCode = "US",
        };

        var recipient = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "20 Recipient Road",
            City = "City2",
            State = "S2",
            PostalCode = "22224",
            CountryCode = "US",
            ContactName = "Recipient",
        };

        var parcel1 = CreateParcel("LM-TWODEP-001", ParcelStatus.Staged, shipper, recipient, zone2);
        var parcel2 = CreateParcel("LM-TWODEP-002", ParcelStatus.Staged, shipper, recipient, zone2);

        var route = new Route
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle2.Id,
            Vehicle = vehicle2,
            DriverId = driver2.Id,
            Driver = driver2,
            StartDate = DateTimeOffset.UtcNow,
            StagingArea = StagingArea.B,
            Status = RouteStatus.Planned,
            Parcels = [parcel1, parcel2],
        };

        db.AddRange(address1, depot1, zone1, vehicle1, driver1, operatorUser, address2, depot2, zone2, vehicle2, driver2, shipper, recipient, parcel1, parcel2, route);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(operatorUser.Id.ToString());
        currentUser.UserName.Returns(operatorUser.UserName);
        currentUser.Roles.Returns(["WarehouseOperator"]);

        var fixture = new QueryFixture(depot2, operatorUser, route, vehicle2, driver2);
        return (fixture, currentUser);
    }

    private static Parcel CreateParcel(
        string trackingNumber,
        ParcelStatus status,
        Address shipper,
        Address recipient,
        Zone zone) =>
        new()
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            Description = "Route load-out query test parcel",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddressId = shipper.Id,
            ShipperAddress = shipper,
            RecipientAddressId = recipient.Id,
            RecipientAddress = recipient,
            Weight = 1.0m,
            WeightUnit = WeightUnit.Kg,
            Length = 10,
            Width = 10,
            Height = 10,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 50m,
            Currency = "USD",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2),
            ZoneId = zone.Id,
            Zone = zone,
        };

    private sealed record QueryFixture(
        Depot Depot,
        ApplicationUser Operator,
        Route Route,
        Vehicle Vehicle,
        Driver Driver);
}
