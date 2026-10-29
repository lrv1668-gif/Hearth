using Sports.Records;

namespace Sports;

/// <summary>The leagues ESPN's site API covers that Hearth offers. Keys are what's stored and sent.</summary>
public static class Leagues
{
    public static readonly IReadOnlyList<League> All =
    [
        new("nwsl", "NWSL",           "soccer",     "soccer/usa.nwsl"),
        new("mls",  "MLS",            "soccer",     "soccer/usa.1"),
        new("epl",  "Premier League", "soccer",     "soccer/eng.1"),
        new("nfl",  "NFL",            "football",   "football/nfl"),
        new("nba",  "NBA",            "basketball", "basketball/nba"),
        new("wnba", "WNBA",           "basketball", "basketball/wnba"),
        new("mlb",  "MLB",            "baseball",   "baseball/mlb"),
        new("nhl",  "NHL",            "hockey",     "hockey/nhl"),
    ];

    public static League? Find(string key) => All.FirstOrDefault(l => l.Key == key);
}
