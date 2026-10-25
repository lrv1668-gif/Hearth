using System.Net;
using System.Net.Http.Json;
using Data.Abstractions;
using Display.Records;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ServiceDefaults;
using Xunit;

namespace Display.Tests;

/// <summary>/display/mode and its schedule and override, plus <see cref="ModeStore"/> round-trips.</summary>
public sealed class ModeEndpointTests : IDisposable
{
    // Monday 19 October 2026, 12:00 UTC
    private static readonly DateTimeOffset Noon = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _db = new();
    private readonly FakeTimeProvider _clock = new(Noon);
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ModeEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("TZ", "Etc/UTC");
            host.ConfigureTestServices(services =>
            {
                services.AddKeyedSingleton<IDatabase>("display", _db.Db);
                services.AddSingleton<TimeProvider>(_clock);
            });
        });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    private async Task<ResolvedMode> Mode() =>
        (await _client.GetFromJsonAsync<ResolvedMode>("/display/mode", HearthJson.SnakeCaseLower))!;

    private Task<HttpResponseMessage> PutSchedule(object schedule) =>
        _client.PutAsJsonAsync("/display/mode/schedule", schedule);

    [Fact]
    public async Task GetMode_NothingSaved_ReturnsAmbientDefault()
    {
        Assert.Equal(new ResolvedMode(FrameModes.Ambient, "default", null), await Mode());
    }

    [Fact]
    public async Task PutSchedule_RuleCoveringNow_ModeFollowsIt()
    {
        var put = await PutSchedule(new
        {
            default_mode = "ambient",
            rules = new[] { new { mode = "daily", days = new[] { "mon" }, start = "11:00", end = "13:00" } },
        });

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal(new ResolvedMode(FrameModes.Daily, "schedule", Noon.AddHours(1)), await Mode());
    }

    [Fact]
    public async Task GetSchedule_ReturnsRulesSnakeCasedWithoutParsedTimes()
    {
        await PutSchedule(new
        {
            default_mode = "night",
            rules = new[] { new { mode = "daily", days = new[] { "mon" }, start = "07:00", end = "09:00" } },
        });

        var json = await _client.GetStringAsync("/display/mode/schedule");

        Assert.Contains("\"default_mode\":\"night\"", json);
        Assert.Contains("\"start\":\"07:00\"", json);
        Assert.DoesNotContain("start_time", json);
    }

    [Fact]
    public async Task PutSchedule_Invalid_Returns400()
    {
        var put = await PutSchedule(new
        {
            default_mode = "ambient",
            rules = new[] { new { mode = "daily", days = new[] { "someday" }, start = "07:00", end = "09:00" } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task PutOverride_ThenItExpires_FallsBackToSchedule()
    {
        await _client.PutAsJsonAsync("/display/mode/override", new { mode = "countdown", until = Noon.AddHours(1) });
        Assert.Equal(new ResolvedMode(FrameModes.Countdown, "override", Noon.AddHours(1)), await Mode());

        _clock.Advance(TimeSpan.FromHours(2));

        Assert.Equal("default", (await Mode()).Reason);
    }

    [Fact]
    public async Task PutOverride_UntilInThePast_Returns400()
    {
        var put = await _client.PutAsJsonAsync("/display/mode/override", new { mode = "daily", until = Noon.AddMinutes(-1) });

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task DeleteOverride_ForAnotherPhoto_KeepsThePin()
    {
        await _client.PutAsJsonAsync("/display/mode/override", new { mode = "photo", photo_id = "abc" });

        await _client.DeleteAsync("/display/mode/override?photo_id=other");
        Assert.Equal("abc", (await Mode()).PhotoId);

        await _client.DeleteAsync("/display/mode/override?photo_id=abc");
        Assert.Equal("default", (await Mode()).Reason);
    }

    [Fact]
    public async Task DeleteOverride_NoPhotoId_ClearsAnyOverride()
    {
        await _client.PutAsJsonAsync("/display/mode/override", new { mode = "night" });

        var response = await _client.DeleteAsync("/display/mode/override");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("default", (await Mode()).Reason);
    }

    [Fact]
    public async Task PostNext_HoldsTheNextModeUntilTheScheduleChanges()
    {
        await PutSchedule(new
        {
            default_mode = "ambient",
            rules = new[] { new { mode = "night", days = new[] { "mon" }, start = "22:00", end = "23:00" } },
        });

        var response = await _client.PostAsync("/display/mode/next", null);
        var next = await response.Content.ReadFromJsonAsync<ResolvedMode>(HearthJson.SnakeCaseLower);

        Assert.Equal(new ResolvedMode(FrameModes.Daily, "override", Noon.AddHours(10)), next);
        Assert.Equal(FrameModes.Daily, (await Mode()).Mode);
    }

    [Fact]
    public async Task PostNext_InARotatingRule_HoldsOnlyUntilTheNextStep()
    {
        await PutSchedule(new
        {
            default_mode = "ambient",
            rules = new[]
            {
                new { mode = "daily", days = new[] { "mon" }, start = "00:00", end = "00:00", rotate = new[] { "photo" }, rotate_minutes = 60 },
            },
        });
        Assert.Equal(new ResolvedMode(FrameModes.Daily, "schedule", Noon.AddHours(1)), await Mode());

        var response = await _client.PostAsync("/display/mode/next", null);
        var next = await response.Content.ReadFromJsonAsync<ResolvedMode>(HearthJson.SnakeCaseLower);

        Assert.Equal(new ResolvedMode(FrameModes.Photo, "override", Noon.AddHours(1)), next);
    }

    [Fact]
    public async Task PutSchedule_RotatingRule_RoundTrips()
    {
        await PutSchedule(new
        {
            default_mode = "ambient",
            rules = new[]
            {
                new { mode = "daily", days = new[] { "mon" }, start = "07:00", end = "09:00", rotate = new[] { "photo", "note" }, rotate_minutes = 20 },
            },
        });

        var rule = (await _client.GetFromJsonAsync<ModeSchedule>("/display/mode/schedule", HearthJson.SnakeCaseLower))!.Rules.Single();

        Assert.Equal(["photo", "note"], rule.Rotate);
        Assert.Equal(20, rule.RotateMinutes);
    }

    [Fact]
    public async Task PutSchedule_RotationWithoutInterval_Returns400()
    {
        var response = await PutSchedule(new
        {
            default_mode = "ambient",
            rules = new[] { new { mode = "daily", days = new[] { "mon" }, start = "07:00", end = "09:00", rotate = new[] { "photo" } } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Store_OverrideRoundTripsWithItsExpiry()
    {
        var store = new ModeStore(_db.Db);
        store.Migrate();

        store.SetOverride(new ModeOverride(FrameModes.Photo, Noon.AddHours(3), "abc"));

        Assert.Equal(new ModeOverride(FrameModes.Photo, Noon.AddHours(3), "abc"), store.GetOverride());
        store.ClearOverride();
        Assert.Null(store.GetOverride());
    }
}
