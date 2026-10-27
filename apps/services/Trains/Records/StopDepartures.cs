namespace Trains.Records;

/// <summary>All known upcoming departures for one requested stop.</summary>
public record StopDepartures(string StopKey, string? StopName, IEnumerable<TrainDeparture> Departures);
