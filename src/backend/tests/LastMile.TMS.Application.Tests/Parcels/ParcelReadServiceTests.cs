using FluentAssertions;
using LastMile.TMS.Application.Parcels.Reads;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Tests.Parcels;

public class ParcelReadServiceTests
{
    private static readonly GeometryFactory GeoFactory = new(new PrecisionModel(), 4326);

    private static AppDbContext MakeDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    [Fact]
    public async Task GetParcelByIdAsync_ReturnsAggregateDetailWithSenderTimelineRouteAndProofOfDelivery()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedAggregateParcelAsync(db, includeRouteAssignment: true, includeProofOfDelivery: true);
        var service = new ParcelReadService(db);

        var result = await service.GetParcelByIdAsync(fixture.Parcel.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TrackingNumber.Should().Be(fixture.Parcel.TrackingNumber);
        result.SenderAddress.Street1.Should().Be("10 Shipper Lane");
        result.SenderAddress.ContactName.Should().Be("Sender Contact");
        result.RecipientAddress.ContactName.Should().Be("Recipient Contact");
        result.ChangeHistory.Should().ContainSingle(entry => entry.FieldName == "Description");
        result.StatusTimeline.Select(entry => entry.EventType).Should().ContainInOrder("OutForDelivery", "ArrivedAtFacility");
        result.RouteAssignment.Should().NotBeNull();
        result.RouteAssignment!.RouteId.Should().Be(fixture.InProgressRoute!.Id);
        result.RouteAssignment.DriverName.Should().Be("Jamie Driver");
        result.RouteAssignment.VehiclePlate.Should().Be("TST-100");
        result.ProofOfDelivery.Should().NotBeNull();
        result.ProofOfDelivery!.ReceivedBy.Should().Be("Front Desk");
        result.ProofOfDelivery.HasPhoto.Should().BeTrue();
        result.ProofOfDelivery.HasSignatureImage.Should().BeTrue();
    }

    [Fact]
    public async Task GetParcelByTrackingNumberAsync_ReturnsNullOptionalsWhenRouteAndPodAreMissing()
    {
        await using var db = MakeDbContext();
        var fixture = await SeedAggregateParcelAsync(db, includeRouteAssignment: false, includeProofOfDelivery: false);
        var service = new ParcelReadService(db);

        var result = await service.GetParcelByTrackingNumberAsync(fixture.Parcel.TrackingNumber, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(fixture.Parcel.Id);
        result.TrackingNumber.Should().Be(fixture.Parcel.TrackingNumber);
        result.SenderAddress.City.Should().Be("Sydney");
        result.RouteAssignment.Should().BeNull();
        result.ProofOfDelivery.Should().BeNull();
        result.StatusTimeline.Should().HaveCount(2);
    }

    private static async Task<ParcelAggregateFixture> SeedAggregateParcelAsync(
        AppDbContext db,
        bool includeRouteAssignment,
        bool includeProofOfDelivery)
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

        var shipperAddress = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "10 Shipper Lane",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2001",
            CountryCode = "AU",
            ContactName = "Sender Contact",
            Phone = "+61000000001",
            Email = "sender@example.com",
        };

        var recipientAddress = new Address
        {
            Id = Guid.NewGuid(),
            Street1 = "99 Recipient Road",
            City = "Melbourne",
            State = "VIC",
            PostalCode = "3000",
            CountryCode = "AU",
            ContactName = "Recipient Contact",
            Phone = "+61000000002",
            Email = "recipient@example.com",
        };

        var depot = new Depot
        {
            Id = Guid.NewGuid(),
            Name = "North Depot",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };

        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            Name = "North Zone",
            Boundary = MakePolygon(),
            DepotId = depot.Id,
            Depot = depot,
            IsActive = true,
        };

        var driverUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "driver@example.com",
            Email = "driver@example.com",
            FirstName = "Jamie",
            LastName = "Driver",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            FirstName = "Jamie",
            LastName = "Driver",
            LicenseNumber = "LIC-100",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(2),
            ZoneId = zone.Id,
            Zone = zone,
            DepotId = depot.Id,
            Depot = depot,
            UserId = driverUser.Id,
            User = driverUser,
            Status = DriverStatus.Active,
        };

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationPlate = "TST-100",
            Type = VehicleType.Van,
            ParcelCapacity = 50,
            WeightCapacity = 500m,
            Status = VehicleStatus.Available,
            DepotId = depot.Id,
            Depot = depot,
        };

        var parcel = new Parcel
        {
            Id = Guid.NewGuid(),
            TrackingNumber = "LMREADSERVICE001",
            Description = "Warehouse intake parcel",
            ServiceType = ServiceType.Standard,
            Status = ParcelStatus.OutForDelivery,
            ShipperAddressId = shipperAddress.Id,
            ShipperAddress = shipperAddress,
            RecipientAddressId = recipientAddress.Id,
            RecipientAddress = recipientAddress,
            Weight = 2.5m,
            WeightUnit = WeightUnit.Kg,
            Length = 20m,
            Width = 10m,
            Height = 5m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 100m,
            Currency = "AUD",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2),
            DeliveryAttempts = 1,
            ParcelType = "Box",
            ZoneId = zone.Id,
            Zone = zone,
            TrackingEvents =
            [
                new TrackingEvent
                {
                    Id = Guid.NewGuid(),
                    Timestamp = new DateTimeOffset(2026, 04, 06, 12, 0, 0, TimeSpan.Zero),
                    EventType = EventType.ArrivedAtFacility,
                    Description = "Parcel received at depot",
                    Location = "Sydney Central Depot",
                    Operator = "Warehouse User",
                },
                new TrackingEvent
                {
                    Id = Guid.NewGuid(),
                    Timestamp = new DateTimeOffset(2026, 04, 07, 7, 30, 0, TimeSpan.Zero),
                    EventType = EventType.OutForDelivery,
                    Description = "Parcel loaded onto route",
                    Location = "Dock 3",
                    Operator = "Dispatch User",
                },
            ],
            ChangeHistory =
            [
                new ParcelChangeHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    ParcelId = Guid.Empty,
                    Action = ParcelChangeAction.Updated,
                    FieldName = "Description",
                    BeforeValue = "Old description",
                    AfterValue = "Warehouse intake parcel",
                    ChangedAt = new DateTimeOffset(2026, 04, 06, 10, 0, 0, TimeSpan.Zero),
                    ChangedBy = "Warehouse User",
                },
            ],
        };

        parcel.ChangeHistory.Single().ParcelId = parcel.Id;

        Route? completedRoute = null;
        Route? plannedRoute = null;
        Route? inProgressRoute = null;

        if (includeRouteAssignment)
        {
            completedRoute = new Route
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle,
                StartDate = new DateTimeOffset(2026, 04, 05, 7, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2026, 04, 05, 11, 0, 0, TimeSpan.Zero),
                StartMileage = 100,
                EndMileage = 150,
                Status = RouteStatus.Completed,
                Parcels = [parcel],
            };

            plannedRoute = new Route
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle,
                StartDate = new DateTimeOffset(2026, 04, 08, 7, 0, 0, TimeSpan.Zero),
                EndDate = null,
                StartMileage = 150,
                EndMileage = 0,
            Status = RouteStatus.Draft,
                Parcels = [parcel],
            };

            inProgressRoute = new Route
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle,
                StartDate = new DateTimeOffset(2026, 04, 07, 6, 30, 0, TimeSpan.Zero),
                EndDate = null,
                StartMileage = 151,
                EndMileage = 0,
                Status = RouteStatus.InProgress,
                Parcels = [parcel],
            };
        }

        db.Addresses.AddRange(depotAddress, shipperAddress, recipientAddress);
        db.Depots.Add(depot);
        db.Zones.Add(zone);
        db.Users.Add(driverUser);
        db.Drivers.Add(driver);
        db.Vehicles.Add(vehicle);
        db.Parcels.Add(parcel);

        if (completedRoute is not null && plannedRoute is not null && inProgressRoute is not null)
        {
            db.Routes.AddRange(completedRoute, plannedRoute, inProgressRoute);
        }

        if (includeProofOfDelivery)
        {
            db.Add(new DeliveryConfirmation
            {
                Id = Guid.NewGuid(),
                ParcelId = parcel.Id,
                Parcel = parcel,
                ReceivedBy = "Front Desk",
                DeliveryLocation = "Building A",
                DeliveredAt = new DateTimeOffset(2026, 04, 07, 9, 45, 0, TimeSpan.Zero),
                SignatureImage = [1, 2, 3],
                Photo = [4, 5, 6],
            });
        }

        await db.SaveChangesAsync();

        return new ParcelAggregateFixture(parcel, inProgressRoute);
    }

    private static Polygon MakePolygon()
    {
        var polygon = GeoFactory.CreatePolygon(
            [
                new Coordinate(151.0, -33.0),
                new Coordinate(152.0, -33.0),
                new Coordinate(152.0, -34.0),
                new Coordinate(151.0, -34.0),
                new Coordinate(151.0, -33.0),
            ]);
        polygon.SRID = 4326;
        return polygon;
    }

    private sealed record ParcelAggregateFixture(Parcel Parcel, Route? InProgressRoute);
}
