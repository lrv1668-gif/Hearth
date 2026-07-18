using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Tasks.v1;
using Calendar.Providers;
using Calendar.Records;
using GTask = Google.Apis.Tasks.v1.Data.Task;

namespace Calendar.Providers.Google;

public sealed class GoogleCalendarProvider(
    CalendarStore store,
    GoogleAuthService authService,
    ILogger<GoogleCalendarProvider> logger) : ICalendarProvider
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private const string Key = GoogleAuthService.ProviderKey;
    private static readonly TimeSpan CacheTtl           = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TokenRefreshBuffer = TimeSpan.FromSeconds(30);

    public string ProviderKey   => Key;
    public bool IsAuthenticated => store.HasToken(Key);

    public string GetAuthUrl(string state)                             => authService.GenerateAuthUrl(state);
    public Task HandleCallbackAsync(string code, CancellationToken ct) => authService.HandleCallbackAsync(code, ct);
    public void Disconnect()                                           => store.Clear(Key);

    public async Task<IEnumerable<CalendarItem>> GetItemsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        // 1. Serve from cache if fresh
        var cached = store.LoadItemsCache(Key);
        if (cached is not null && DateTimeOffset.UtcNow - cached.Value.CachedAt < CacheTtl)
            return JsonSerializer.Deserialize<List<CalendarItem>>(cached.Value.Json, JsonOpts) ?? [];

        // 2. Load stored token
        var token = store.LoadToken(Key);
        if (token is null)
        {
            logger.LogWarning("Google token missing despite IsAuthenticated=true; returning empty items.");
            return [];
        }

        // 3. Refresh if near expiry, then build API clients
        string accessToken;
        try
        {
            accessToken = await EnsureFreshAccessTokenAsync(token, ct);
        }
        catch
        {
            logger.LogWarning("Google token refresh failed; returning empty items. Will retry next request.");
            return [];
        }

        var initializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName       = "Hearth",
        };

        // 4. Fetch events and tasks in parallel
        var eventsTask = FetchCalendarEventsAsync(initializer, from, to, ct);
        var tasksTask  = FetchGoogleTasksAsync(initializer, from, to, ct);

        await Task.WhenAll(eventsTask, tasksTask);

        var items = new List<CalendarItem>(eventsTask.Result.Count + tasksTask.Result.Count);
        items.AddRange(eventsTask.Result);
        items.AddRange(tasksTask.Result);

        // 5. Persist cache and return
        store.SaveItemsCache(Key, JsonSerializer.Serialize(items, JsonOpts));
        return items;
    }

    public async Task SetTaskCompletedAsync(
        string taskListId, string taskId, bool completed, CancellationToken ct = default)
    {
        var token = store.LoadToken(Key);
        if (token is null)
        {
            logger.LogWarning("Google token missing; cannot update task completion.");
            return;
        }

        var accessToken = await EnsureFreshAccessTokenAsync(token, ct);

        var svc  = new TasksService(new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName       = "Hearth",
        });

        var task = await svc.Tasks.Get(taskListId, taskId).ExecuteAsync(ct);
        task.Status    = completed ? "completed" : "needsAction";
        task.Completed = completed ? DateTimeOffset.UtcNow.ToString("o") : null;
        await svc.Tasks.Update(task, taskListId, taskId).ExecuteAsync(ct);

        // Invalidate cache so next /calendar/items re-fetches from Google
        store.InvalidateItemsCache(Key);
    }

    internal static bool NeedsTokenRefresh(DateTimeOffset expiresAt, DateTimeOffset now) =>
        expiresAt <= now + TokenRefreshBuffer;

    private async Task<string> EnsureFreshAccessTokenAsync(CalendarToken token, CancellationToken ct)
    {
        if (!NeedsTokenRefresh(token.ExpiresAt, DateTimeOffset.UtcNow))
            return token.AccessToken;

        var flow       = authService.BuildFlow();
        var tokenResp  = new TokenResponse { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken };
        var credential = new UserCredential(flow, "user", tokenResp);

        if (await credential.RefreshTokenAsync(ct))
        {
            var newRefresh = credential.Token.RefreshToken ?? token.RefreshToken;
            var newExpiry  = DateTimeOffset.UtcNow.AddSeconds(credential.Token.ExpiresInSeconds ?? 3600);
            store.SaveToken(Key, credential.Token.AccessToken, newRefresh, newExpiry);
            return credential.Token.AccessToken;
        }

        return token.AccessToken;
    }

    private async Task<List<CalendarItem>> FetchCalendarEventsAsync(
        BaseClientService.Initializer initializer,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var calService = new CalendarService(initializer);
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

            return MapCalendarEvents(allItems);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google Calendar API error; skipping events.");
            return [];
        }
    }

    private async Task<List<CalendarItem>> FetchGoogleTasksAsync(
        BaseClientService.Initializer initializer,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var svc = new TasksService(initializer);
        try
        {
            // Fetch all tasks without date filters — Google Tasks stores due dates as midnight UTC
            // ("2026-06-01T00:00:00.000Z"), so using DueMin=UtcNow would exclude every task due
            // "today" because midnight < current time. Filter in-memory instead.
            var req = svc.Tasks.List("@default");
            req.ShowCompleted = true;
            req.ShowHidden    = false;
            req.MaxResults    = 100;

            var allTasks = new List<GTask>();
            do
            {
                var resp = await req.ExecuteAsync(ct);
                allTasks.AddRange(resp.Items ?? []);
                req.PageToken = resp.NextPageToken;
            } while (!string.IsNullOrEmpty(req.PageToken));

            var fromDate = from.UtcDateTime.Date;
            var toDate   = to.UtcDateTime.Date;

            var filtered = allTasks.Where(t =>
            {
                if (t.Due is null) return true; // undated tasks always shown
                return DateTimeOffset.TryParse(t.Due, out var due) &&
                       due.UtcDateTime.Date >= fromDate &&
                       due.UtcDateTime.Date <= toDate;
            }).ToList();

            return MapGoogleTasks(filtered, "@default");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google Tasks API error; skipping tasks.");
            return [];
        }
    }

    internal static List<CalendarItem> MapCalendarEvents(IList<Event> items)
    {
        var result = new List<CalendarItem>(items.Count);
        foreach (var e in items)
        {
            if (e.Start is null) continue;

            var isAllDay = e.Start.DateTimeDateTimeOffset is null;
            var start    = isAllDay ? e.Start.Date! : e.Start.DateTimeDateTimeOffset!.Value.ToString("o");
            var end      = isAllDay
                ? (e.End?.Date ?? e.Start.Date!)
                : (e.End?.DateTimeDateTimeOffset?.ToString("o") ?? start);

            result.Add(new CalendarItem(
                Kind:         "event",
                Id:           e.Id ?? Guid.NewGuid().ToString(),
                Title:        e.Summary ?? "(No title)",
                Description:  e.Description,
                Location:     e.Location,
                Start:        start,
                End:          end,
                IsAllDay:     isAllDay,
                CalendarName: null,
                Provider:     Key,
                IsCompleted:  null,
                TaskListId:   null,
                HtmlLink:     e.HtmlLink));
        }
        return result;
    }

    internal static List<CalendarItem> MapGoogleTasks(
        IList<GTask> tasks, string taskListId)
    {
        var result = new List<CalendarItem>(tasks.Count);
        foreach (var t in tasks)
        {
            if (t.Id is null) continue;

            var start = t.Due is not null ? t.Due[..10] : null;

            result.Add(new CalendarItem(
                Kind:         "task",
                Id:           t.Id,
                Title:        t.Title ?? "(No title)",
                Description:  t.Notes,
                Location:     null,
                Start:        start,
                End:          null,
                IsAllDay:     true,
                CalendarName: null,
                Provider:     Key,
                IsCompleted:  t.Status == "completed",
                TaskListId:   taskListId,
                HtmlLink:     "https://tasks.google.com/"));
        }
        return result;
    }
}
