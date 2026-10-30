using Hearth.TestUtilities;
using Sports.Records;

namespace Sports.Tests;

public sealed class FollowedTeamStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly FollowedTeamStore _store;

    public FollowedTeamStoreTests()
    {
        _store = new FollowedTeamStore(_db.Db);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    private static Team Team(string id, string name, string league = "nwsl") =>
        new(league, id, name[..3].ToUpperInvariant(), name, name, $"https://logo/{id}.png", "000000");

    [Fact]
    public void Follow_Teams_ListsThemInTheOrderFollowed()
    {
        _store.Follow(Team("2", "Portland Thorns"));
        _store.Follow(Team("1", "Angel City FC"));

        Assert.Equal(["Portland Thorns", "Angel City FC"], _store.List().Select(t => t.Name));
    }

    [Fact]
    public void Follow_SameTeamAgain_UpdatesItInPlace()
    {
        _store.Follow(Team("1", "Angel City FC"));
        _store.Follow(Team("2", "Portland Thorns"));

        _store.Follow(Team("1", "Angel City FC") with { Logo = "https://logo/new.png" });

        var teams = _store.List();
        Assert.Equal(2, teams.Count);
        Assert.Equal("https://logo/new.png", teams[0].Logo);
    }

    [Fact]
    public void Follow_SameIdInAnotherLeague_IsADifferentTeam()
    {
        _store.Follow(Team("1", "Angel City FC"));
        _store.Follow(Team("1", "Atlanta Hawks", league: "nba"));

        Assert.Equal(2, _store.List().Count);
    }

    [Fact]
    public void Unfollow_Team_RemovesOnlyThatTeam()
    {
        _store.Follow(Team("1", "Angel City FC"));
        _store.Follow(Team("2", "Portland Thorns"));

        _store.Unfollow("nwsl", "1");

        Assert.Equal("Portland Thorns", Assert.Single(_store.List()).Name);
    }

    [Fact]
    public void Migrate_Twice_KeepsTeams()
    {
        _store.Follow(Team("1", "Angel City FC"));

        _store.Migrate();

        Assert.Single(_store.List());
    }
}
