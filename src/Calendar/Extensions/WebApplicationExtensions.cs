using Calendar;
using Calendar.Providers;
using Calendar.Providers.Google;
using Calendar.Records;

namespace Calendar.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForCalendar(this WebApplication app)
    {
        app.Services.GetRequiredService<CalendarStore>().Migrate();

        var health = GetHealth(app.Configuration);
        if (health.Configured)
            app.Logger.LogInformation("Calendar configured — GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, GOOGLE_REDIRECT_URI set");
        else
            app.Logger.LogWarning(
                "Calendar not configured — missing {Missing}; /calendar/google/auth will return 503 until set in src/Calendar/.env",
                string.Join(", ", health.Missing));

        app.AddCalendarEndpoints();
    }

    private static HealthResponse GetHealth(IConfiguration config) =>
        Health.Evaluate(
            ("GOOGLE_CLIENT_ID", config["GOOGLE_CLIENT_ID"]),
            ("GOOGLE_CLIENT_SECRET", config["GOOGLE_CLIENT_SECRET"]),
            ("GOOGLE_REDIRECT_URI", config["GOOGLE_REDIRECT_URI"]));

    private static void AddCalendarEndpoints(this WebApplication app)
    {
        // GET /calendar/health → { configured, missing[] } — env readiness, distinct from
        // /calendar/google/status which reports OAuth-connected state.
        app.MapGet("/calendar/health", (IConfiguration config) => Results.Ok(GetHealth(config)));

        // GET /calendar/google/auth
        // Validates required env vars (CLAUDE.md: LogError + 503 if any missing).
        // Generates CSRF state token (10-min expiry), redirects to Google consent page.
        app.MapGet("/calendar/google/auth", (
            GoogleCalendarProvider provider,
            IConfiguration config,
            ILogger<GoogleCalendarProvider> logger) =>
        {
            foreach (var varName in new[] { "GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "GOOGLE_REDIRECT_URI" })
            {
                if (string.IsNullOrWhiteSpace(config[varName]))
                {
                    logger.LogError("{Var} is not set. Configure it in services/Calendar/.env", varName);
                    return Results.Problem(statusCode: 503, detail: $"{varName} not configured");
                }
            }

            var state = Guid.NewGuid().ToString("N");
            return Results.Redirect(provider.GetAuthUrl(state));
        });

        // GET /calendar/google/callback
        // Validates CSRF state, exchanges code for tokens, redirects to frontend.
        app.MapGet("/calendar/google/callback", async (
            string code,
            string state,
            GoogleAuthService authService,
            IConfiguration config) =>
        {
            if (!authService.ValidateAndConsumeState(state))
                return Results.BadRequest("Invalid or expired state token");

            await authService.HandleCallbackAsync(code);

            return Results.Redirect(config["FRONTEND_URL"] ?? "/");
        });

        // GET /calendar/google/status → { authenticated: bool }
        app.MapGet("/calendar/google/status", (GoogleCalendarProvider provider) =>
            Results.Ok(new CalendarStatusResponse(provider.IsAuthenticated)));

        // DELETE /calendar/google/auth — clears token + item cache, returns 204
        app.MapDelete("/calendar/google/auth", (GoogleCalendarProvider provider) =>
        {
            provider.Disconnect();
            return Results.NoContent();
        });

        // GET /calendar/items — aggregates across all authenticated providers.
        // GetItemsAsync catches all exceptions internally and returns empty on failure,
        // so no outer try/catch is needed here.
        app.MapGet("/calendar/items", async (
            IEnumerable<ICalendarProvider> providers,
            CancellationToken ct) =>
        {
            var from = DateTimeOffset.UtcNow;
            var to   = from.AddDays(14);

            var fetches = providers
                .Where(p => p.IsAuthenticated)
                .Select(p => p.GetItemsAsync(from, to, ct));

            var all = await Task.WhenAll(fetches);
            return Results.Ok(all.SelectMany(e => e).ToList());
        });

        // POST /calendar/google/refresh — invalidates the items cache so the next /calendar/items
        // fetches directly from Google rather than serving the 5-minute cached result.
        app.MapPost("/calendar/google/refresh", (
            GoogleCalendarProvider provider,
            CalendarStore store) =>
        {
            if (!provider.IsAuthenticated)
                return Results.Unauthorized();

            store.InvalidateItemsCache(GoogleAuthService.ProviderKey);
            return Results.NoContent();
        });

        // PATCH /calendar/google/tasks/{taskListId}/{taskId}
        // Body: { "completed": bool }
        // Updates completion state in Google Tasks and invalidates the items cache.
        app.MapMethods("/calendar/google/tasks/{taskListId}/{taskId}", ["PATCH"], async (
            string taskListId,
            string taskId,
            ToggleTaskRequest body,
            GoogleCalendarProvider provider,
            CancellationToken ct) =>
        {
            if (!provider.IsAuthenticated)
                return Results.Unauthorized();

            try
            {
                await provider.SetTaskCompletedAsync(taskListId, taskId, body.Completed, ct);
                return Results.NoContent();
            }
            catch (Exception)
            {
                return Results.Problem(statusCode: 502, detail: "Failed to update task in Google Tasks.");
            }
        });
    }
}

internal record ToggleTaskRequest(bool Completed);
