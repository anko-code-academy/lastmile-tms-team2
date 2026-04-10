using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Infrastructure.Services;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Tests.Parcels;

public class ZoneMatchingServiceTests
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private static AppDbContext MakeDbContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    [Fact]
    public async Task FindZoneIdAsync_WhenZonesOverlap_ReturnsSmallestCoveringZone()
    {
        var db = MakeDbContext();
        var depot = new Depot
        {
            Name = "Chicago Central",
            Address = new Address
            {
                Street1 = "1 Depot Way",
                City = "Chicago",
                State = "IL",
                PostalCode = "60601",
                CountryCode = "US",
            },
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
        };

        var broadZone = new Zone
        {
            Name = "Test Zone",
            Boundary = CreatePolygon(
                (-87.6460, 41.8745),
                (-87.6180, 41.8745),
                (-87.6180, 41.8995),
                (-87.6460, 41.8995)),
            IsActive = true,
            Depot = depot,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
        };

        var preciseZone = new Zone
        {
            Name = "Near North Side",
            Boundary = CreatePolygon(
                (-87.6390, 41.8850),
                (-87.6260, 41.8850),
                (-87.6260, 41.8950),
                (-87.6390, 41.8950)),
            IsActive = true,
            Depot = depot,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Depots.Add(depot);
        db.Zones.AddRange(broadZone, preciseZone);
        await db.SaveChangesAsync();

        var sut = new ZoneMatchingService(db);
        var point = GeometryFactory.CreatePoint(new Coordinate(-87.6325, 41.8900));

        var zoneId = await sut.FindZoneIdAsync(point, CancellationToken.None);

        zoneId.Should().Be(preciseZone.Id);
    }

    private static Polygon CreatePolygon(
        (double Lon, double Lat) southWest,
        (double Lon, double Lat) southEast,
        (double Lon, double Lat) northEast,
        (double Lon, double Lat) northWest)
    {
        return GeometryFactory.CreatePolygon(
        [
            new Coordinate(southWest.Lon, southWest.Lat),
            new Coordinate(southEast.Lon, southEast.Lat),
            new Coordinate(northEast.Lon, northEast.Lat),
            new Coordinate(northWest.Lon, northWest.Lat),
            new Coordinate(southWest.Lon, southWest.Lat),
        ]);
    }
}
