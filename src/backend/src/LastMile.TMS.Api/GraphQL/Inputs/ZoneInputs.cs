namespace LastMile.TMS.Api.GraphQL.Inputs;

public class CreateZoneInput
{
    public string Name { get; set; } = null!;
    public Guid DepotId { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// GeoJSON Polygon geometry (e.g. { "type": "Polygon", "coordinates": [[[151.195, -33.880], ...]] })
    /// Provide exactly one of: geoJson, coordinates, or boundaryWkt.
    /// </summary>
    public string? GeoJson { get; set; }

    /// <summary>
    /// Polygon vertex coordinates as [lon, lat] pairs (closed ring).
    /// Example: [[151.195, -33.880], [151.225, -33.880], [151.225, -33.855], [151.195, -33.855], [151.195, -33.880]]
    /// Provide exactly one of: geoJson, coordinates, or boundaryWkt.
    /// </summary>
    public List<List<double>>? Coordinates { get; set; }

    /// <summary>
    /// WKT POLYGON string (e.g. "POLYGON ((151.195 -33.880, ...))").
    /// Provide exactly one of: geoJson, coordinates, or boundaryWkt.
    /// </summary>
    public string? BoundaryWkt { get; set; }
}

public class UpdateZoneInput
{
    public string Name { get; set; } = null!;
    public Guid DepotId { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// GeoJSON Polygon geometry (e.g. { "type": "Polygon", "coordinates": [[[151.195, -33.880], ...]] })
    /// Provide exactly one of: geoJson, coordinates, or boundaryWkt.
    /// </summary>
    public string? GeoJson { get; set; }

    /// <summary>
    /// Polygon vertex coordinates as [lon, lat] pairs (closed ring).
    /// Example: [[151.195, -33.880], [151.225, -33.880], [151.225, -33.855], [151.195, -33.855], [151.195, -33.880]]
    /// Provide exactly one of: geoJson, coordinates, or boundaryWkt.
    /// </summary>
    public List<List<double>>? Coordinates { get; set; }

    /// <summary>
    /// WKT POLYGON string (e.g. "POLYGON ((151.195 -33.880, ...))").
    /// Provide exactly one of: geoJson, coordinates, or boundaryWkt.
    /// </summary>
    public string? BoundaryWkt { get; set; }
}
