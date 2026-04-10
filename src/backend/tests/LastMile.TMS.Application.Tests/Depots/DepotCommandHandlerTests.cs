using FluentAssertions;
using LastMile.TMS.Application.Depots.Commands;
using LastMile.TMS.Application.Depots.DTOs;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Tests.Depots;

public sealed class DepotCommandHandlerTests
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private static AppDbContext MakeDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    [Fact]
    public async Task CreateDepot_GeocodesAndStoresAddressLocation()
    {
        var db = MakeDbContext();
        var point = CreatePoint(144.9631, -37.8136);
        var geocodingService = new RecordingGeocodingService(point);
        var handler = new CreateDepotCommandHandler(db, geocodingService);

        var result = await handler.Handle(
            new CreateDepotCommand(new CreateDepotDto
            {
                Name = "Central Depot",
                IsActive = true,
                Address = new AddressDto
                {
                    Street1 = "500 Collins Street",
                    City = "Melbourne",
                    State = "VIC",
                    PostalCode = "3000",
                    CountryCode = "au",
                },
            }),
            CancellationToken.None);

        geocodingService.RequestedAddresses.Should().ContainSingle()
            .Which.Should().Be("500 Collins Street, Melbourne, VIC, 3000, AU");
        result.Address.GeoLocation.Should().NotBeNull();
        result.Address.GeoLocation!.X.Should().BeApproximately(144.9631, 0.000001);
        result.Address.GeoLocation.Y.Should().BeApproximately(-37.8136, 0.000001);

        var persistedDepot = await db.Depots
            .Include(depot => depot.Address)
            .SingleAsync(depot => depot.Id == result.Id);

        persistedDepot.Address.CountryCode.Should().Be("AU");
        persistedDepot.Address.GeoLocation.Should().NotBeNull();
        persistedDepot.Address.GeoLocation!.X.Should().BeApproximately(144.9631, 0.000001);
        persistedDepot.Address.GeoLocation.Y.Should().BeApproximately(-37.8136, 0.000001);
    }

    [Fact]
    public async Task UpdateDepot_WithAddress_RegeocodesAndReplacesStoredLocation()
    {
        var db = MakeDbContext();
        var depot = await SeedDepotAsync(db);
        var point = CreatePoint(151.2093, -33.8688);
        var geocodingService = new RecordingGeocodingService(point);
        var handler = new UpdateDepotCommandHandler(db, geocodingService);

        var result = await handler.Handle(
            new UpdateDepotCommand(
                depot.Id,
                new UpdateDepotDto
                {
                    Name = "Sydney Depot",
                    IsActive = true,
                    Address = new AddressDto
                    {
                        Street1 = "123 Market Street",
                        City = "Sydney",
                        State = "NSW",
                        PostalCode = "2000",
                        CountryCode = "au",
                    },
                }),
            CancellationToken.None);

        result.Should().NotBeNull();
        geocodingService.RequestedAddresses.Should().ContainSingle()
            .Which.Should().Be("123 Market Street, Sydney, NSW, 2000, AU");

        var persistedDepot = await db.Depots
            .Include(currentDepot => currentDepot.Address)
            .SingleAsync(currentDepot => currentDepot.Id == depot.Id);

        persistedDepot.Address.Street1.Should().Be("123 Market Street");
        persistedDepot.Address.GeoLocation.Should().NotBeNull();
        persistedDepot.Address.GeoLocation!.X.Should().BeApproximately(151.2093, 0.000001);
        persistedDepot.Address.GeoLocation.Y.Should().BeApproximately(-33.8688, 0.000001);
    }

    [Fact]
    public async Task UpdateDepot_WithoutAddress_DoesNotGeocode()
    {
        var db = MakeDbContext();
        var depot = await SeedDepotAsync(db);
        var geocodingService = new RecordingGeocodingService(CreatePoint(151.2093, -33.8688));
        var handler = new UpdateDepotCommandHandler(db, geocodingService);

        var result = await handler.Handle(
            new UpdateDepotCommand(
                depot.Id,
                new UpdateDepotDto
                {
                    Name = "Renamed Depot",
                    IsActive = false,
                }),
            CancellationToken.None);

        result.Should().NotBeNull();
        geocodingService.RequestedAddresses.Should().BeEmpty();

        var persistedDepot = await db.Depots
            .Include(currentDepot => currentDepot.Address)
            .SingleAsync(currentDepot => currentDepot.Id == depot.Id);

        persistedDepot.Name.Should().Be("Renamed Depot");
        persistedDepot.IsActive.Should().BeFalse();
        persistedDepot.Address.GeoLocation.Should().NotBeNull();
        persistedDepot.Address.GeoLocation!.X.Should().BeApproximately(144.9631, 0.000001);
        persistedDepot.Address.GeoLocation.Y.Should().BeApproximately(-37.8136, 0.000001);
    }

    [Fact]
    public async Task UpdateDepot_WithUnchangedAddress_WhenGeocodingFails_KeepsStoredLocation()
    {
        var db = MakeDbContext();
        var depot = await SeedDepotAsync(db);
        var handler = new UpdateDepotCommandHandler(db, new ThrowingGeocodingService());

        var result = await handler.Handle(
            new UpdateDepotCommand(
                depot.Id,
                new UpdateDepotDto
                {
                    Name = "Seed Depot Updated",
                    IsActive = true,
                    Address = new AddressDto
                    {
                        Street1 = "101 Market Street",
                        City = "Melbourne",
                        State = "VIC",
                        PostalCode = "3000",
                        CountryCode = "AU",
                    },
                }),
            CancellationToken.None);

        result.Should().NotBeNull();

        var persistedDepot = await db.Depots
            .Include(currentDepot => currentDepot.Address)
            .SingleAsync(currentDepot => currentDepot.Id == depot.Id);

        persistedDepot.Address.GeoLocation.Should().NotBeNull();
        persistedDepot.Address.GeoLocation!.X.Should().BeApproximately(144.9631, 0.000001);
        persistedDepot.Address.GeoLocation.Y.Should().BeApproximately(-37.8136, 0.000001);
    }

    [Fact]
    public async Task UpdateDepot_WithChangedAddress_WhenGeocodingFails_ClearsStoredLocation()
    {
        var db = MakeDbContext();
        var depot = await SeedDepotAsync(db);
        var handler = new UpdateDepotCommandHandler(db, new ThrowingGeocodingService());

        var result = await handler.Handle(
            new UpdateDepotCommand(
                depot.Id,
                new UpdateDepotDto
                {
                    Name = "Sydney Depot",
                    IsActive = true,
                    Address = new AddressDto
                    {
                        Street1 = "123 Market Street",
                        City = "Sydney",
                        State = "NSW",
                        PostalCode = "2000",
                        CountryCode = "AU",
                    },
                }),
            CancellationToken.None);

        result.Should().NotBeNull();

        var persistedDepot = await db.Depots
            .Include(currentDepot => currentDepot.Address)
            .SingleAsync(currentDepot => currentDepot.Id == depot.Id);

        persistedDepot.Address.Street1.Should().Be("123 Market Street");
        persistedDepot.Address.GeoLocation.Should().BeNull();
    }

    private static async Task<Depot> SeedDepotAsync(AppDbContext db)
    {
        var depot = new Depot
        {
            Name = "Seed Depot",
            IsActive = true,
            Address = new Address
            {
                Street1 = "101 Market Street",
                City = "Melbourne",
                State = "VIC",
                PostalCode = "3000",
                CountryCode = "AU",
                GeoLocation = CreatePoint(144.9631, -37.8136),
            },
        };

        db.Depots.Add(depot);
        await db.SaveChangesAsync();

        return depot;
    }

    private static Point CreatePoint(double longitude, double latitude)
    {
        var point = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        point.SRID = 4326;
        return point;
    }

    private sealed class RecordingGeocodingService(Point? point) : IGeocodingService
    {
        public List<string> RequestedAddresses { get; } = [];

        public Task<Point?> GeocodeAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            RequestedAddresses.Add(address);
            return Task.FromResult(point);
        }
    }

    private sealed class ThrowingGeocodingService : IGeocodingService
    {
        public Task<Point?> GeocodeAsync(
            string address,
            CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("Mapbox is unavailable.");
    }
}
