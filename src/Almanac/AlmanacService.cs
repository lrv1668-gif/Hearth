using System.Globalization;
using Almanac.Records;

namespace Almanac;

public class AlmanacService
{
    private const int MilestoneScanDays = 180;

    // The widget shows the pinned season plus two rotating slots. Frost only
    // claims a slot when it's close enough to matter; otherwise the note shows.
    private const int FrostTimelyDays = 42;
    private static readonly TimeSpan EveningSunset = new(20, 0, 0);
    private static readonly TimeSpan EarlySunset = new(17, 0, 0);
    private static readonly TimeSpan LateSunrise = new(7, 0, 0);

    private readonly double? _latitude;
    private readonly double? _longitude;
    private readonly (int Month, int Day)? _firstFrost;
    private readonly (int Month, int Day)? _lastFrost;
    private readonly TimeZoneInfo _tz;

    public AlmanacService(IConfiguration config, ILogger<AlmanacService> logger)
    {
        _latitude = ParseCoordinate(config["LATITUDE"]);
        _longitude = ParseCoordinate(config["LONGITUDE"]);
        _firstFrost = FrostCalculator.Parse(config["FIRST_FROST"]);
        _lastFrost = FrostCalculator.Parse(config["LAST_FROST"]);

        var tzId = config["TZ"];
        try
        {
            _tz = string.IsNullOrEmpty(tzId) ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch (TimeZoneNotFoundException)
        {
            logger.LogError("TZ value '{TzId}' is not a valid IANA time zone — falling back to the system zone.", tzId);
            _tz = TimeZoneInfo.Local;
        }
    }

    public AlmanacResponse Build(DateTimeOffset utcNow)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, _tz).DateTime);
        var isNorthern = _latitude is null or >= 0;

        var season = SeasonCalculator.GetSeason(utcNow, isNorthern);
        var nextMarkerDate = ToLocalDate(season.End);
        var seasonInfo = new SeasonInfo(
            season.Name,
            season.Label,
            season.DayOfSeason,
            season.TotalDays,
            Math.Round(season.Progress, 2),
            season.NextMarkerName,
            nextMarkerDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            nextMarkerDate.DayNumber - today.DayNumber);

        FrostInfo? frost = null;
        if (FrostCalculator.Next(today, _firstFrost, _lastFrost) is { } f && f.DaysUntil <= FrostTimelyDays)
            frost = new FrostInfo(f.Label, f.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), f.DaysUntil);

        var daylight = BuildDaylight(utcNow, today, isNorthern);
        var note = PhenologyData.NoteFor(today, isNorthern);

        // Fill the two rotating slots in priority order: daylight, timely frost, note.
        var filled = 0;
        DaylightInfo? daylightSlot = daylight is not null && filled++ < 2 ? daylight : null;
        FrostInfo? frostSlot = frost is not null && filled++ < 2 ? frost : null;
        string? noteSlot = note is not null && filled++ < 2 ? note : null;

        return new AlmanacResponse(seasonInfo, daylightSlot, frostSlot, noteSlot);
    }

    private DaylightInfo? BuildDaylight(DateTimeOffset utcNow, DateOnly today, bool isNorthern)
    {
        if (_latitude is not { } lat || _longitude is not { } lon) return null;

        var todayLength = SolarCalculator.DayLengthMinutes(today, lat);
        var weekAgoLength = SolarCalculator.DayLengthMinutes(today.AddDays(-7), lat);
        if (todayLength is null || weekAgoLength is null) return null; // polar day/night

        var trend = (todayLength.Value - weekAgoLength.Value) / 7.0;
        trend = Math.Abs(trend) < 0.25 ? 0 : Math.Round(trend, 1);

        var (solstice, isLongestDay) = SeasonCalculator.LastSolstice(utcNow, isNorthern);
        var solsticeLength = SolarCalculator.DayLengthMinutes(ToLocalDate(solstice), lat);
        var drift = (int)Math.Round(todayLength.Value - (solsticeLength ?? todayLength.Value));

        return new DaylightInfo(
            trend,
            drift,
            isLongestDay ? "longest day" : "shortest day",
            FindMilestones(today, lat, lon));
    }

    private List<MilestoneInfo> FindMilestones(DateOnly today, double lat, double lon)
    {
        var found = new List<(string Label, DateOnly Date)>();
        var previous = SolarCalculator.SunTimesFor(today, lat, lon, _tz);

        for (var d = 1; d <= MilestoneScanDays && previous is not null; d++)
        {
            var date = today.AddDays(d);
            var current = SolarCalculator.SunTimesFor(date, lat, lon, _tz);
            if (current is null) break; // entering polar day/night — stop scanning

            RecordTransition(found, "Last 8pm sunset", "First 8pm sunset",
                previous.Sunset.TimeOfDay >= EveningSunset, current.Sunset.TimeOfDay >= EveningSunset, date);
            RecordTransition(found, "Last sunset before 5pm", "First sunset before 5pm",
                previous.Sunset.TimeOfDay < EarlySunset, current.Sunset.TimeOfDay < EarlySunset, date);
            RecordTransition(found, "Last sunrise after 7am", "First sunrise after 7am",
                previous.Sunrise.TimeOfDay >= LateSunrise, current.Sunrise.TimeOfDay >= LateSunrise, date);

            previous = current;
        }

        return [.. found
            .OrderBy(m => m.Date)
            .Take(1)
            .Select(m => new MilestoneInfo(m.Label, m.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))];
    }

    private static void RecordTransition(
        List<(string Label, DateOnly Date)> found,
        string endedLabel, string startedLabel,
        bool wasTrue, bool isTrue, DateOnly date)
    {
        if (wasTrue == isTrue) return;
        var label = wasTrue ? endedLabel : startedLabel;
        // Ended conditions belong to the last day they still held.
        var eventDate = wasTrue ? date.AddDays(-1) : date;
        if (found.All(m => m.Label != label)) found.Add((label, eventDate));
    }

    private DateOnly ToLocalDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _tz).DateTime);

    private static double? ParseCoordinate(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
}
