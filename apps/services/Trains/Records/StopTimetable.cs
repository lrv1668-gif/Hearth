namespace Trains.Records;

/// <summary>
/// The frame's Scheduled Departures for one stop: the timetabled departures from when the
/// clock hour's edition was picked until the end of the next hour, per line and direction.
/// </summary>
/// <param name="WindowStart">ISO 8601 UTC — the top of the hour the edition belongs to.</param>
/// <param name="WindowEnd">ISO 8601 UTC — two hours after <paramref name="WindowStart"/>.</param>
public record StopTimetable(string StopKey, string? StopName, string WindowStart, string WindowEnd, IReadOnlyList<TimetableLine> Lines);

/// <param name="Departures">Scheduled departure instants, ISO 8601 UTC, in order.</param>
public record TimetableLine(string RouteShortName, string? Headsign, string Mode, IReadOnlyList<string> Departures);
