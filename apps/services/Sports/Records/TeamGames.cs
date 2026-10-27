namespace Sports.Records;

/// <summary>
/// A followed team's live game, last result and next game. <c>Outcome</c> is the last result from
/// the team's side: "win", "loss" or "draw" (a shootout decides a level score).
/// </summary>
public record TeamGames(Team Team, Game? Live, Game? Last, string? Outcome, Game? Next);

/// <summary>Every followed team's games, and how soon the dashboard should ask again.</summary>
public record SportsBoard(IReadOnlyList<TeamGames> Teams, int NextPollSeconds);

public record FollowRequest(string League, string TeamId);
