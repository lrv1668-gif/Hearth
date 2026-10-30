using System.Net;
using Hearth.TestUtilities;

namespace Sports.Tests;

public sealed class EspnFetcherTests
{
    private static readonly Records.League Nwsl = Leagues.Find("nwsl")!;

    private static (EspnFetcher Fetcher, FakeHttpMessageHandler Handler) MakeFetcher(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(json, status);
        return (new EspnFetcher(new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task FetchScoreboardAsync_LiveGame_MapsStringScoresClockAndBroadcasts()
    {
        var (fetcher, handler) = MakeFetcher(EspnJson.Events(EspnJson.ScoreboardEvent()));

        var game = Assert.Single(await fetcher.FetchScoreboardAsync(Nwsl));

        Assert.Equal("https://site.api.espn.com/apis/site/v2/sports/soccer/usa.nwsl/scoreboard", handler.LastRequestUri!.ToString());
        Assert.Equal("nwsl", game.League);
        Assert.Equal("soccer", game.Sport);
        Assert.Equal("in", game.State);
        Assert.Equal("72'", game.Status);
        Assert.Equal(1, game.Home.Score);
        Assert.Equal(0, game.Away.Score);
        Assert.Equal("KC", game.Home.Abbreviation);
        Assert.Equal("15-6-7", game.Home.Record);
        Assert.Equal("https://a.espncdn.com/i/teamlogos/soccer/500/20907.png", game.Home.Logo);
        Assert.Equal(["ESPN", "ESPN Deportes"], game.Broadcasts);
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 21, 0, 0, TimeSpan.Zero), game.Start);
    }

    [Fact]
    public async Task FetchScheduleAsync_Result_MapsObjectScoresLogosAndMedia()
    {
        var (fetcher, _) = MakeFetcher(EspnJson.Events(EspnJson.ScheduleEvent(note: "Quarterfinal")));

        var game = Assert.Single(await fetcher.FetchScheduleAsync(Nwsl, "21422", fixtures: false));

        Assert.True(game.Completed);
        Assert.Equal("FT", game.Status);
        Assert.Equal(2, game.Home.Score);
        Assert.Equal(3, game.Away.Score);
        Assert.Equal("14-7-8", game.Away.Record);
        Assert.Equal("https://a.espncdn.com/i/teamlogos/soccer/500/21422.png", game.Away.Logo);
        Assert.Equal(["ION"], game.Broadcasts);
        Assert.Equal("Quarterfinal", game.Note);
    }

    [Fact]
    public async Task FetchScheduleAsync_Fixtures_AsksForFixturesAndBlanksPregameScores()
    {
        var (fetcher, handler) = MakeFetcher(EspnJson.Events(
            EspnJson.ScheduleEvent(state: "pre", completed: false, detail: "Sun, November 1st", homeScore: 0, awayScore: 0)));

        var game = Assert.Single(await fetcher.FetchScheduleAsync(Nwsl, "21422", fixtures: true));

        Assert.Equal("/apis/site/v2/sports/soccer/usa.nwsl/teams/21422/schedule", handler.LastRequestUri!.AbsolutePath);
        Assert.Equal("?fixture=true", handler.LastRequestUri.Query);
        Assert.Null(game.Home.Score);
        Assert.Null(game.Away.Score);
    }

    [Fact]
    public async Task FetchScoreboardAsync_ShootoutScore_IsMapped()
    {
        var (fetcher, _) = MakeFetcher(EspnJson.Events(EspnJson.ScoreboardEvent(
            state: "post", completed: true, detail: "FT-Pens", homeScore: "2", awayScore: "2",
            extraHome: """, "shootoutScore": 4""", extraAway: """, "shootoutScore": 3""")));

        var game = Assert.Single(await fetcher.FetchScoreboardAsync(Nwsl));

        Assert.Equal(4, game.Home.Shootout);
        Assert.Equal(3, game.Away.Shootout);
    }

    [Fact]
    public async Task FetchScoreboardAsync_EventWithoutCompetitors_IsSkipped()
    {
        var (fetcher, _) = MakeFetcher("""{"events": [{"id": "9", "date": "2026-10-25T21:00Z", "competitions": [{"competitors": []}]}]}""");

        Assert.Empty(await fetcher.FetchScoreboardAsync(Nwsl));
    }

    [Fact]
    public async Task FetchScoreboardAsync_NoEvents_ReturnsEmpty()
    {
        var (fetcher, _) = MakeFetcher("{}");

        Assert.Empty(await fetcher.FetchScoreboardAsync(Nwsl));
    }

    [Fact]
    public async Task FetchTeamsAsync_Teams_MapsAndSortsByName()
    {
        var (fetcher, _) = MakeFetcher(EspnJson.Teams);

        var teams = await fetcher.FetchTeamsAsync(Nwsl);

        Assert.Equal(["Angel City FC", "Bay FC"], teams.Select(t => t.Name));
        Assert.Equal("nwsl", teams[0].League);
        Assert.Equal("21422", teams[0].Id);
        Assert.Equal("LA", teams[0].Abbreviation);
        Assert.Equal("Angel City", teams[0].ShortName);
        Assert.Equal("202121", teams[0].Color);
        Assert.Equal("https://a.espncdn.com/i/teamlogos/soccer/500/21422.png", teams[0].Logo);
    }

    [Fact]
    public async Task FetchScoreboardAsync_HttpError_Throws()
    {
        var (fetcher, _) = MakeFetcher("", HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<HttpRequestException>(() => fetcher.FetchScoreboardAsync(Nwsl));
    }

    [Fact]
    public async Task FetchScoreboardAsync_MalformedJson_Throws()
    {
        var (fetcher, _) = MakeFetcher("not-json");

        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() => fetcher.FetchScoreboardAsync(Nwsl));
    }
}
