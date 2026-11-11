using Calendar.Records;
using ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace Calendar.Providers.Ics;

/// <summary>
/// Calendars subscribed to by link — an iCloud public calendar's <c>webcal://</c> address, or
/// any other .ics URL. Read-only, no sign-in. Each calendar is fetched at most every
/// <see cref="Duration"/>; a failed refresh keeps serving the last good copy.
/// </summary>
public sealed class IcsCalendarProvider(
    IcsFeedStore store,
    IIcsSource source,
    IFusionCache cache,
    HomeLocationClient home,
    ILogger<IcsCalendarProvider> logger) : ICalendarProvider
{
    public const string Key = "ics";

    internal const int MaxNameLength = 60;
    internal const int MaxUrlLength  = 2000;

    internal static readonly TimeSpan Duration   = TimeSpan.FromMinutes(15);
    internal static readonly TimeSpan RetryAfter = TimeSpan.FromMinutes(5);

    private static readonly FusionCacheEntryOptions CacheOptions = new()
    {
        Duration = Duration,
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(1),
        FailSafeThrottleDuration = RetryAfter,
        FactorySoftTimeout = HearthCache.SoftTimeout,
        EagerRefreshThreshold = HearthCache.EagerRefresh,
    };

    public string ProviderKey => Key;
    public bool IsConnected   => store.Any();

    public List<IcsFeed> Feeds => store.List();

    public async Task<IEnumerable<CalendarItem>> GetItemsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var zone   = await HomeZoneAsync(ct);
        var perFeed = await Task.WhenAll(store.List().Select(feed => GetFeedItemsAsync(feed, from, to, zone, ct)));
        return perFeed.SelectMany(items => items);
    }

    /// <summary>
    /// Subscribes to a calendar after reading it once, so a wrong link is refused here rather
    /// than showing nothing later. A blank name takes the calendar's own. Throws
    /// <see cref="IcsFeedException"/> with a message for Settings.
    /// </summary>
    public async Task<IcsFeed> AddFeedAsync(string? name, string? url, CancellationToken ct = default)
    {
        if (store.List().Count >= IcsFeedStore.MaxFeeds)
            throw new IcsFeedException($"At most {IcsFeedStore.MaxFeeds} calendars can be subscribed to.");

        if (url is { Length: > MaxUrlLength } || IcsFetcher.NormalizeUrl(url) is not { } normalized)
            throw new IcsFeedException("Enter the calendar's link — it starts with webcal:// or https://.");

        string ics;
        try
        {
            ics = await source.FetchAsync(normalized, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            // The link is a secret, so only the failure is logged
            logger.LogWarning("Couldn't read a calendar link being added: {Error}", ex.Message);
            throw new IcsFeedException("Couldn't read that link. Check the calendar is still shared publicly.");
        }

        var calendar = IcsParser.Load(ics);
        var chosen   = string.IsNullOrWhiteSpace(name) ? IcsParser.NameOf(calendar) ?? "Calendar" : name.Trim();
        if (chosen.Length > MaxNameLength) chosen = chosen[..MaxNameLength];

        return store.Add(chosen, normalized);
    }

    /// <summary>False when there was no such calendar.</summary>
    public async Task<bool> RemoveFeedAsync(string id, CancellationToken ct = default)
    {
        var removed = store.Remove(id);
        await cache.RemoveAsync(CacheKey(id), token: ct);
        return removed;
    }

    /// <summary>Makes the next read fetch every calendar again; the old copies stay as the fail-safe.</summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        foreach (var feed in store.List())
            await cache.ExpireAsync(CacheKey(feed.Id), token: ct);
    }

    private static string CacheKey(string feedId) => $"ics:{feedId}";

    private async Task<List<CalendarItem>> GetFeedItemsAsync(
        IcsFeed feed, DateTimeOffset from, DateTimeOffset to, TimeZoneInfo? zone, CancellationToken ct)
    {
        try
        {
            // Keyed by calendar alone: the window only moves at UTC midnight, well inside what a
            // 15-minute-old copy still covers
            return await cache.GetOrSetAsync<List<CalendarItem>>(
                CacheKey(feed.Id),
                async (_, token) => IcsParser.Expand(
                    IcsParser.Load(await source.FetchAsync(feed.Url, token)), feed, from, to, zone),
                CacheOptions,
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Never fetched successfully. The link is a secret, so it's named rather than logged.
            logger.LogError("Couldn't read the subscribed calendar \"{Name}\": {Error}", feed.Name, ex.Message);
            return [];
        }
    }

    private async Task<TimeZoneInfo?> HomeZoneAsync(CancellationToken ct)
    {
        var id = (await home.GetAsync(ct)).TimeZone;
        return id is not null && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) ? zone : null;
    }
}
