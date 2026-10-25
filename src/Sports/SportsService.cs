using Sports.Records;
using ZiggyCreatures.Caching.Fusion;

namespace Sports;

/// <summary>
/// Builds each followed team's live game, last result and next game from ESPN. A team's season
/// changes once a game ends, so schedules are cached for half an hour; the league scoreboard
/// carries the live clock and is cached for 30 seconds while a game is on, 5 minutes otherwise —
/// so ESPN is only asked often while something is being played.
/// </summary>
public sealed class SportsService(EspnFetcher fetcher, IFusionCache cache, ILogger<SportsService> logger)
{
    internal static readonly TimeSpan LivePoll = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan IdlePoll = TimeSpan.FromMinutes(5);

    private static readonly FusionCacheEntryOptions TeamsOptions = new()
    {
        Duration = TimeSpan.FromHours(24),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(30),
        FailSafeThrottleDuration = TimeSpan.FromMinutes(5),
    };

    private static readonly FusionCacheEntryOptions ScheduleOptions = new()
    {
        Duration = TimeSpan.FromMinutes(30),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(3),
        FailSafeThrottleDuration = TimeSpan.FromMinutes(5),
    };

    // Duration is set per fetch: short while a game is live
    private static readonly FusionCacheEntryOptions ScoreboardOptions = new()
    {
        Duration = IdlePoll,
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(6),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
    };

    public async Task<List<Team>> TeamsAsync(League league, CancellationToken ct = default) =>
        await cache.GetOrSetAsync<List<Team>>(
            $"sports:teams:{league.Key}", (_, t) => fetcher.FetchTeamsAsync(league, t), TeamsOptions, ct);

    public async Task<SportsBoard> BoardAsync(IReadOnlyList<Team> followed, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // One scoreboard per league, however many of its teams are followed
        var scoreboards = new Dictionary<string, List<Game>>();
        foreach (var key in followed.Select(t => t.League).Distinct())
        {
            if (Leagues.Find(key) is not { } league) continue;
            scoreboards[key] = await TryAsync(() => ScoreboardAsync(league, ct), $"the {league.Name} scoreboard") ?? [];
        }

        var teams = new List<TeamGames>();
        foreach (var team in followed)
        {
            if (Leagues.Find(team.League) is not { } league) continue;
            var results  = await TryAsync(() => ScheduleAsync(league, team.Id, ScheduleKind.Results, ct), $"{team.Name}'s results");
            // Out of the postseason (or into the offseason): the last result is the regular-season finale
            if (results is not null && !results.Any(g => g.Completed))
                results = await TryAsync(() => ScheduleAsync(league, team.Id, ScheduleKind.RegularSeason, ct), $"{team.Name}'s season");
            var fixtures = league.Sport == "soccer"
                ? await TryAsync(() => ScheduleAsync(league, team.Id, ScheduleKind.Fixtures, ct), $"{team.Name}'s fixtures")
                : null;

            var games = (results ?? []).Concat(fixtures ?? []).Concat(scoreboards.GetValueOrDefault(team.League, []));
            teams.Add(Summarize(team, games, now));
        }

        var live = teams.Any(t => t.Live is not null);
        return new SportsBoard(teams, (int)(live ? LivePoll : IdlePoll).TotalSeconds);
    }

    /// <summary>
    /// Picks the team's games out of <paramref name="games"/>, where a later copy of the same game
    /// (the scoreboard, listed last) replaces an earlier one.
    /// </summary>
    internal static TeamGames Summarize(Team team, IEnumerable<Game> games, DateTimeOffset now)
    {
        var mine = new Dictionary<string, Game>();
        foreach (var g in games)
            if (g.Home.Id == team.Id || g.Away.Id == team.Id)
                mine[g.Id] = g;

        var live = mine.Values.Where(g => g.State == "in").OrderBy(g => g.Start).FirstOrDefault();
        var last = mine.Values.Where(g => g.State == "post" && g.Completed && g.Start <= now)
            .OrderByDescending(g => g.Start).FirstOrDefault();
        var next = mine.Values.Where(g => g.State == "pre" && g.Start > now - TimeSpan.FromHours(6))
            .OrderBy(g => g.Start).FirstOrDefault();

        return new TeamGames(team, live, last, last is null ? null : Outcome(last, team.Id), next);
    }

    /// <summary>"win", "loss" or "draw" from <paramref name="teamId"/>'s side; a shootout breaks a level score.</summary>
    internal static string? Outcome(Game game, string teamId)
    {
        var (us, them) = game.Home.Id == teamId ? (game.Home, game.Away) : (game.Away, game.Home);
        if (us.Score is not { } a || them.Score is not { } b) return null;
        if (a == b && us.Shootout is { } sa && them.Shootout is { } sb) (a, b) = (sa, sb);
        return a > b ? "win" : a < b ? "loss" : "draw";
    }

    private enum ScheduleKind { Results, Fixtures, RegularSeason }

    private async Task<List<Game>> ScheduleAsync(League league, string teamId, ScheduleKind kind, CancellationToken ct) =>
        await cache.GetOrSetAsync<List<Game>>(
            $"sports:schedule:{league.Key}:{teamId}:{kind.ToString().ToLowerInvariant()}",
            (_, t) => fetcher.FetchScheduleAsync(
                league, teamId, fixtures: kind == ScheduleKind.Fixtures, regularSeason: kind == ScheduleKind.RegularSeason, t),
            ScheduleOptions, ct);

    private async Task<List<Game>> ScoreboardAsync(League league, CancellationToken ct) =>
        await cache.GetOrSetAsync<List<Game>>(
            $"sports:scoreboard:{league.Key}",
            async (ctx, t) =>
            {
                var games = await fetcher.FetchScoreboardAsync(league, t);
                ctx.Options.Duration = games.Any(g => g.State == "in") ? LivePoll : IdlePoll;
                return games;
            },
            ScoreboardOptions, ct);

    /// <summary>One failed feed drops only its games, not the whole board.</summary>
    private async Task<List<Game>?> TryAsync(Func<Task<List<Game>>> fetch, string what)
    {
        try { return await fetch(); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to fetch {What} from ESPN", what);
            return null;
        }
    }
}
