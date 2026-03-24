using FluentAssertions;
using LastMile.TMS.Infrastructure.Services;
using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Tests;

public class ZoneBoundaryParserTests
{
    private readonly ZoneBoundaryParser _sut = new();

    #region ParseGeoJson — raw Polygon

    [Fact]
    public void ParseGeoJson_RawPolygon_ReturnsPolygonWithCorrectSrid()
    {
        var geoJson = """
        {
          "type": "Polygon",
          "coordinates": [[
            [30.5, 50.1],
            [30.6, 50.1],
            [30.6, 50.2],
            [30.5, 50.2],
            [30.5, 50.1]
          ]]
        }
        """;

        var result = _sut.ParseGeoJson(geoJson);

        result.Should().NotBeNull();
        result.Should().BeOfType<Polygon>();
        result!.SRID.Should().Be(4326);
    }

    [Fact]
    public void ParseGeoJson_RawPolygon_ReturnsPolygonWithCorrectCoordinates()
    {
        var geoJson = """
        {
          "type": "Polygon",
          "coordinates": [[
            [30.5, 50.1],
            [30.6, 50.1],
            [30.6, 50.2],
            [30.5, 50.2],
            [30.5, 50.1]
          ]]
        }
        """;

        var result = _sut.ParseGeoJson(geoJson);

        result!.Coordinates.Should().HaveCount(5);
        result.Coordinates.First().X.Should().BeApproximately(30.5, 0.0001);
        result.Coordinates.First().Y.Should().BeApproximately(50.1, 0.0001);
    }

    #endregion

    #region ParseGeoJson — GeoJSON Feature with Polygon geometry

    [Fact]
    public void ParseGeoJson_FeatureWithPolygon_ReturnsPolygonWithCorrectSrid()
    {
        var geoJson = """
        {
          "type": "Feature",
          "geometry": {
            "type": "Polygon",
            "coordinates": [[
              [30.5, 50.1],
              [30.6, 50.1],
              [30.6, 50.2],
              [30.5, 50.2],
              [30.5, 50.1]
            ]]
          },
          "properties": { "name": "Zone A" }
        }
        """;

        var result = _sut.ParseGeoJson(geoJson);

        result.Should().NotBeNull();
        result.Should().BeOfType<Polygon>();
        result!.SRID.Should().Be(4326);
    }

    [Fact]
    public void ParseGeoJson_FeatureWithPolygon_ReturnsPolygonWithCorrectCoordinates()
    {
        var geoJson = """
        {
          "type": "Feature",
          "geometry": {
            "type": "Polygon",
            "coordinates": [[
              [30.5, 50.1],
              [30.6, 50.1],
              [30.6, 50.2],
              [30.5, 50.2],
              [30.5, 50.1]
            ]]
          },
          "properties": { "name": "Zone A" }
        }
        """;

        var result = _sut.ParseGeoJson(geoJson);

        result!.Coordinates.First().X.Should().BeApproximately(30.5, 0.0001);
        result.Coordinates.First().Y.Should().BeApproximately(50.1, 0.0001);
    }

    #endregion

    #region ParseGeoJson — invalid inputs

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseGeoJson_NullOrWhitespace_ReturnsNull(string? input)
    {
        var result = _sut.ParseGeoJson(input!);
        result.Should().BeNull();
    }

    [Fact]
    public void ParseGeoJson_EmptyObject_ReturnsNull()
    {
        var result = _sut.ParseGeoJson("{}");
        result.Should().BeNull();
    }

    [Fact]
    public void ParseGeoJson_ObjectWithNoCoordinates_ReturnsNull()
    {
        var result = _sut.ParseGeoJson("""{"type": "Polygon"}""");
        result.Should().BeNull();
    }

    [Fact]
    public void ParseGeoJson_EmptyFeatureCollection_ReturnsNull()
    {
        var result = _sut.ParseGeoJson("""{"type": "FeatureCollection", "features": []}""");
        result.Should().BeNull();
    }

    [Fact]
    public void ParseGeoJson_InvalidJson_ReturnsNull()
    {
        var result = _sut.ParseGeoJson("not valid json at all");
        result.Should().BeNull();
    }

    [Fact]
    public void ParseGeoJson_FewerThan4Points_ReturnsNull()
    {
        var geoJson = """
        {
          "type": "Polygon",
          "coordinates": [[
            [30.5, 50.1],
            [30.6, 50.1],
            [30.5, 50.1]
          ]]
        }
        """;
        var result = _sut.ParseGeoJson(geoJson);
        result.Should().BeNull();
    }

    #endregion

    #region ParseCoordinates — polygon vertices as [lon, lat] pairs

    [Fact]
    public void ParseCoordinates_ClosedRing_ReturnsPolygonWithCorrectSrid()
    {
        var coordinates = new List<List<double>>
        {
            new() { 30.5, 50.1 },
            new() { 30.6, 50.1 },
            new() { 30.6, 50.2 },
            new() { 30.5, 50.2 },
            new() { 30.5, 50.1 }
        };

        var result = _sut.ParseCoordinates(coordinates);

        result.Should().NotBeNull();
        result.Should().BeOfType<Polygon>();
        result!.SRID.Should().Be(4326);
    }

    [Fact]
    public void ParseCoordinates_UnclosedRing_AutoClosesAndReturnsPolygon()
    {
        var coordinates = new List<List<double>>
        {
            new() { 30.5, 50.1 },
            new() { 30.6, 50.1 },
            new() { 30.6, 50.2 },
            new() { 30.5, 50.2 }
        };

        var result = _sut.ParseCoordinates(coordinates);

        result.Should().NotBeNull();
        result.Should().BeOfType<Polygon>();
        result!.SRID.Should().Be(4326);
        result.Coordinates.Should().HaveCount(5); // 4 input + 1 auto-closed
    }

    [Fact]
    public void ParseCoordinates_UnclosedRing_AutoClosesWithCorrectFirstPoint()
    {
        var coordinates = new List<List<double>>
        {
            new() { 30.5, 50.1 },
            new() { 30.6, 50.1 },
            new() { 30.6, 50.2 },
            new() { 30.5, 50.2 }
        };

        var result = _sut.ParseCoordinates(coordinates);

        result!.Coordinates.First().X.Should().BeApproximately(30.5, 0.0001);
        result.Coordinates.Last().X.Should().BeApproximately(30.5, 0.0001);
    }

    [Theory]
    [InlineData(null)]
    public void ParseCoordinates_Null_ReturnsNull(List<List<double>>? input)
    {
        var result = _sut.ParseCoordinates(input!);
        result.Should().BeNull();
    }

    [Fact]
    public void ParseCoordinates_TooFewPoints_ReturnsNull()
    {
        var coordinates = new List<List<double>>
        {
            new() { 30.5, 50.1 },
            new() { 30.6, 50.1 }
        };
        var result = _sut.ParseCoordinates(coordinates);
        result.Should().BeNull();
    }

    #endregion

    #region ParseWkt

    [Fact]
    public void ParseWkt_ValidPolygonWkt_ReturnsPolygonWithCorrectSrid()
    {
        var wkt = "POLYGON ((30.5 50.1, 30.6 50.1, 30.6 50.2, 30.5 50.2, 30.5 50.1))";

        var result = _sut.ParseWkt(wkt);

        result.Should().NotBeNull();
        result.Should().BeOfType<Polygon>();
        result!.SRID.Should().Be(4326);
    }

    [Fact]
    public void ParseWkt_ValidPolygonWkt_ReturnsPolygonWithCorrectCoordinates()
    {
        var wkt = "POLYGON ((30.5 50.1, 30.6 50.1, 30.6 50.2, 30.5 50.2, 30.5 50.1))";

        var result = _sut.ParseWkt(wkt);

        result!.Coordinates.First().X.Should().BeApproximately(30.5, 0.0001);
        result.Coordinates.First().Y.Should().BeApproximately(50.1, 0.0001);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseWkt_NullOrWhitespace_ReturnsNull(string? input)
    {
        var result = _sut.ParseWkt(input!);
        result.Should().BeNull();
    }

    [Fact]
    public void ParseWkt_InvalidWkt_ReturnsNull()
    {
        var result = _sut.ParseWkt("NOT A VALID WKT STRING");
        result.Should().BeNull();
    }

    [Fact]
    public void ParseWkt_NonPolygonGeometry_ReturnsNull()
    {
        var wkt = "POINT (30.5 50.1)";
        var result = _sut.ParseWkt(wkt);
        result.Should().BeNull();
    }

    #endregion
}
