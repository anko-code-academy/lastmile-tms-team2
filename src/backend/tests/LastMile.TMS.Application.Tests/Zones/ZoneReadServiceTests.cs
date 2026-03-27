using FluentAssertions;
using LastMile.TMS.Application.Zones.DTOs;
using LastMile.TMS.Application.Zones.Reads;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Tests.Zones;

public class ZoneReadServiceTests
{
    private static readonly GeometryFactory GeoFactory = new(new PrecisionModel(), 4326);

    private static AppDbContext MakeDbContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
    }

    private static async Task<(Zone Zone, Depot Depot)> SeedZone(
        AppDbContext db,
        string zoneName,
        string depotName = "Test Depot")
    {
        var address = new Address
        {
            Street1 = "1 Test Street",
            City = "Cairo",
            State = "Cairo",
            PostalCode = "12345",
            CountryCode = "EG",
        };
        db.Addresses.Add(address);

        var depot = new Depot
        {
            Name = depotName,
            AddressId = address.Id,
            Address = address,
            IsActive = true,
        };
        db.Depots.Add(depot);

        var polygon = GeoFactory.CreatePolygon(new[]
        {
            new Coordinate(31.20, 29.90),
            new Coordinate(31.30, 29.90),
            new Coordinate(31.30, 30.00),
            new Coordinate(31.20, 30.00),
            new Coordinate(31.20, 29.90),
        });
        polygon.SRID = 4326;

        var zone = new Zone
        {
            Name = zoneName,
            Boundary = polygon,
            IsActive = true,
            DepotId = depot.Id,
            Depot = depot,
        };
        db.Zones.Add(zone);
        await db.SaveChangesAsync();
        return (zone, depot);
    }

    [Fact]
    public async Task GetZones_ReturnsAllZonesWithDepotName()
    {
        var db = MakeDbContext();
        var (zone, depot) = await SeedZone(db, "Maadi District");

        var service = new ZoneReadService(db);
        var result = await service.GetZones().ToListAsync();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(zone.Id);
        result[0].Name.Should().Be("Maadi District");
        result[0].DepotId.Should().Be(depot.Id);
        result[0].DepotName.Should().Be("Test Depot");
        result[0].IsActive.Should().BeTrue();
        result[0].Boundary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetZones_Boundary_IsWktString()
    {
        var db = MakeDbContext();
        await SeedZone(db, "Maadi");

        var service = new ZoneReadService(db);
        var result = await service.GetZones().ToListAsync();

        result[0].Boundary.Should().StartWith("POLYGON");
    }

    [Fact]
    public async Task GetZones_ReturnsMultipleZones()
    {
        var db = MakeDbContext();
        await SeedZone(db, "Zone A", "Depot A");
        await SeedZone(db, "Zone B", "Depot B");

        var service = new ZoneReadService(db);
        var result = await service.GetZones().ToListAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetZoneByIdAsync_ReturnsZoneOrNull()
    {
        var db = MakeDbContext();
        var (zone, _) = await SeedZone(db, "Maadi");

        var service = new ZoneReadService(db);

        var found = await service.GetZoneByIdAsync(zone.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Maadi");

        var notFound = await service.GetZoneByIdAsync(Guid.NewGuid());
        notFound.Should().BeNull();
    }
}
