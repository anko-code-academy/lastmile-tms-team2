using NetTopologySuite.Geometries;

namespace LastMile.TMS.Application.Zones.Services;

public interface IZoneBoundaryParser
{
    Polygon? ParseGeoJson(string geoJson);
    Polygon? ParseCoordinates(List<List<double>> coordinates);
    Polygon? ParseWkt(string wkt);
}
