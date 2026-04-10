using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Commands;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Parcels;

public class LoadParcelForRouteTests
{
    private static AppDbContext MakeDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    [Fact]
    public async Task Handle_StagedParcelOnRoute_TransitionsToLoaded()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new LoadParcelForRouteCommandHandler(db, currentUser, notifier);

        var result = await handler.Handle(
            new LoadParcelForRouteCommand(fixture.Route.Id, fixture.StagedParcel.TrackingNumber),
            CancellationToken.None);

        result.Outcome.Should().Be(RouteLoadOutScanOutcome.Loaded);
        result.ParcelId.Should().Be(fixture.StagedParcel.Id);

        var parcel = await db.Parcels.Include(p => p.TrackingEvents)
            .FirstAsync(p => p.Id == fixture.StagedParcel.Id);
        parcel.Status.Should().Be(ParcelStatus.Loaded);
        parcel.TrackingEvents.Should().ContainSingle();
        parcel.LastModifiedBy.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_AlreadyLoadedParcel_ReturnsAlreadyLoaded()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new LoadParcelForRouteCommandHandler(db, currentUser, notifier);

        await handler.Handle(
            new LoadParcelForRouteCommand(fixture.Route.Id, fixture.StagedParcel.TrackingNumber),
            CancellationToken.None);

        var result = await handler.Handle(
            new LoadParcelForRouteCommand(fixture.Route.Id, fixture.StagedParcel.TrackingNumber),
            CancellationToken.None);

        result.Outcome.Should().Be(RouteLoadOutScanOutcome.AlreadyLoaded);
    }

    [Fact]
    public async Task Handle_InvalidStatusParcel_ReturnsInvalidStatus()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new LoadParcelForRouteCommandHandler(db, currentUser, notifier);

        var result = await handler.Handle(
            new LoadParcelForRouteCommand(fixture.Route.Id, fixture.SortedParcel.TrackingNumber),
            CancellationToken.None);

        result.Outcome.Should().Be(RouteLoadOutScanOutcome.InvalidStatus);
    }

    [Fact]
    public async Task Handle_ParbelOnDifferentRoute_ReturnsWrongRoute()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new LoadParcelForRouteCommandHandler(db, currentUser, notifier);

        var result = await handler.Handle(
            new LoadParcelForRouteCommand(fixture.Route.Id, fixture.OtherRouteParcel.TrackingNumber),
            CancellationToken.None);

        result.Outcome.Should().Be(RouteLoadOutScanOutcome.WrongRoute);
        result.ConflictingRouteId.Should().Be(fixture.OtherRoute.Id);
    }

    [Fact]
    public async Task Handle_UnknownParcel_ReturnsNotExpected()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new LoadParcelForRouteCommandHandler(db, currentUser, notifier);

        var result = await handler.Handle(
            new LoadParcelForRouteCommand(fixture.Route.Id, "UNKNOWN-999"),
            CancellationToken.None);

        result.Outcome.Should().Be(RouteLoadOutScanOutcome.NotExpected);
    }

    [Fact]
    public async Task Handle_RouteNotFound_ReturnsNotExpected()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new LoadParcelForRouteCommandHandler(db, currentUser, notifier);

        var result = await handler.Handle(
            new LoadParcelForRouteCommand(Guid.NewGuid(), fixture.StagedParcel.TrackingNumber),
            CancellationToken.None);

        result.Outcome.Should().Be(RouteLoadOutScanOutcome.NotExpected);
    }

    [Fact]
    public async Task Handle_RouteWithNoStagedOrLoadedParcels_ReturnsNotExpected()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedNoEligibleParcelsFixtureAsync(db);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new LoadParcelForRouteCommandHandler(db, currentUser, notifier);

        var result = await handler.Handle(
            new LoadParcelForRouteCommand(fixture.Route.Id, fixture.Parcel.TrackingNumber),
            CancellationToken.None);

        result.Outcome.Should().Be(RouteLoadOutScanOutcome.NotExpected);
        result.Message.Should().Contain("Route was not found");
    }

    [Fact]
    public async Task Handle_BoardIsRefreshedAfterScan()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var notifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new LoadParcelForRouteCommandHandler(db, currentUser, notifier);

        var result = await handler.Handle(
            new LoadParcelForRouteCommand(fixture.Route.Id, fixture.StagedParcel.TrackingNumber),
            CancellationToken.None);

        result.Board.Should().NotBeNull();
        result.Board.Id.Should().Be(fixture.Route.Id);
        result.Board.LoadedParcelCount.Should().Be(1);
        result.Board.RemainingParcelCount.Should().Be(0);
    }

    private static ICurrentUserService CreateCurrentUser(ApplicationUser user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id.ToString());
        currentUser.UserName.Returns(user.UserName);
        currentUser.Roles.Returns(["WarehouseOperator"]);
        return currentUser;
    }

    private static async Task<LoadOutFixture> SeedFixtureAsync(AppDbContext db)
    {
        var depotAddress = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "1 Depot Street",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
        };

        var depot = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "Sydney Load-Out Depot",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };

        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "Sydney Zone",
            Boundary = TestsPolygonFactory.CreateDefault(),
            DepotId = depot.Id,
            Depot = depot,
            IsActive = true,
        };

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationPlate = "LOAD-001",
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
            LastName = "Driver",
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
            UserName = "warehouse.operator@example.com",
            Email = "warehouse.operator@example.com",
            FirstName = "Warehouse",
            LastName = "Operator",
            IsActive = true,
            DepotId = depot.Id,
            ZoneId = zone.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var shipper = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "10 Shipper Lane",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2001",
            CountryCode = "AU",
        };

        var recipient = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "20 Recipient Road",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2002",
            CountryCode = "AU",
            ContactName = "Recipient",
        };

        var stagedParcel = CreateParcel("LM-LOAD-001", ParcelStatus.Staged, shipper, recipient, zone);
        var sortedParcel = CreateParcel("LM-LOAD-002", ParcelStatus.Sorted, shipper, recipient, zone);
        var otherRouteParcel = CreateParcel("LM-LOAD-099", ParcelStatus.Staged, shipper, recipient, zone);

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
            Parcels = [stagedParcel, sortedParcel],
        };

        var otherRoute = new Route
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            DriverId = driver.Id,
            Driver = driver,
            StartDate = DateTimeOffset.UtcNow,
            StagingArea = StagingArea.B,
            Status = RouteStatus.Planned,
            Parcels = [otherRouteParcel],
        };

        db.AddRange(
            depotAddress, depot, zone,
            vehicle, driver, operatorUser,
            shipper, recipient,
            stagedParcel, sortedParcel, otherRouteParcel,
            route, otherRoute);

        await db.SaveChangesAsync();

        return new LoadOutFixture(
            depot, operatorUser, vehicle, driver,
            route, otherRoute,
            stagedParcel, sortedParcel, otherRouteParcel);
    }

    private static async Task<NoEligibleParcelsFixture> SeedNoEligibleParcelsFixtureAsync(AppDbContext db)
    {
        var depotAddress = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "1 Depot Street",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
        };

        var depot = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "Sydney No-Eligible Depot",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };

        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "Sydney Zone",
            Boundary = TestsPolygonFactory.CreateDefault(),
            DepotId = depot.Id,
            Depot = depot,
            IsActive = true,
        };

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationPlate = "NOELIG-001",
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
            LastName = "Driver",
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
            UserName = "warehouse.operator@example.com",
            Email = "warehouse.operator@example.com",
            FirstName = "Warehouse",
            LastName = "Operator",
            IsActive = true,
            DepotId = depot.Id,
            ZoneId = zone.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var shipper = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "10 Shipper Lane",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2001",
            CountryCode = "AU",
        };

        var recipient = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "20 Recipient Road",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2002",
            CountryCode = "AU",
            ContactName = "Recipient",
        };

        // Parcel is in Sorted status — not Staged or Loaded, so route is ineligible for load-out
        var parcel = CreateParcel("LM-NOELIG-001", ParcelStatus.Sorted, shipper, recipient, zone);

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
            Parcels = [parcel],
        };

        db.AddRange(
            depotAddress, depot, zone,
            vehicle, driver, operatorUser,
            shipper, recipient,
            parcel, route);

        await db.SaveChangesAsync();

        return new NoEligibleParcelsFixture(depot, operatorUser, route, parcel);
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
            Description = "Load-out test parcel",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddressId = shipper.Id,
            ShipperAddress = shipper,
            RecipientAddressId = recipient.Id,
            RecipientAddress = recipient,
            Weight = 1.2m,
            WeightUnit = WeightUnit.Kg,
            Length = 20,
            Width = 10,
            Height = 5,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 50m,
            Currency = "AUD",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2),
            ZoneId = zone.Id,
            Zone = zone,
        };

    private sealed record LoadOutFixture(
        Depot Depot,
        ApplicationUser Operator,
        Vehicle Vehicle,
        Driver Driver,
        Route Route,
        Route OtherRoute,
        Parcel StagedParcel,
        Parcel SortedParcel,
        Parcel OtherRouteParcel);

    private sealed record NoEligibleParcelsFixture(
        Depot Depot,
        ApplicationUser Operator,
        Route Route,
        Parcel Parcel);
}
