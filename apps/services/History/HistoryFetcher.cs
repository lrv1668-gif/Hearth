using System.Globalization;
using System.Text.Json;
using History.Records;

namespace History;

/// <summary>
/// Wikipedia's curated "On this day" events (the Wikimedia feed API — free, no key). Its
/// content is CC BY-SA, so the frame credits Wikipedia wherever it shows them.
/// </summary>
public sealed class HistoryFetcher(HttpClient http, ILogger<HistoryFetcher> logger)
{
    public const int MaxEvents = 6;

    /// <summary>The selected events for <paramref name="date"/>'s month and day, oldest first; null on failure.</summary>
    public async Task<IReadOnlyList<HistoryEvent>?> FetchAsync(DateOnly date, CancellationToken ct = default)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"https://api.wikimedia.org/feed/v1/wikipedia/en/onthisday/selected/{date.Month:00}/{date.Day:00}");
        try
        {
            using var response = await http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            return Parse(doc.RootElement);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to fetch On this day from Wikipedia for {Date}", date);
            return null;
        }
    }

    internal static IReadOnlyList<HistoryEvent> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("selected", out var selected) || selected.ValueKind != JsonValueKind.Array)
            return [];

        var events = new List<HistoryEvent>();
        foreach (var e in selected.EnumerateArray())
        {
            if (!e.TryGetProperty("year", out var y) || !y.TryGetInt32(out var year)) continue;
            if (!e.TryGetProperty("text", out var t) || t.GetString() is not { Length: > 0 } text) continue;

            string? page = null;
            if (e.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array && pages.GetArrayLength() > 0 &&
                pages[0].TryGetProperty("titles", out var titles) && titles.TryGetProperty("normalized", out var normalized))
            {
                page = normalized.GetString();
            }

            events.Add(new HistoryEvent(year, text.Trim(), page));
        }

        return Spread(events, MaxEvents);
    }

    /// <summary>
    /// <paramref name="count"/> of the editors' picks spread evenly across the years, oldest
    /// first, so the card reads like an almanac rather than leading with last year's news.
    /// Deterministic, so the frame doesn't redraw for it during the day.
    /// </summary>
    internal static IReadOnlyList<HistoryEvent> Spread(IReadOnlyList<HistoryEvent> events, int count)
    {
        var byYear = events.OrderBy(e => e.Year).ToList();
        if (byYear.Count <= count) return byYear;
        return Enumerable.Range(0, count)
            .Select(i => byYear[(int)Math.Round(i * (byYear.Count - 1) / (double)(count - 1))])
            .ToList();
    }
}
