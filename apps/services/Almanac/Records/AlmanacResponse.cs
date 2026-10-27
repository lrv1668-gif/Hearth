namespace Almanac.Records;

public record AlmanacResponse(
    SeasonInfo Season,
    DaylightInfo? Daylight,
    FrostInfo? Frost,
    string? Note
);

public record SeasonInfo(
    string Name,
    string Label,
    int DayOfSeason,
    int TotalDays,
    double Progress,
    string NextMarker,
    string NextMarkerDate,
    int DaysUntilMarker
);

public record DaylightInfo(
    double TrendMinutesPerDay,
    int DriftMinutes,
    string DriftReference,
    List<MilestoneInfo> Milestones
);

public record MilestoneInfo(
    string Label,
    string Date
);

public record FrostInfo(
    string Label,
    string Date,
    int DaysUntil
);
