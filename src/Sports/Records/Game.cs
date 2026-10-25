namespace Sports.Records;

/// <summary>One side of a game. Scores are null before kickoff.</summary>
public record Competitor(
    string  Id,
    string  Abbreviation,
    string  Name,
    string  ShortName,
    string? Logo,
    int?    Score,
    int?    Shootout,
    string? Record);

/// <summary>
/// A game, normalised across ESPN's scoreboard and schedule shapes. <c>State</c> is "pre", "in"
/// or "post"; <c>Status</c> is ESPN's short detail — the clock while live ("72'", "HT", "Q3 4:12",
/// "Bot 7th"), then "FT", "Final/OT", "Postponed"…; <c>Completed</c> is false for a postponed or
/// cancelled game even though its state is "post". <c>Sport</c> is the league's ("soccer", "hockey"…). <c>Note</c> is a playoff round or similar.
/// </summary>
public record Game(
    string                Id,
    string                League,
    string                Sport,
    DateTimeOffset        Start,
    string                State,
    bool                  Completed,
    string                Status,
    Competitor            Home,
    Competitor            Away,
    IReadOnlyList<string> Broadcasts,
    string?               Note);
