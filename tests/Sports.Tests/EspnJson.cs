namespace Sports.Tests;

/// <summary>Trimmed copies of ESPN's two event shapes, as the NWSL feeds return them.</summary>
internal static class EspnJson
{
    /// <summary>A scoreboard event: scores are strings, the team has <c>logo</c>, broadcasts list <c>names</c>.</summary>
    public static string ScoreboardEvent(
        string id = "1", string date = "2026-10-25T21:00Z", string state = "in", bool completed = false,
        string detail = "72'", string homeScore = "1", string awayScore = "0",
        string homeId = "20907", string awayId = "15365", string extraHome = "", string extraAway = "") => $$"""
        {"id": "{{id}}", "date": "{{date}}", "competitions": [{
            "id": "{{id}}", "date": "{{date}}",
            "status": {"displayClock": "72'", "type": {"state": "{{state}}", "completed": {{(completed ? "true" : "false")}}, "shortDetail": "{{detail}}"} },
            "competitors": [
                {"id": "{{homeId}}", "homeAway": "home", "score": "{{homeScore}}"{{extraHome}},
                 "team": {"id": "{{homeId}}", "abbreviation": "KC", "displayName": "Kansas City Current", "shortDisplayName": "Kansas City",
                          "logo": "https://a.espncdn.com/i/teamlogos/soccer/500/{{homeId}}.png"},
                 "records": [{"summary": "15-6-7"}]},
                {"id": "{{awayId}}", "homeAway": "away", "score": "{{awayScore}}"{{extraAway}},
                 "team": {"id": "{{awayId}}", "abbreviation": "WAS", "displayName": "Washington Spirit", "shortDisplayName": "Washington",
                          "logo": "https://a.espncdn.com/i/teamlogos/soccer/500/{{awayId}}.png"},
                 "records": [{"summary": "13-8-7"}]}
            ],
            "broadcasts": [{"market": "national", "names": ["ESPN", "ESPN Deportes"]}],
            "notes": []
        }]}
        """;

    /// <summary>A schedule event: scores are <c>{value}</c> objects, teams have <c>logos</c>, broadcasts one <c>media</c>.</summary>
    public static string ScheduleEvent(
        string id = "2", string date = "2026-10-18T00:45Z", string state = "post", bool completed = true,
        string detail = "FT", double homeScore = 2, double awayScore = 3,
        string homeId = "131563", string awayId = "21422", string note = "") => $$"""
        {"id": "{{id}}", "date": "{{date}}", "competitions": [{
            "id": "{{id}}", "date": "{{date}}",
            "status": {"type": {"state": "{{state}}", "completed": {{(completed ? "true" : "false")}}, "shortDetail": "{{detail}}"} },
            "competitors": [
                {"id": "{{homeId}}", "homeAway": "home", "score": {"value": {{homeScore}}, "displayValue": "{{homeScore}}"},
                 "team": {"id": "{{homeId}}", "abbreviation": "DEN", "displayName": "Denver Summit FC", "shortDisplayName": "Denver",
                          "logos": [{"href": "https://a.espncdn.com/i/teamlogos/soccer/500/{{homeId}}.png"}]},
                 "record": [{"displayValue": "9-12-7"}]},
                {"id": "{{awayId}}", "homeAway": "away", "score": {"value": {{awayScore}}, "displayValue": "{{awayScore}}"},
                 "team": {"id": "{{awayId}}", "abbreviation": "LA", "displayName": "Angel City FC", "shortDisplayName": "Angel City",
                          "logos": [{"href": "https://a.espncdn.com/i/teamlogos/soccer/500/{{awayId}}.png"}]},
                 "record": [{"displayValue": "14-7-8"}]}
            ],
            "broadcasts": [{"type": {"shortName": "TV"}, "media": {"shortName": "ION"} }],
            "notes": [{{NoteJson(note)}}]
        }]}
        """;

    private static string NoteJson(string note) => note.Length > 0 ? "{\"headline\": \"" + note + "\"}" : "";

    public static string Events(params string[] events) => $$"""{"events": [{{string.Join(",", events)}}]}""";

    public const string Teams = """
        {"sports": [{"leagues": [{"teams": [
            {"team": {"id": "21422", "abbreviation": "LA", "displayName": "Angel City FC", "shortDisplayName": "Angel City",
                      "color": "202121", "logos": [{"href": "https://a.espncdn.com/i/teamlogos/soccer/500/21422.png"}]}},
            {"team": {"id": "22187", "abbreviation": "BAY", "displayName": "Bay FC", "shortDisplayName": "Bay",
                      "color": "0d2032", "logos": [{"href": "https://a.espncdn.com/i/teamlogos/soccer/500/22187.png"}]}}
        ]}]}]}
        """;
}
