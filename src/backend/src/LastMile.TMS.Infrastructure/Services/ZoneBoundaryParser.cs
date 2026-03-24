using System.Text.Json;
using LastMile.TMS.Application.Zones.Services;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace LastMile.TMS.Infrastructure.Services;

public class ZoneBoundaryParser : IZoneBoundaryParser
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);
    private static readonly WKTReader WktReader = new(GeometryFactory) { DefaultSRID = 4326 };

    public Polygon? ParseGeoJson(string geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var root = doc.RootElement;

            JsonElement polygonElement;

            // Navigate to the Polygon geometry
            if (root.ValueKind == JsonValueKind.Array)
            {
                // FeatureCollection — take first feature
                if (root.GetArrayLength() == 0)
                    return null;
                var feature = root[0];
                if (!feature.TryGetProperty("geometry", out polygonElement))
                    return null;
            }
            else if (root.TryGetProperty("geometry", out var geom))
            {
                polygonElement = geom;
            }
            else
            {
                // Raw Polygon (no wrapping Feature)
                polygonElement = root;
            }

            // Verify it is a Polygon
            if (!polygonElement.TryGetProperty("type", out var typeEl)
                || typeEl.ValueKind != JsonValueKind.String
                || typeEl.GetString() != "Polygon")
                return null;

            // Get coordinates — coordinates[0] is the exterior ring
            if (!polygonElement.TryGetProperty("coordinates", out var coordsArray))
                return null;
            if (coordsArray.ValueKind != JsonValueKind.Array)
                return null;
            if (coordsArray.GetArrayLength() == 0)
                return null;

            var exteriorRing = coordsArray[0];

            // Parse the ring
            var polygonCoords = ParseRing(exteriorRing);
            if (polygonCoords is null || polygonCoords.Count < 4)
                return null;

            var ring = GeometryFactory.CreateLinearRing(polygonCoords.ToArray());
            if (ring is null)
                return null;

            var polygon = GeometryFactory.CreatePolygon(ring);
            polygon.SRID = 4326;
            return polygon;
        }
        catch
        {
            return null;
        }
    }

    public Polygon? ParseCoordinates(List<List<double>> coordinates)
    {
        if (coordinates is null || coordinates.Count < 4)
            return null;

        try
        {
            var ringCoords = coordinates.ToList();

            // Ensure closed ring
            if (ringCoords.Count >= 2)
            {
                var first = ringCoords[0];
                var last = ringCoords[^1];
                if (first[0] != last[0] || first[1] != last[1])
                    ringCoords.Add(first);
            }

            var coords = ringCoords
                .Select(c => new Coordinate(c[0], c[1]))
                .ToArray();

            var ring = GeometryFactory.CreateLinearRing(coords);
            if (ring is null)
                return null;

            var polygon = GeometryFactory.CreatePolygon(ring);
            polygon.SRID = 4326;
            return polygon;
        }
        catch
        {
            return null;
        }
    }

    public Polygon? ParseWkt(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
            return null;

        try
        {
            var geometry = WktReader.Read(wkt);
            if (geometry is Polygon polygon)
            {
                polygon.SRID = 4326;
                return polygon;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static List<Coordinate>? ParseRing(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return null;

        var coords = new List<Coordinate>();
        foreach (var point in element.EnumerateArray())
        {
            // Each point is a [lon, lat] pair (an array of 2 numbers)
            if (point.ValueKind != JsonValueKind.Array)
                return null;
            var pair = point.EnumerateArray().ToList();
            if (pair.Count < 2)
                return null;
            // pair[0] = longitude, pair[1] = latitude
            coords.Add(new Coordinate(pair[0].GetDouble(), pair[1].GetDouble()));
        }

        return coords;
    }
}
