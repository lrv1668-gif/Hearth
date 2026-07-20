namespace Photos;

/// <summary>
/// Expands the "seasonal" category token in a comma-separated query into a
/// season-appropriate Unsplash search term, so the frontend never needs to
/// know what season it is.
/// </summary>
public static class SeasonalQuery
{
    public const string Token = "seasonal";

    public static string Expand(string query, DateOnly today, bool isNorthern)
    {
        var terms = query.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < terms.Length; i++)
        {
            if (string.Equals(terms[i], Token, StringComparison.OrdinalIgnoreCase))
                terms[i] = TermFor(today, isNorthern);
        }
        return string.Join(',', terms);
    }

    private static string TermFor(DateOnly today, bool isNorthern)
    {
        var month = isNorthern ? today.Month : today.Month <= 6 ? today.Month + 6 : today.Month - 6;
        return month switch
        {
            >= 3 and <= 5  => "spring blossoms",
            >= 6 and <= 8  => "summer nature",
            >= 9 and <= 11 => "autumn leaves",
            _              => "winter snow",
        };
    }
}
