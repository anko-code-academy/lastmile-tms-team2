using FluentAssertions;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Parcels;

public class ParcelRegistrationServiceTests
{
    private static readonly GeometryFactory GeoFactory = new(new PrecisionModel(), 4326);

    private static AppDbContext MakeDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static Point MakePoint(double lon, double lat)
    {
        var point = GeoFactory.CreatePoint(new Coordinate(lon, lat));
        point.SRID = 4326;
        return point;
    }

    [Fact]
    public async Task RegisterAsync_ValidInput_ReturnsParcelDtoWithZoneAndDepot()
    {
        var db = MakeDbContext();

        var shipperAddress = new Address
        {
            Street1 = "10 Shipper Lane",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
        };
        db.Addresses.Add(shipperAddress);

        var depotAddress = new Address
        {
            Street1 = "1 Depot Road",
            City = "Sydney",
            State = "NSW",
            PostalCode = "2000",
            CountryCode = "AU",
        };
        db.Addresses.Add(depotAddress);

        var depot = new Depot
        {
            Name = "Sydney Central",
            AddressId = depotAddress.Id,
            Address = depotAddress,
            IsActive = true,
        };
        db.Depots.Add(depot);

        var zonePolygon = GeoFactory.CreatePolygon(
        [
            new Coordinate(151.0, -33.0),
            new Coordinate(152.0, -33.0),
            new Coordinate(152.0, -34.0),
            new Coordinate(151.0, -34.0),
            new Coordinate(151.0, -33.0),
        ]);
        zonePolygon.SRID = 4326;

        var zone = new Zone
        {
            Name = "Sydney Metro",
            Boundary = zonePolygon,
            IsActive = true,
            DepotId = depot.Id,
            Depot = depot,
        };
        db.Zones.Add(zone);
        await db.SaveChangesAsync();

        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.GeocodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakePoint(151.3, -33.8));

        var zoneMatching = Substitute.For<IZoneMatchingService>();
        zoneMatching.FindZoneIdAsync(Arg.Any<Point>(), Arg.Any<CancellationToken>())
            .Returns(zone.Id);

        var currentUser = Substitute.For<LastMile.TMS.Application.Common.Interfaces.ICurrentUserService>();
        currentUser.UserName.Returns("ops.manager");

        var service = new ParcelRegistrationService(db, geocoding, zoneMatching, currentUser);

        var result = await service.RegisterAsync(
            new RegisterParcelDto
            {
                ShipperAddressId = shipperAddress.Id,
                RecipientAddress = new RegisterParcelRecipientAddressDto
                {
                    Street1 = "15 George Street",
                    City = "Sydney",
                    State = "NSW",
                    PostalCode = "2000",
                    CountryCode = "AU",
                    IsResidential = true,
                    ContactName = "Taylor Smith",
                    Phone = "+61000000000",
                    Email = "taylor@example.com",
                },
                Description = "Electronics",
                ServiceType = ServiceType.Standard,
                Weight = 2.5m,
                WeightUnit = WeightUnit.Kg,
                Length = 20m,
                Width = 10m,
                Height = 5m,
                DimensionUnit = DimensionUnit.Cm,
                DeclaredValue = 150m,
                Currency = "AUD",
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(5),
                ParcelType = "Package",
            },
            CancellationToken.None);

        result.Should().NotBeNull();
        result.TrackingNumber.Should().StartWith("LM");
        result.Status.Should().Be("Registered");
        result.ZoneId.Should().Be(zone.Id);
        result.ZoneName.Should().Be("Sydney Metro");
        result.DepotId.Should().Be(depot.Id);
        result.DepotName.Should().Be("Sydney Central");

        var parcel = await db.Parcels
            .Include(x => x.RecipientAddress)
            .Include(x => x.TrackingEvents)
            .SingleAsync();

        parcel.ParcelImportId.Should().BeNull();
        parcel.RecipientAddress.GeoLocation.Should().NotBeNull();
        parcel.CreatedBy.Should().Be("ops.manager");
        parcel.TrackingEvents.Should().ContainSingle();
        var ev = parcel.TrackingEvents.Single();
        ev.EventType.Should().Be(EventType.LabelCreated);
        ev.Operator.Should().Be("ops.manager");
        ev.Location.Should().Be("Sydney Central");
        ev.Description.Should().Be("Parcel registered");
    }
}
