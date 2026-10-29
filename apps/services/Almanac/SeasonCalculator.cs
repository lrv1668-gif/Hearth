namespace Almanac;

public enum SeasonMarker
{
    MarchEquinox,
    JuneSolstice,
    SeptemberEquinox,
    DecemberSolstice,
}

public record SeasonSpan(
    string Name,
    string Label,
    DateTimeOffset Start,
    DateTimeOffset End,
    int DayOfSeason,
    int TotalDays,
    double Progress,
    string NextMarkerName
);

public static class SeasonCalculator
{
    // Equinox and solstice instants (UTC), 2024–2040, from USNO/NASA tables.
    // Season-boundary math only needs day-level accuracy, so a fixed table beats
    // reimplementing Meeus.
    private static readonly (DateTimeOffset Instant, SeasonMarker Marker)[] Markers =
    [
        (new DateTimeOffset(2024, 3, 20, 3, 6, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2024, 6, 20, 20, 51, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2024, 9, 22, 12, 44, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2024, 12, 21, 9, 20, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2025, 3, 20, 9, 1, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2025, 6, 21, 2, 42, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2025, 9, 22, 18, 19, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2025, 12, 21, 15, 3, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2026, 3, 20, 14, 46, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2026, 6, 21, 8, 24, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2026, 9, 23, 0, 5, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2026, 12, 21, 20, 50, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2027, 3, 20, 20, 25, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2027, 6, 21, 14, 11, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2027, 9, 23, 6, 2, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2027, 12, 22, 2, 42, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2028, 3, 20, 2, 17, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2028, 6, 20, 20, 2, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2028, 9, 22, 11, 45, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2028, 12, 21, 8, 19, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2029, 3, 20, 8, 2, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2029, 6, 21, 1, 48, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2029, 9, 22, 17, 38, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2029, 12, 21, 14, 14, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2030, 3, 20, 13, 52, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2030, 6, 21, 7, 31, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2030, 9, 22, 23, 27, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2030, 12, 21, 20, 9, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2031, 3, 20, 19, 41, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2031, 6, 21, 13, 17, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2031, 9, 23, 5, 15, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2031, 12, 22, 1, 55, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2032, 3, 20, 1, 22, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2032, 6, 20, 19, 9, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2032, 9, 22, 11, 11, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2032, 12, 21, 7, 56, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2033, 3, 20, 7, 22, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2033, 6, 21, 1, 1, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2033, 9, 22, 16, 51, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2033, 12, 21, 13, 46, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2034, 3, 20, 13, 17, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2034, 6, 21, 6, 44, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2034, 9, 22, 22, 39, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2034, 12, 21, 19, 34, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2035, 3, 20, 19, 2, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2035, 6, 21, 12, 33, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2035, 9, 23, 4, 39, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2035, 12, 22, 1, 31, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2036, 3, 20, 1, 3, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2036, 6, 20, 18, 32, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2036, 9, 22, 10, 23, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2036, 12, 21, 7, 13, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2037, 3, 20, 6, 50, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2037, 6, 21, 0, 22, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2037, 9, 22, 16, 13, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2037, 12, 21, 13, 7, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2038, 3, 20, 12, 40, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2038, 6, 21, 6, 9, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2038, 9, 22, 22, 2, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2038, 12, 21, 19, 2, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2039, 3, 20, 18, 32, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2039, 6, 21, 11, 57, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2039, 9, 23, 3, 49, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2039, 12, 22, 0, 40, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
        (new DateTimeOffset(2040, 3, 20, 0, 11, 0, TimeSpan.Zero), SeasonMarker.MarchEquinox),
        (new DateTimeOffset(2040, 6, 20, 17, 46, 0, TimeSpan.Zero), SeasonMarker.JuneSolstice),
        (new DateTimeOffset(2040, 9, 22, 9, 44, 0, TimeSpan.Zero), SeasonMarker.SeptemberEquinox),
        (new DateTimeOffset(2040, 12, 21, 5, 32, 0, TimeSpan.Zero), SeasonMarker.DecemberSolstice),
    ];

    public static SeasonSpan GetSeason(DateTimeOffset utcNow, bool isNorthern)
    {
        var startIndex = Array.FindLastIndex(Markers, m => m.Instant <= utcNow);
        if (startIndex < 0 || startIndex >= Markers.Length - 1)
            throw new InvalidOperationException($"{utcNow:O} is outside the season marker table (2024–2040).");

        var (start, marker) = Markers[startIndex];
        var (end, nextMarker) = Markers[startIndex + 1];

        var name = SeasonName(marker, isNorthern);
        var totalDays = (int)Math.Round((end - start).TotalDays);
        var dayOfSeason = (int)Math.Floor((utcNow - start).TotalDays) + 1;
        var progress = (utcNow - start).TotalDays / (end - start).TotalDays;

        return new SeasonSpan(
            name,
            SubSeasonLabel(name, progress),
            start,
            end,
            dayOfSeason,
            totalDays,
            progress,
            MarkerName(nextMarker, isNorthern));
    }

    /// <summary>The most recent solstice before <paramref name="utcNow"/>, and whether it was the longest day.</summary>
    public static (DateTimeOffset Instant, bool IsLongestDay) LastSolstice(DateTimeOffset utcNow, bool isNorthern)
    {
        var index = Array.FindLastIndex(Markers, m =>
            m.Instant <= utcNow &&
            m.Marker is SeasonMarker.JuneSolstice or SeasonMarker.DecemberSolstice);
        if (index < 0)
            throw new InvalidOperationException($"{utcNow:O} is outside the season marker table (2024–2040).");

        var (instant, marker) = Markers[index];
        return (instant, marker == SeasonMarker.JuneSolstice == isNorthern);
    }

    private static string SeasonName(SeasonMarker startMarker, bool isNorthern) => startMarker switch
    {
        SeasonMarker.MarchEquinox => isNorthern ? "spring" : "autumn",
        SeasonMarker.JuneSolstice => isNorthern ? "summer" : "winter",
        SeasonMarker.SeptemberEquinox => isNorthern ? "autumn" : "spring",
        _ => isNorthern ? "winter" : "summer",
    };

    private static string MarkerName(SeasonMarker marker, bool isNorthern) => marker switch
    {
        SeasonMarker.MarchEquinox => isNorthern ? "Spring equinox" : "Autumn equinox",
        SeasonMarker.JuneSolstice => isNorthern ? "Summer solstice" : "Winter solstice",
        SeasonMarker.SeptemberEquinox => isNorthern ? "Autumn equinox" : "Spring equinox",
        _ => isNorthern ? "Winter solstice" : "Summer solstice",
    };

    private static string SubSeasonLabel(string name, double progress)
    {
        if (progress < 1.0 / 3.0) return $"Early {name}";
        if (progress < 2.0 / 3.0)
            return name is "summer" or "winter" ? $"Mid{name}" : $"Mid-{name}";
        return $"Late {name}";
    }
}
