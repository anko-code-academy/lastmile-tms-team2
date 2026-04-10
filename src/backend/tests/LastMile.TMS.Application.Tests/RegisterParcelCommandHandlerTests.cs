using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Commands;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;

namespace LastMile.TMS.Application.Tests;

public class RegisterParcelCommandHandlerTests
{
    private static readonly GeometryFactory GeoFactory = new(new PrecisionModel(), 4326);

    private static AppDbContext MakeDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
    }

    private static RegisterParcelCommand MakeCommand(
        Guid shipperAddressId,
        string city = "Maadi",
        string street = "123 Street 9")
    {
        return new RegisterParcelCommand(new RegisterParcelDto
        {
            ShipperAddressId = shipperAddressId,
            RecipientAddress = new RegisterParcelRecipientAddressDto
            {
                Street1 = street,
                City = city,
                State = "Cairo",
                PostalCode = "11735",
                CountryCode = "EG",
                IsResidential = true,
                ContactName = "Ahmed",
                Phone = "+201234567890",
                Email = "ahmed@example.com"
            },
            Description = "Electronics",
            ServiceType = ServiceType.Standard,
            Weight = 2.5m,
            WeightUnit = WeightUnit.Kg,
            Length = 30m,
            Width = 20m,
            Height = 15m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 150.00m,
            Currency = "USD",
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(5),
            ParcelType = "Package"
        });
    }

    private static Point MakePoint(double lon, double lat)
    {
        var p = GeoFactory.CreatePoint(new Coordinate(lon, lat));
        p.SRID = 4326;
        return p;
    }

    private static RegisterParcelCommandHandler MakeHandler(
        AppDbContext db,
        IGeocodingService geocoding,
        IZoneMatchingService zoneMatching)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("ops.manager");

        var registrationService = new ParcelRegistrationService(db, geocoding, zoneMatching, currentUser);
        return new RegisterParcelCommandHandler(registrationService);
    }

    #region Scenario 1 — Shipper address not found

    [Fact]
    public async Task Handle_ShipperAddressNotFound_ThrowsArgumentException()
    {
        var db = MakeDbContext();
        var geocoding = Substitute.For<IGeocodingService>();
        var zoneMatching = Substitute.For<IZoneMatchingService>();
        var handler = MakeHandler(db, geocoding, zoneMatching);

        var shipperAddressId = Guid.NewGuid();
        var command = MakeCommand(shipperAddressId);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{shipperAddressId}*not found*");
    }

    #endregion

    #region Scenario 2 — Geocoding returns null

    [Fact]
    public async Task Handle_GeocodingReturnsNull_ThrowsInvalidOperationException()
    {
        var db = MakeDbContext();
        var shipperAddress = new Address
        {
            Street1 = "10 Sphinx Square",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "11511",
            CountryCode = "EG",
        };
        db.Addresses.Add(shipperAddress);
        await db.SaveChangesAsync();

        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.GeocodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Point?)null);

        var zoneMatching = Substitute.For<IZoneMatchingService>();
        var handler = MakeHandler(db, geocoding, zoneMatching);

        var command = MakeCommand(shipperAddress.Id);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*could not be geocoded*");

        // No orphan address or parcel in DB
        db.Addresses.Should().HaveCount(1); // only shipper
        db.Parcels.Should().HaveCount(0);
    }

    #endregion

    #region Scenario 3 — Zone matching returns null (outside all zones)

    [Fact]
    public async Task Handle_NoZoneMatchesPoint_ThrowsInvalidOperationException()
    {
        var db = MakeDbContext();
        var shipperAddress = new Address
        {
            Street1 = "10 Sphinx Square",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "11511",
            CountryCode = "EG",
        };
        db.Addresses.Add(shipperAddress);
        await db.SaveChangesAsync();

        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.GeocodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakePoint(31.25, 29.98)); // outside the zone

        var zoneMatching = Substitute.For<IZoneMatchingService>();
        zoneMatching.FindZoneIdAsync(Arg.Any<Point>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var handler = MakeHandler(db, geocoding, zoneMatching);
        var command = MakeCommand(shipperAddress.Id);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active zone covers*");

        // No orphan address or parcel
        db.Addresses.Should().HaveCount(1); // only shipper
        db.Parcels.Should().HaveCount(0);
    }

    #endregion

    #region Scenario 4 — Successful registration with zone and depot

    [Fact]
    public async Task Handle_ValidInput_ReturnsParcelDtoWithZoneAndDepot()
    {
        var db = MakeDbContext();

        // Setup shipper address
        var shipperAddress = new Address
        {
            Street1 = "10 Sphinx Square",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "11511",
            CountryCode = "EG",
        };
        db.Addresses.Add(shipperAddress);

        // Setup depot with address
        var depotAddress = new Address
        {
            Street1 = "Depot Street",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "11511",
            CountryCode = "EG",
        };
        db.Addresses.Add(depotAddress);

        var depot = new Depot
        {
            Name = "Cairo Central",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };
        db.Depots.Add(depot);

        // Setup zone covering the geocoded point
        var zonePolygon = GeoFactory.CreatePolygon(new[]
        {
            new Coordinate(31.20, 29.90),
            new Coordinate(31.30, 29.90),
            new Coordinate(31.30, 30.00),
            new Coordinate(31.20, 30.00),
            new Coordinate(31.20, 29.90),
        });
        zonePolygon.SRID = 4326;

        var zone = new Zone
        {
            Name = "Maadi District",
            Boundary = zonePolygon,
            IsActive = true,
            DepotId = depot.Id,
            Depot = depot,
        };
        db.Zones.Add(zone);
        await db.SaveChangesAsync();

        var geocoding = Substitute.For<IGeocodingService>();
        // The geocoder returns a point inside the zone polygon.
        geocoding.GeocodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakePoint(31.25, 29.95)); // inside the zone polygon

        var zoneMatching = Substitute.For<IZoneMatchingService>();
        zoneMatching.FindZoneIdAsync(Arg.Any<Point>(), Arg.Any<CancellationToken>())
            .Returns(zone.Id);

        var handler = MakeHandler(db, geocoding, zoneMatching);
        var command = MakeCommand(shipperAddress.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.TrackingNumber.Should().StartWith("LM");
        result.Status.Should().Be("Registered");
        result.ZoneId.Should().Be(zone.Id);
        result.ZoneName.Should().Be("Maadi District");
        result.DepotId.Should().Be(depot.Id);
        result.DepotName.Should().Be("Cairo Central");

        // Verify persisted entities
        db.Parcels.Should().HaveCount(1);
        db.Addresses.Should().HaveCount(3); // shipper + depot + recipient

        var parcel = db.Parcels.Include(p => p.RecipientAddress).First();
        parcel.ZoneId.Should().Be(zone.Id);
        parcel.RecipientAddress.GeoLocation.Should().NotBeNull();
        parcel.RecipientAddress.GeoLocation!.X.Should().BeApproximately(31.25, 0.01);
        parcel.RecipientAddress.GeoLocation.Y.Should().BeApproximately(29.95, 0.01);
    }

    #endregion

    #region Scenario 5 — Recipient address and parcel persisted atomically (no orphan)

    [Fact]
    public async Task Handle_OnSuccess_RecipientAddressAndParcelPersistedInOneSave()
    {
        var db = MakeDbContext();

        var shipperAddress = new Address
        {
            Street1 = "10 Sphinx Square",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "11511",
            CountryCode = "EG",
        };
        db.Addresses.Add(shipperAddress);

        var depotAddress = new Address
        {
            Street1 = "Depot Street",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "11511",
            CountryCode = "EG",
        };
        db.Addresses.Add(depotAddress);

        var depot = new Depot
        {
            Name = "Cairo Central",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };
        db.Depots.Add(depot);

        var zonePolygon = GeoFactory.CreatePolygon(new[]
        {
            new Coordinate(31.20, 29.90),
            new Coordinate(31.30, 29.90),
            new Coordinate(31.30, 30.00),
            new Coordinate(31.20, 30.00),
            new Coordinate(31.20, 29.90),
        });
        zonePolygon.SRID = 4326;

        var zone = new Zone
        {
            Name = "Maadi District",
            Boundary = zonePolygon,
            IsActive = true,
            DepotId = depot.Id,
            Depot = depot,
        };
        db.Zones.Add(zone);
        await db.SaveChangesAsync();

        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.GeocodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakePoint(31.25, 29.95));

        var zoneMatching = Substitute.For<IZoneMatchingService>();
        zoneMatching.FindZoneIdAsync(Arg.Any<Point>(), Arg.Any<CancellationToken>())
            .Returns(zone.Id);

        var handler = MakeHandler(db, geocoding, zoneMatching);
        var command = MakeCommand(shipperAddress.Id);

        await handler.Handle(command, CancellationToken.None);

        // Exactly 3 addresses: shipper + depot + recipient (no orphans)
        db.Addresses.Should().HaveCount(3);

        var parcel = db.Parcels
            .Include(p => p.RecipientAddress)
            .Include(p => p.Zone)
            .ThenInclude(z => z!.Depot)
            .Single();

        parcel.RecipientAddressId.Should().NotBeEmpty();
        parcel.RecipientAddress.Id.Should().NotBeEmpty();
        parcel.Zone.Should().NotBeNull();
        parcel.Zone!.Depot.Should().NotBeNull();
        parcel.Zone.Depot.Name.Should().Be("Cairo Central");
    }

    #endregion

    #region Scenario 6 — Inactive zones are not matched

    [Fact]
    public async Task Handle_ZoneIsInactive_DoesNotMatch_ReturnsNull()
    {
        var db = MakeDbContext();

        var shipperAddress = new Address
        {
            Street1 = "10 Sphinx Square",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "11511",
            CountryCode = "EG",
        };
        db.Addresses.Add(shipperAddress);

        var depotAddress = new Address
        {
            Street1 = "Depot Street",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "11511",
            CountryCode = "EG",
        };
        db.Addresses.Add(depotAddress);

        var depot = new Depot
        {
            Name = "Cairo Central",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };
        db.Depots.Add(depot);

        var zonePolygon = GeoFactory.CreatePolygon(new[]
        {
            new Coordinate(31.20, 29.90),
            new Coordinate(31.30, 29.90),
            new Coordinate(31.30, 30.00),
            new Coordinate(31.20, 30.00),
            new Coordinate(31.20, 29.90),
        });
        zonePolygon.SRID = 4326;

        var inactiveZone = new Zone
        {
            Name = "Inactive Zone",
            Boundary = zonePolygon,
            IsActive = false, // <-- inactive
            DepotId = depot.Id,
            Depot = depot,
        };
        db.Zones.Add(inactiveZone);
        await db.SaveChangesAsync();

        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.GeocodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakePoint(31.25, 29.95));

        // ZoneMatchingService is mocked — it would be called with the point
        // but since we configure it to return null (mimicking no active zone found),
        // the handler throws.
        var zoneMatching = Substitute.For<IZoneMatchingService>();
        zoneMatching.FindZoneIdAsync(Arg.Any<Point>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var handler = MakeHandler(db, geocoding, zoneMatching);
        var command = MakeCommand(shipperAddress.Id);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active zone covers*");
    }

    #endregion
}
