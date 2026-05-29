using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Calendar.Providers;
using Calendar.Records;

namespace Calendar.Providers.Google;

public sealed class GoogleCalendarProvider(
    CalendarStore store,
    GoogleAuthService authService,
    ILogger<GoogleCalendarProvider> logger) : ICalendarProvider
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private const string Key = GoogleAuthService.ProviderKey;
    private static readonly TimeSpan CacheTtl           = TimeSpan.FromHours(1);
    private static readonly TimeSpan TokenRefreshBuffer = TimeSpan.FromSeconds(30);

    public string ProviderKey   => Key;
    public bool IsAuthenticated => store.HasToken(Key);

    public string GetAuthUrl(string state)                             => authService.GenerateAuthUrl(state);
    public Task HandleCallbackAsync(string code, CancellationToken ct) => authService.HandleCallbackAsync(code, ct);
    public void Disconnect()                                           => store.Clear(Key);

    public async Task<IEnumerable<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // 1. Serve from cache if fresh
        var cached = store.LoadEventsCache(Key);
        if (cached is not null && DateTimeOffset.UtcNow - cached.Value.CachedAt < CacheTtl)
            return JsonSerializer.Deserialize<List<CalendarEvent>>(cached.Value.Json, JsonOpts) ?? [];

        // 2. Load stored token
        var token = store.LoadToken(Key);
        if (token is null)
        {
            logger.LogWarning("Google token missing despite IsAuthenticated=true; returning empty events.");
            return [];
        }

        // 3. Refresh if within 30s of expiry
        var accessToken = token.AccessToken;
        if (token.ExpiresAt <= DateTimeOffset.UtcNow + TokenRefreshBuffer)
        {
            try
            {
                var flow       = authService.BuildFlow();
                var tokenResp  = new TokenResponse { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken };
                var credential = new UserCredential(flow, "user", tokenResp);

                if (await credential.RefreshTokenAsync(ct))
                {
                    var newRefresh = credential.Token.RefreshToken ?? token.RefreshToken;
                    var newExpiry  = DateTimeOffset.UtcNow.AddSeconds(credential.Token.ExpiresInSeconds ?? 3600);
                    store.SaveToken(Key, credential.Token.AccessToken, newRefresh, newExpiry);
                    accessToken = credential.Token.AccessToken;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Google token refresh failed; returning empty events. Will retry next request.");
                return [];
            }
        }

        // 4. Fetch from Google Calendar API
        var calService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName       = "Hearth",
        });

        List<CalendarEvent> events;
        try
        {
            var req = calService.Events.List("primary");
            req.SingleEvents          = true;
            req.OrderBy               = EventsResource.ListRequest.OrderByEnum.StartTime;
            req.TimeMinDateTimeOffset = from;
            req.TimeMaxDateTimeOffset = to;
            req.MaxResults            = 2500;

            var allItems = new List<Event>();
            do
            {
                var resp = await req.ExecuteAsync(ct);
                allItems.AddRange(resp.Items ?? []);
                req.PageToken = resp.NextPageToken;
            } while (!string.IsNullOrEmpty(req.PageToken));

            events = MapEvents(allItems);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google Calendar API error; returning empty events.");
            return [];
        }

        // 5. Persist cache and return
        store.SaveEventsCache(Key, JsonSerializer.Serialize(events, JsonOpts));
        return events;
    }

    private static List<CalendarEvent> MapEvents(IList<Event> items)
    {
        var result = new List<CalendarEvent>(items.Count);
        foreach (var e in items)
        {
            if (e.Start is null) continue;

            var isAllDay = e.Start.DateTimeDateTimeOffset is null;
            var start    = isAllDay ? e.Start.Date! : e.Start.DateTimeDateTimeOffset!.Value.ToString("o");
            var end      = isAllDay
                ? (e.End?.Date ?? e.Start.Date!)
                : (e.End?.DateTimeDateTimeOffset?.ToString("o") ?? start);

            result.Add(new CalendarEvent(
                Id:           e.Id ?? Guid.NewGuid().ToString(),
                Title:        e.Summary ?? "(No title)",
                Description:  e.Description,
                Location:     e.Location,
                Start:        start,
                End:          end,
                IsAllDay:     isAllDay,
                CalendarName: null,
                Provider:     Key));
        }
        return result;
    }
}
