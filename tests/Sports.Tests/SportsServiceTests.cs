using System.Net;
using Hearth.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Sports.Records;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Sports.Tests;

public sealed class SportsServiceTests
{
    private static readonly Team AngelCity = new("nwsl", "21422", "LA", "Angel City FC", "Angel City", null, null);
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 22, 0, 0, TimeSpan.Zero);

    private static Competitor Side(string id, int? score, int? shootout = null) =>
        new(id, id, id, id, null, score, shootout, null);

    private static Game Game(
        string id, string state, DateTimeOffset start, int? us = null, int? them = null,
        bool completed = true, bool home = true, int? usPens = null, int? themPens = null) =>
        new(id, "nwsl", "soccer", start, state, completed, "",
            home ? Side("21422", us, usPens) : Side("999", them, themPens),
            home ? Side("999", them, themPens) : Side("21422", us, usPens),
            [], null);

    [Theory]
    [InlineData(2, 0, null, null, "win")]
    [InlineData(0, 1, null, null, "loss")]
    [InlineData(1, 1, null, null, "draw")]
    [InlineData(2, 2, 4, 3, "win")]
    [InlineData(2, 2, 3, 5, "loss")]
    public void Outcome_Scores_FromTheTeamsSide(int us, int them, int? usPens, int? themPens, string expected)
    {
        Assert.Equal(expected, SportsService.Outcome(Game("1", "post", Now, us, them, usPens: usPens, themPens: themPens), "21422"));
        Assert.Equal(expected, SportsService.Outcome(Game("1", "post", Now, us, them, home: false, usPens: usPens, themPens: themPens), "21422"));
    }

    [Fact]
    public void Summarize_Season_PicksLiveLatestResultAndNextGame()
    {
        var games = new[]
        {
            Game("old",  "post", Now.AddDays(-14), 1, 0),
            Game("last", "post", Now.AddDays(-7), 2, 3),
            Game("ppd",  "post", Now.AddDays(-2), completed: false),
            Game("live", "in",   Now.AddMinutes(-60), 1, 0, completed: false),
            Game("far",  "pre",  Now.AddDays(14), completed: false),
            Game("next", "pre",  Now.AddDays(7), completed: false),
            new("other", "nwsl", "soccer", Now, "in", false, "", Side("1", 0), Side("2", 0), [], null),
        };

        var summary = SportsService.Summarize(AngelCity, games, Now);

        Assert.Equal("live", summary.Live?.Id);
        Assert.Equal("last", summary.Last?.Id);
        Assert.Equal("loss", summary.Outcome);
        Assert.Equal("next", summary.Next?.Id);
    }

    [Fact]
    public void Summarize_SameGameTwice_LaterCopyWins()
    {
        // The schedule still says "pre" while the scoreboard (listed last) has it live
        var games = new[]
        {
            Game("g", "pre", Now.AddMinutes(-30), completed: false),
            Game("g", "in",  Now.AddMinutes(-30), 0, 0, completed: false),
        };

        var summary = SportsService.Summarize(AngelCity, games, Now);

        Assert.Equal("g", summary.Live?.Id);
        Assert.Null(summary.Next);
    }

    [Fact]
    public void Summarize_NoGames_ReturnsEmptySummary()
    {
        var summary = SportsService.Summarize(AngelCity, [], Now);

        Assert.Null(summary.Live);
        Assert.Null(summary.Last);
        Assert.Null(summary.Outcome);
        Assert.Null(summary.Next);
    }

    [Fact]
    public async Task BoardAsync_LiveGame_PollsSoonAndMergesScoreboard()
    {
        var handler = new FakeHttpMessageHandler("{}");
        handler.ByPath["/apis/site/v2/sports/soccer/usa.nwsl/teams/21422/schedule"] =
            (HttpStatusCode.OK, EspnJson.Events(EspnJson.ScheduleEvent()));
        handler.ByPath["/apis/site/v2/sports/soccer/usa.nwsl/scoreboard"] =
            (HttpStatusCode.OK, EspnJson.Events(EspnJson.ScoreboardEvent(homeId: "21422")));
        var service = new SportsService(new EspnFetcher(new HttpClient(handler)), new FusionCache(new FusionCacheOptions()),
            NullLogger<SportsService>.Instance);

        var board = await service.BoardAsync([AngelCity]);

        var team = Assert.Single(board.Teams);
        Assert.Equal("1", team.Live?.Id);
        Assert.Equal("2", team.Last?.Id);
        Assert.Equal("win", team.Outcome);
        Assert.Equal(30, board.NextPollSeconds);
    }

    [Fact]
    public async Task BoardAsync_OutOfThePostseason_FallsBackToTheRegularSeason()
    {
        // In the postseason ESPN's default schedule is empty for a team that missed it
        var handler = new FakeHttpMessageHandler("{}");
        handler.ByPath["/apis/site/v2/sports/soccer/usa.nwsl/teams/21422/schedule"] = (HttpStatusCode.OK, """{"events": []}""");
        var regular = EspnJson.Events(EspnJson.ScheduleEvent());
        var service = new SportsService(new EspnFetcher(new HttpClient(new RegularSeasonHandler(handler, regular))),
            new FusionCache(new FusionCacheOptions()), NullLogger<SportsService>.Instance);

        var board = await service.BoardAsync([AngelCity]);

        Assert.Equal("2", Assert.Single(board.Teams).Last?.Id);
    }

    /// <summary>Answers <c>?seasontype=2</c> with <paramref name="json"/>, everything else from <paramref name="inner"/>.</summary>
    private sealed class RegularSeasonHandler(FakeHttpMessageHandler inner, string json) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            request.RequestUri!.Query == "?seasontype=2"
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) })
                : base.SendAsync(request, ct);
    }

    [Fact]
    public async Task BoardAsync_EspnDown_ReturnsTeamWithNoGames()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.ServiceUnavailable);
        var service = new SportsService(new EspnFetcher(new HttpClient(handler)), new FusionCache(new FusionCacheOptions()),
            NullLogger<SportsService>.Instance);

        var board = await service.BoardAsync([AngelCity]);

        var team = Assert.Single(board.Teams);
        Assert.Null(team.Last);
        Assert.Equal(300, board.NextPollSeconds);
    }
}
