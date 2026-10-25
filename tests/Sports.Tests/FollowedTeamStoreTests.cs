using Hearth.TestUtilities;
using Sports.Records;
using Xunit;

namespace Sports.Tests;

public sealed class FollowedTeamStoreTests
{
    private static FollowedTeamStore Migrated(TempDatabase tmp)
    {
        var store = new FollowedTeamStore(tmp.Db);
        store.Migrate();
        return store;
    }

    private static Team Team(string id, string name, string league = "nwsl") =>
        new(league, id, name[..3].ToUpperInvariant(), name, name, $"https://logo/{id}.png", "000000");

    [Fact]
    public void Follow_Teams_ListsThemInTheOrderFollowed()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Follow(Team("2", "Portland Thorns"));
        store.Follow(Team("1", "Angel City FC"));

        Assert.Equal(["Portland Thorns", "Angel City FC"], store.List().Select(t => t.Name));
    }

    [Fact]
    public void Follow_SameTeamAgain_UpdatesItInPlace()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.Follow(Team("1", "Angel City FC"));
        store.Follow(Team("2", "Portland Thorns"));

        store.Follow(Team("1", "Angel City FC") with { Logo = "https://logo/new.png" });

        var teams = store.List();
        Assert.Equal(2, teams.Count);
        Assert.Equal("https://logo/new.png", teams[0].Logo);
    }

    [Fact]
    public void Follow_SameIdInAnotherLeague_IsADifferentTeam()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        store.Follow(Team("1", "Angel City FC"));
        store.Follow(Team("1", "Atlanta Hawks", league: "nba"));

        Assert.Equal(2, store.List().Count);
    }

    [Fact]
    public void Unfollow_Team_RemovesOnlyThatTeam()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.Follow(Team("1", "Angel City FC"));
        store.Follow(Team("2", "Portland Thorns"));

        store.Unfollow("nwsl", "1");

        Assert.Equal("Portland Thorns", Assert.Single(store.List()).Name);
    }

    [Fact]
    public void Migrate_Twice_KeepsTeams()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        store.Follow(Team("1", "Angel City FC"));

        store.Migrate();

        Assert.Single(store.List());
    }
}
