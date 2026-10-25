using ServiceDefaults;
using Sports.Records;

namespace Sports.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForSports(this WebApplication app)
    {
        app.Services.GetRequiredService<FollowedTeamStore>().Migrate();
        app.AddSportsEndpoints();
        app.MapHearthHealth("sports");
    }

    private static void AddSportsEndpoints(this WebApplication app)
    {
        app.MapGet("/sports/leagues", () => Leagues.All);

        app.MapGet("/sports/leagues/{league}/teams", async (string league, SportsService sports, CancellationToken ct) =>
        {
            if (Leagues.Find(league) is not { } found) return Results.NotFound();
            try
            {
                return Results.Ok(await sports.TeamsAsync(found, ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                app.Logger.LogError(ex, "Failed to fetch {League} teams from ESPN", found.Name);
                return Results.Json(new { error = "teams fetch failed" }, statusCode: 502);
            }
        });

        app.MapGet("/sports/followed", (FollowedTeamStore store) => store.List());

        // Saves the team's names and logo with it, so the list shows without asking ESPN
        app.MapPost("/sports/followed", async (FollowRequest req, FollowedTeamStore store, SportsService sports, CancellationToken ct) =>
        {
            if (Leagues.Find(req.League) is not { } league) return Results.BadRequest("unknown league");
            List<Team> teams;
            try
            {
                teams = await sports.TeamsAsync(league, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                app.Logger.LogError(ex, "Failed to fetch {League} teams from ESPN", league.Name);
                return Results.Json(new { error = "teams fetch failed" }, statusCode: 502);
            }
            if (teams.FirstOrDefault(t => t.Id == req.TeamId) is not { } team) return Results.NotFound();

            store.Follow(team);
            return Results.Ok(store.List());
        });

        app.MapDelete("/sports/followed/{league}/{teamId}", (string league, string teamId, FollowedTeamStore store) =>
        {
            store.Unfollow(league, teamId);
            return Results.NoContent();
        });

        app.MapGet("/sports/games", (FollowedTeamStore store, SportsService sports, CancellationToken ct) =>
            sports.BoardAsync(store.List(), ct));
    }
}
