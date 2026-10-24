using Calendar;
using Calendar.Providers;
using Calendar.Providers.Google;
using Calendar.Records;
using Google.Apis.Auth.OAuth2.Responses;
using ServiceDefaults;

namespace Calendar.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForCalendar(this WebApplication app)
    {
        app.Services.GetRequiredService<CalendarStore>().Migrate();
        app.AddCalendarEndpoints();
        app.MapHearthHealth("calendar");
    }

    private static void AddCalendarEndpoints(this WebApplication app)
    {
        // GET /calendar/google/auth
        // Validates required env vars (CLAUDE.md: LogError + 503 if any missing).
        // Generates CSRF state token (10-min expiry), redirects to Google consent page.
        app.MapGet("/calendar/google/auth", (
            GoogleCalendarProvider provider,
            IConfiguration config,
            ILogger<GoogleCalendarProvider> logger) =>
        {
            if (config.RequireOrFail(
                    logger,
                    missing => Results.Problem(statusCode: 503, detail: $"{missing[0]} not configured"),
                    "GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "GOOGLE_REDIRECT_URI") is { } configError)
            {
                return configError;
            }

            var state = Guid.NewGuid().ToString("N");
            return Results.Redirect(provider.GetAuthUrl(state));
        });

        // GET /calendar/google/callback
        // Validates CSRF state, exchanges code for tokens, redirects to frontend. When Google
        // refuses (consent declined, or a rejected client secret) it redirects to Settings →
        // Connections with ?calendar_error=<Google's error code> rather than failing with a 500.
        app.MapGet("/calendar/google/callback", async (
            string? code,
            string? state,
            string? error,
            GoogleAuthService authService,
            IConfiguration config) =>
        {
            if (state is null || !authService.ValidateAndConsumeState(state))
                return Results.BadRequest("Invalid or expired state token");

            if (error is not null || code is null)
            {
                app.Logger.LogWarning("Google sign-in was not completed: {Error}", error ?? "no code");
                return Results.Redirect(GoogleAuthService.ConnectionsErrorUrl(config["FRONTEND_URL"], error ?? "no_code"));
            }

            try
            {
                await authService.HandleCallbackAsync(code);
            }
            catch (TokenResponseException ex)
            {
                // e.g. invalid_client: GOOGLE_CLIENT_SECRET doesn't match GOOGLE_CLIENT_ID in Google Cloud Console
                app.Logger.LogError(ex, "Google rejected the token exchange: {Error} — {Description}",
                    ex.Error.Error, ex.Error.ErrorDescription);
                return Results.Redirect(GoogleAuthService.ConnectionsErrorUrl(config["FRONTEND_URL"], ex.Error.Error ?? "token_exchange_failed"));
            }

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
            // Start at midnight UTC yesterday rather than "now", so events that already ended today
            // stay on the calendar; the extra day covers the viewer's local "today" beginning before
            // UTC midnight. 60 days ahead fills the month grid through next month.
            var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
            var from  = today.AddDays(-1);
            var to    = today.AddDays(60);

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
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Failed to update task {TaskListId}/{TaskId} in Google Tasks", taskListId, taskId);
                return Results.Problem(statusCode: 502, detail: "Failed to update task in Google Tasks.");
            }
        });
    }
}

internal record ToggleTaskRequest(bool Completed);
