using ServiceDefaults;
using Quote.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Quote.Extensions;

public static class WebApplicationExtensions
{
    private const string CacheKey = "quote";

    // Yesterday's quote beats an empty widget if ZenQuotes is down past midnight.
    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(24),
    };

    public static void InitializeWebAppForQuote(this WebApplication app)
    {
        app.AddQuoteEndpoints();
        app.MapHearthHealth("quote");
    }

    private static void AddQuoteEndpoints(this WebApplication app)
    {
        app.MapGet("/quote", async (IFusionCache cache, QuoteFetcher fetcher, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var options = CacheOptions.Duplicate(QuoteCacheExpiry.NextMidnightUtc(now) - now);

            try
            {
                var quote = await cache.GetOrSetAsync<QuoteItem>(
                    CacheKey,
                    async (_, token) => await fetcher.FetchAsync(token)
                        ?? throw new InvalidOperationException("ZenQuotes returned no quote"),
                    options,
                    ct);
                return Results.Ok(quote);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                app.Logger.LogError(ex, "Failed to fetch daily quote from ZenQuotes");
                return Results.Problem("No quote available", statusCode: 503);
            }
        });
    }
}
