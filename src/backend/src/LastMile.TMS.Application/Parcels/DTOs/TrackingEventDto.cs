namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed record TrackingEventDto
{
    public Guid Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? Operator { get; init; }

    public TrackingEventDto() { }
}
