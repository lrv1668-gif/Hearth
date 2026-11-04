using System.Globalization;
using History.Records;
using ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace History.Extensions;

public static class WebApplicationExtensions
{
    // Keyed by the date, so each day fetches its own; fail-safe rides out a failed refresh within it.
    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(24),
        FactorySoftTimeout = HearthCache.SoftTimeout,
        EagerRefreshThreshold = HearthCache.EagerRefresh,
    };

    public static void InitializeWebAppForHistory(this WebApplication app)
    {
        app.AddHistoryEndpoints();
        app.MapHearthHealth("history");
    }

    private static void AddHistoryEndpoints(this WebApplication app)
    {
        // Today's events for the household's date, held until its local midnight.
        app.MapGet("/history/onthisday", async (
            IFusionCache cache, HistoryFetcher fetcher, HomeLocationClient home, TimeProvider clock, CancellationToken ct) =>
        {
            var now = clock.GetUtcNow();
            var zone = HistoryExpiry.Zone((await home.GetAsync(ct)).TimeZone);
            var today = HistoryExpiry.Today(now, zone);
            var date = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            try
            {
                var events = await cache.GetOrSetAsync<IReadOnlyList<HistoryEvent>>(
                    $"onthisday:{date}",
                    async (_, token) => await fetcher.FetchAsync(today, token)
                        ?? throw new InvalidOperationException("Wikipedia returned no events"),
                    CacheOptions.Duplicate(HistoryExpiry.NextMidnight(now, zone) - now),
                    ct);
                return Results.Ok(new OnThisDay(date, events));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                app.Logger.LogError(ex, "No On this day events for {Date}", date);
                return Results.Problem("No events available", statusCode: 503);
            }
        });
    }
}
