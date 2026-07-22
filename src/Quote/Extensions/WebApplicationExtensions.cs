using Microsoft.Extensions.Caching.Memory;
using Quote.Records;

namespace Quote.Extensions;

public static class WebApplicationExtensions
{
    private const string CacheKey = "quote";

    public static void InitializeWebAppForQuote(this WebApplication app)
    {
        app.AddQuoteEndpoints();
    }

    private static void AddQuoteEndpoints(this WebApplication app)
    {
        app.MapGet("/quote", async (IMemoryCache cache, QuoteFetcher fetcher) =>
        {
            if (!cache.TryGetValue(CacheKey, out QuoteItem? quote))
            {
                quote = await fetcher.FetchAsync();
                if (quote is not null)
                {
                    var expiry = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)
                        .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                    cache.Set(CacheKey, quote, new MemoryCacheEntryOptions { AbsoluteExpiration = expiry });
                }
                else
                {
                    app.Logger.LogError("Failed to fetch daily quote from ZenQuotes");
                }
            }

            return quote is null
                ? Results.Problem("No quote available", statusCode: 503)
                : Results.Ok(quote);
        });
    }
}
