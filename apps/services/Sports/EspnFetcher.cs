using System.Globalization;
using System.Text.Json;
using Sports.Records;

namespace Sports;

/// <summary>
/// ESPN's public site API (site.api.espn.com) — undocumented but keyless. The scoreboard and the
/// team schedule describe the same events in slightly different shapes (a score is a string on
/// one and an object on the other, logos and broadcasts are nested differently), so both go
/// through <see cref="ParseEvent"/>.
/// </summary>
public sealed class EspnFetcher(HttpClient http)
{
    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports";

    public async Task<List<Team>> FetchTeamsAsync(League league, CancellationToken ct = default)
    {
        using var doc = await GetAsync($"{BaseUrl}/{league.Path}/teams?limit=500", ct);

        var teams = new List<Team>();
        foreach (var sport in Array(doc.RootElement, "sports"))
        foreach (var l in Array(sport, "leagues"))
        foreach (var entry in Array(l, "teams"))
        {
            if (!entry.TryGetProperty("team", out var t)) continue;
            teams.Add(new Team(
                league.Key,
                Str(t, "id") ?? "",
                Str(t, "abbreviation") ?? "",
                Str(t, "displayName") ?? "",
                Str(t, "shortDisplayName") ?? Str(t, "displayName") ?? "",
                Logo(t),
                Str(t, "color")));
        }
        return teams.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// A team's season. For soccer the plain schedule holds only results and <paramref name="fixtures"/>
    /// asks for the games still to play; other sports return the whole season either way. ESPN answers
    /// with the league's current season type, so in the postseason a team that missed it gets nothing
    /// back — <paramref name="regularSeason"/> asks for the regular season instead.
    /// </summary>
    public async Task<List<Game>> FetchScheduleAsync(
        League league, string teamId, bool fixtures, bool regularSeason = false, CancellationToken ct = default)
    {
        var query = fixtures ? "?fixture=true" : regularSeason ? "?seasontype=2" : "";
        var url = $"{BaseUrl}/{league.Path}/teams/{Uri.EscapeDataString(teamId)}/schedule{query}";
        using var doc = await GetAsync(url, ct);
        return ParseEvents(doc.RootElement, league);
    }

    /// <summary>The league's current scoreboard — today's games, with the live clock while they're on.</summary>
    public async Task<List<Game>> FetchScoreboardAsync(League league, CancellationToken ct = default)
    {
        using var doc = await GetAsync($"{BaseUrl}/{league.Path}/scoreboard", ct);
        return ParseEvents(doc.RootElement, league);
    }

    private async Task<JsonDocument> GetAsync(string url, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }

    internal static List<Game> ParseEvents(JsonElement root, League league) =>
        Array(root, "events").Select(e => ParseEvent(e, league)).OfType<Game>().ToList();

    internal static Game? ParseEvent(JsonElement e, League league)
    {
        var competition = Array(e, "competitions").FirstOrDefault();
        if (competition.ValueKind != JsonValueKind.Object) return null;

        var competitors = Array(competition, "competitors").ToList();
        var home = competitors.FirstOrDefault(c => Str(c, "homeAway") == "home");
        var away = competitors.FirstOrDefault(c => Str(c, "homeAway") == "away");
        if (home.ValueKind != JsonValueKind.Object || away.ValueKind != JsonValueKind.Object) return null;

        if (!DateTimeOffset.TryParse(Str(competition, "date") ?? Str(e, "date"), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var start))
            return null;

        // The scoreboard carries status on the competition and the event; the schedule only on the competition
        var status = competition.TryGetProperty("status", out var s) ? s
            : e.TryGetProperty("status", out var es) ? es : default;
        var type = status.ValueKind == JsonValueKind.Object && status.TryGetProperty("type", out var ty) ? ty : default;
        var state = Str(type, "state") ?? "pre";
        var completed = type.ValueKind == JsonValueKind.Object && type.TryGetProperty("completed", out var c) && c.ValueKind == JsonValueKind.True;

        // Both feeds report "0" for a game that hasn't started
        var started = state != "pre";

        return new Game(
            Str(e, "id") ?? Str(competition, "id") ?? "",
            league.Key,
            league.Sport,
            start,
            state,
            completed,
            Str(type, "shortDetail") ?? Str(type, "detail") ?? "",
            ParseCompetitor(home, started),
            ParseCompetitor(away, started),
            ParseBroadcasts(competition),
            Array(competition, "notes").Select(n => Str(n, "headline")).FirstOrDefault(h => !string.IsNullOrEmpty(h)));
    }

    private static Competitor ParseCompetitor(JsonElement c, bool started)
    {
        var team = c.TryGetProperty("team", out var t) ? t : default;

        // Scoreboard records are [{summary}], schedule records [{displayValue}]
        var record = Array(c, "records").Concat(Array(c, "record"))
            .Select(r => Str(r, "summary") ?? Str(r, "displayValue"))
            .FirstOrDefault(r => !string.IsNullOrEmpty(r));

        return new Competitor(
            Str(team, "id") ?? Str(c, "id") ?? "",
            Str(team, "abbreviation") ?? "",
            Str(team, "displayName") ?? "",
            Str(team, "shortDisplayName") ?? Str(team, "displayName") ?? "",
            Logo(team),
            started ? Number(c, "score") : null,
            started ? Number(c, "shootoutScore") : null,
            record);
    }

    /// <summary>Scoreboard broadcasts list <c>names</c>; schedule broadcasts one <c>media.shortName</c> each.</summary>
    private static IReadOnlyList<string> ParseBroadcasts(JsonElement competition)
    {
        var names = new List<string>();
        foreach (var b in Array(competition, "broadcasts"))
        {
            if (b.TryGetProperty("names", out var n) && n.ValueKind == JsonValueKind.Array)
                names.AddRange(n.EnumerateArray().Select(x => x.GetString()).OfType<string>());
            else if (b.TryGetProperty("media", out var m) && Str(m, "shortName") is { } media)
                names.Add(media);
        }
        return names.Where(x => x.Length > 0).Distinct().ToList();
    }

    /// <summary>A score is "2" on the scoreboard and <c>{"value": 2.0}</c> on the schedule.</summary>
    private static int? Number(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Object && !v.TryGetProperty("value", out v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => (int)Math.Round(v.GetDouble()),
            JsonValueKind.String when double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                => (int)Math.Round(d),
            _ => null,
        };
    }

    /// <summary>Scoreboard teams carry <c>logo</c>; teams and schedules a <c>logos</c> list, default first.</summary>
    private static string? Logo(JsonElement team) =>
        Str(team, "logo") is { Length: > 0 } logo ? logo
            : Array(team, "logos").Select(l => Str(l, "href")).FirstOrDefault(h => !string.IsNullOrEmpty(h));

    private static string? Str(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static IEnumerable<JsonElement> Array(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray()
            : [];
}
