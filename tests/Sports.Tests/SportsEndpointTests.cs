using System.Net;
using System.Net.Http.Json;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sports.Records;

namespace Sports.Tests;

/// <summary>The endpoints against a stubbed ESPN: following, the board, and the health route.</summary>
public sealed class SportsEndpointTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly FakeHttpMessageHandler _espn = new("{}");
    private readonly WebApplicationFactory<Program> _factory;

    public SportsEndpointTests()
    {
        _espn.ByPath["/apis/site/v2/sports/soccer/usa.nwsl/teams"] = (HttpStatusCode.OK, EspnJson.Teams);
        _factory = HearthApp.Create<Program>("sports", _db, host =>
            host.ConfigureTestServices(services =>
                services.AddHttpClient<EspnFetcher>().ConfigurePrimaryHttpMessageHandler(() => _espn)));
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Health_WithTestDatabase_ReturnsOk()
    {
        var response = await _factory.CreateClient().GetAsync("/sports/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Leagues_IncludesNwsl()
    {
        var leagues = await _factory.CreateClient().GetFromJsonAsync<List<League>>("/sports/leagues", ServiceDefaults.HearthJson.SnakeCaseLower);

        Assert.Contains(leagues!, l => l.Key == "nwsl" && l.Path == "soccer/usa.nwsl");
    }

    [Fact]
    public async Task Teams_UnknownLeague_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/sports/leagues/curling/teams");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Follow_KnownTeam_SavesItsDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/sports/followed", new { league = "nwsl", team_id = "21422" });

        response.EnsureSuccessStatusCode();
        var followed = await client.GetFromJsonAsync<List<Team>>("/sports/followed", ServiceDefaults.HearthJson.SnakeCaseLower);
        var team = Assert.Single(followed!);
        Assert.Equal("Angel City FC", team.Name);
        Assert.Equal("https://a.espncdn.com/i/teamlogos/soccer/500/21422.png", team.Logo);
    }

    [Fact]
    public async Task Follow_UnknownTeam_Returns404()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/sports/followed", new { league = "nwsl", team_id = "nope" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Follow_UnknownLeague_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/sports/followed", new { league = "curling", team_id = "1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unfollow_FollowedTeam_RemovesIt()
    {
        var client = _factory.CreateClient();
        (await client.PostAsJsonAsync("/sports/followed", new { league = "nwsl", team_id = "21422" })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync("/sports/followed/nwsl/21422");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<Team>>("/sports/followed", ServiceDefaults.HearthJson.SnakeCaseLower))!);
    }

    [Fact]
    public async Task Games_NoFollowedTeams_ReturnsEmptyBoard()
    {
        var board = await _factory.CreateClient().GetFromJsonAsync<SportsBoard>("/sports/games", ServiceDefaults.HearthJson.SnakeCaseLower);

        Assert.Empty(board!.Teams);
        Assert.Equal(300, board.NextPollSeconds);
    }
}
