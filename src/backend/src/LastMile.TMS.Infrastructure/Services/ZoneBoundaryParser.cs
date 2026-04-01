using System.Text.Json;
using LastMile.TMS.Application.Zones.Services;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace LastMile.TMS.Infrastructure.Services;

public class ZoneBoundaryParser : IZoneBoundaryParser
{
    private static readonly NtsGeometryServices GeometryServices = new(new PrecisionModel(), 4326);
    private static readonly GeometryFactory GeometryFactory = GeometryServices.CreateGeometryFactory();
    private static readonly WKTReader WktReader = new(GeometryServices);

    public Polygon? ParseGeoJson(string geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var root = doc.RootElement;
            if (!TryGetPolygonGeometry(root, out var polygonElement))
                return null;

            if (!polygonElement.TryGetProperty("coordinates", out var coordinatesElement)
                || coordinatesElement.ValueKind != JsonValueKind.Array
                || coordinatesElement.GetArrayLength() == 0)
            {
                return null;
            }

            var exteriorRing = coordinatesElement[0];
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

    private static bool TryGetPolygonGeometry(JsonElement root, out JsonElement polygonElement)
    {
        polygonElement = default;

        if (root.ValueKind != JsonValueKind.Object)
            return false;

        if (!root.TryGetProperty("type", out var rootTypeElement)
            || rootTypeElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rootType = rootTypeElement.GetString();

        if (rootType == "Polygon")
        {
            polygonElement = root;
            return true;
        }

        if (rootType == "Feature")
        {
            if (!root.TryGetProperty("geometry", out var geometryElement) || !IsPolygonGeometry(geometryElement))
                return false;

            polygonElement = geometryElement;
            return true;
        }

        if (rootType == "FeatureCollection")
        {
            if (!root.TryGetProperty("features", out var featuresElement)
                || featuresElement.ValueKind != JsonValueKind.Array
                || featuresElement.GetArrayLength() != 1)
            {
                return false;
            }

            var feature = featuresElement[0];
            if (feature.ValueKind != JsonValueKind.Object
                || !feature.TryGetProperty("geometry", out var geometryElement)
                || !IsPolygonGeometry(geometryElement))
            {
                return false;
            }

            polygonElement = geometryElement;
            return true;
        }

        return false;
    }

    private static bool IsPolygonGeometry(JsonElement geometryElement)
    {
        return geometryElement.ValueKind == JsonValueKind.Object
            && geometryElement.TryGetProperty("type", out var geometryTypeElement)
            && geometryTypeElement.ValueKind == JsonValueKind.String
            && geometryTypeElement.GetString() == "Polygon";
    }

    private static List<Coordinate>? ParseRing(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return null;

        var coords = new List<Coordinate>();
        foreach (var point in element.EnumerateArray())
        {
            if (point.ValueKind != JsonValueKind.Array)
                return null;

            var pair = point.EnumerateArray().ToList();
            if (pair.Count < 2)
                return null;

            coords.Add(new Coordinate(pair[0].GetDouble(), pair[1].GetDouble()));
        }

        return coords;
    }
}
