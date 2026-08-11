namespace Trains.Records;

/// <summary>A single upcoming departure at a stop, as served to the frontend.</summary>
public record TrainDeparture(
    string RouteShortName,
    string? RouteLongName,
    int RouteType,
    string Mode,
    string? Headsign,
    string? ScheduledDeparture,
    string? EstimatedDeparture,
    bool IsRealtime);
