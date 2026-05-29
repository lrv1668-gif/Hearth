using Calendar;
using Calendar.Providers;
using Calendar.Providers.Google;
using Calendar.Records;

namespace calendar.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForCalendar(this WebApplication app)
    {
        app.Services.GetRequiredService<CalendarStore>().Migrate();
        app.AddCalendarEndpoints();
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

        // DELETE /calendar/google/auth — clears token + event cache, returns 204
        app.MapDelete("/calendar/google/auth", (GoogleCalendarProvider provider) =>
        {
            provider.Disconnect();
            return Results.NoContent();
        });

        // GET /calendar/events — aggregates across all authenticated providers.
        // GetEventsAsync catches all exceptions internally and returns empty on failure,
        // so no outer try/catch is needed here.
        app.MapGet("/calendar/events", async (
            IEnumerable<ICalendarProvider> providers,
            CancellationToken ct) =>
        {
            var from = DateTimeOffset.UtcNow;
            var to   = from.AddDays(365);

            var fetches = providers
                .Where(p => p.IsAuthenticated)
                .Select(p => p.GetEventsAsync(from, to, ct));

            var all = await Task.WhenAll(fetches);
            return Results.Ok(all.SelectMany(e => e).ToList());
        });
    }
}
