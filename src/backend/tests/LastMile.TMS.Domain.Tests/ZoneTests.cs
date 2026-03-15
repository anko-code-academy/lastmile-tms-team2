using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using NetTopologySuite.Geometries;
using NetTopologySuite;

namespace LastMile.TMS.Domain.Tests;

public class ZoneTests
{
    private static readonly GeometryFactory GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    [Fact]
    public void Zone_ShouldInitializeWithDefaultValues()
    {
        var zone = new Zone();

        zone.Name.Should().Be(string.Empty);
        zone.IsActive.Should().BeTrue();
        zone.Boundary.Should().BeNull();
    }

    [Fact]
    public void Zone_ShouldHaveCorrectBaseType()
    {
        var zone = new Zone { Id = Guid.NewGuid() };

        zone.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Zone_ShouldAllowSettingProperties()
    {
        var depot = new Depot { Id = Guid.NewGuid(), Name = "Test Depot" };
        var zone = new Zone
        {
            Name = "Sydney Central",
            IsActive = true,
            DepotId = depot.Id,
            Depot = depot
        };

        zone.Name.Should().Be("Sydney Central");
        zone.IsActive.Should().BeTrue();
        zone.DepotId.Should().Be(depot.Id);
        zone.Depot.Should().NotBeNull();
        zone.Depot.Name.Should().Be("Test Depot");
    }

    [Fact]
    public void Zone_ShouldAllowSettingBoundaryGeometry()
    {
        var coordinates = new[]
        {
            new Coordinate(151.0, -33.0),
            new Coordinate(152.0, -33.0),
            new Coordinate(152.0, -34.0),
            new Coordinate(151.0, -34.0),
            new Coordinate(151.0, -33.0)
        };

        var polygon = GeometryFactory.CreatePolygon(coordinates);
        var zone = new Zone
        {
            Name = "Test Zone",
            Boundary = polygon
        };

        zone.Boundary.Should().NotBeNull();
        zone.Boundary!.GeometryType.Should().Be("Polygon");
    }

    [Fact]
    public void Zone_ShouldAllowDeactivation()
    {
        var zone = new Zone
        {
            Name = "Active Zone",
            IsActive = true
        };

        zone.IsActive.Should().BeTrue();

        zone.IsActive = false;

        zone.IsActive.Should().BeFalse();
    }
}