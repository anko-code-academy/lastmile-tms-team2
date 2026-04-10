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

public class CompleteLoadOutTests
{
    private static AppDbContext MakeDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    [Fact]
    public async Task Handle_AllParcelsLoaded_CompletesAndTransitionsRoute()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, allLoaded: true);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var handler = new CompleteLoadOutCommandHandler(db, currentUser);

        var result = await handler.Handle(
            new CompleteLoadOutCommand(fixture.Route.Id, Force: false),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.LoadedCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
        result.TotalCount.Should().Be(2);

        var route = await db.Routes.FirstAsync(r => r.Id == fixture.Route.Id);
        route.Status.Should().Be(RouteStatus.InProgress);
    }

    [Fact]
    public async Task Handle_UnloadedParcels_NoForce_ReturnsShortLoadWarning()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, allLoaded: false);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var handler = new CompleteLoadOutCommandHandler(db, currentUser);

        var result = await handler.Handle(
            new CompleteLoadOutCommand(fixture.Route.Id, Force: false),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.SkippedCount.Should().BeGreaterThan(0);
        result.Message.Should().Contain("not been loaded");

        var route = await db.Routes.FirstAsync(r => r.Id == fixture.Route.Id);
        route.Status.Should().Be(RouteStatus.Planned);
    }

    [Fact]
    public async Task Handle_UnloadedParcels_Force_CompletesAnyway()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, allLoaded: false);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var handler = new CompleteLoadOutCommandHandler(db, currentUser);

        var result = await handler.Handle(
            new CompleteLoadOutCommand(fixture.Route.Id, Force: true),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.SkippedCount.Should().BeGreaterThan(0);

        var route = await db.Routes.FirstAsync(r => r.Id == fixture.Route.Id);
        route.Status.Should().Be(RouteStatus.InProgress);
    }

    [Fact]
    public async Task Handle_UnloadedParcels_Force_TransitionsStagedParcelsToException()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, allLoaded: false);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var handler = new CompleteLoadOutCommandHandler(db, currentUser);

        var stagedParcelsBefore = fixture.Route.Parcels.Where(p => p.Status == ParcelStatus.Staged).ToList();
        stagedParcelsBefore.Should().NotBeEmpty();

        var result = await handler.Handle(
            new CompleteLoadOutCommand(fixture.Route.Id, Force: true),
            CancellationToken.None);

        result.Success.Should().BeTrue();

        foreach (var parcel in stagedParcelsBefore)
        {
            var updatedParcel = await db.Parcels
                .Include(p => p.TrackingEvents)
                .FirstAsync(p => p.Id == parcel.Id);
            updatedParcel.Status.Should().Be(ParcelStatus.Exception);
            updatedParcel.TrackingEvents.Should().ContainSingle(e => e.EventType == Domain.Enums.EventType.Exception);
        }
    }

    [Fact]
    public async Task Handle_RouteNotFound_ReturnsFailure()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, allLoaded: true);
        var currentUser = CreateCurrentUser(fixture.Operator);
        var handler = new CompleteLoadOutCommandHandler(db, currentUser);

        var result = await handler.Handle(
            new CompleteLoadOutCommand(Guid.NewGuid(), Force: false),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_RouteAlreadyInProgress_ReturnsFailure()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedFixtureAsync(db, allLoaded: true);
        fixture.Route.Status = RouteStatus.InProgress;
        await db.SaveChangesAsync();

        var currentUser = CreateCurrentUser(fixture.Operator);
        var handler = new CompleteLoadOutCommandHandler(db, currentUser);

        var result = await handler.Handle(
            new CompleteLoadOutCommand(fixture.Route.Id, Force: false),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Planned");
    }

    private static ICurrentUserService CreateCurrentUser(ApplicationUser user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id.ToString());
        currentUser.UserName.Returns(user.UserName);
        currentUser.Roles.Returns(["WarehouseOperator"]);
        return currentUser;
    }

    private static async Task<CompleteFixture> SeedFixtureAsync(AppDbContext db, bool allLoaded)
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

        var parcel1Status = allLoaded ? ParcelStatus.Loaded : ParcelStatus.Staged;
        var parcel2Status = allLoaded ? ParcelStatus.Loaded : ParcelStatus.Staged;

        var parcel1 = CreateParcel("LM-COMP-001", parcel1Status, shipper, recipient, zone);
        var parcel2 = CreateParcel("LM-COMP-002", parcel2Status, shipper, recipient, zone);

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

        db.AddRange(
            depotAddress, depot, zone,
            vehicle, driver, operatorUser,
            shipper, recipient,
            parcel1, parcel2,
            route);

        await db.SaveChangesAsync();

        return new CompleteFixture(depot, operatorUser, route);
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
            Description = "Complete load-out test parcel",
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

    private sealed record CompleteFixture(
        Depot Depot,
        ApplicationUser Operator,
        Route Route);
}
