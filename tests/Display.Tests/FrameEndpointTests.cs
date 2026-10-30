using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Display.Records;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ServiceDefaults;

namespace Display.Tests;

/// <summary>
/// /display/frame end to end: the real <see cref="FrameRenderService"/> rendering through
/// a fake renderer whose "PNG" is just the handler's response text.
/// </summary>
public sealed class FrameEndpointTests : IDisposable
{
    private static readonly DateTimeOffset Noon = TestData.Noon;

    private readonly TempDatabase _db = new();
    private readonly FakeTimeProvider _clock = new(Noon);
    private readonly TempDirectory _dir = new();
    private readonly FakeHttpMessageHandler _renderer = new("frame-a");
    // The Photos service, set up only by tests that turn on _withPhotos (PHOTOS_URL)
    private readonly FakeHttpMessageHandler _photos = new(Unsplash("s1"));
    private bool _withPhotos;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    /// <summary>Starts the app (and its render loop) on first use, so a test can set up the renderer first.</summary>
    private HttpClient Client
    {
        get
        {
            if (_client is not null) return _client;
            _factory = HearthApp.Create<Program>("display", _db, host =>
            {
                host.UseSetting("TZ", "Etc/UTC");
                host.UseSetting("RENDERER_URL", "http://renderer");
                if (_withPhotos) host.UseSetting("PHOTOS_URL", "http://photos");
                host.ConfigureTestServices(services =>
                {
                    services.AddSingleton<TimeProvider>(_clock);
                    services.AddSingleton(_ => new FrameCache(_dir.Path, NullLogger<FrameCache>.Instance));
                    services.AddHttpClient<RendererClient>().ConfigurePrimaryHttpMessageHandler(() => _renderer);
                    services.AddHttpClient<PhotosClient>().ConfigurePrimaryHttpMessageHandler(() => _photos);
                });
            });
            return _client = _factory.CreateClient();
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        _db.Dispose();
        _dir.Dispose();
    }

    private string RenderQuery => Uri.UnescapeDataString(_renderer.LastRequestUri!.Query);

    private Task<HttpResponseMessage> Frame(string? ifNoneMatch = null, int? wait = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/display/frame" + (wait is null ? "" : $"?wait={wait}"));
        if (ifNoneMatch is not null) request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(ifNoneMatch));
        return Client.SendAsync(request);
    }

    private async Task<FrameStatus> Status() =>
        (await Client.GetFromJsonAsync<FrameStatus>("/display/frame/status", HearthJson.SnakeCaseLower))!;

    /// <summary>Waits (in real time) for the background loop to reach the renderer again.</summary>
    private async Task RendersSince(int count)
    {
        for (var i = 0; i < 250 && _renderer.RequestCount <= count; i++) await Task.Delay(20);
        Assert.True(_renderer.RequestCount > count, "expected another render");
    }

    [Fact]
    public async Task GetFrame_FirstRender_ReturnsThePngWithAnETag()
    {
        var response = await Frame();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("frame-a", await response.Content.ReadAsStringAsync());
        Assert.NotNull(response.Headers.ETag);
        Assert.True(response.Headers.CacheControl?.NoCache);
        Assert.NotNull(response.Headers.RetryAfter?.Delta);
        Assert.Contains("path=/frame?mode=ambient&", RenderQuery);
        Assert.Contains("dither=errorDiffusion", RenderQuery);
        Assert.DoesNotContain("rotate=", RenderQuery);
    }

    [Fact]
    public async Task GetFrame_MatchingIfNoneMatch_Returns304()
    {
        var etag = (await Frame()).Headers.ETag!.Tag;

        var response = await Frame(etag);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task GetFrame_RendererFailing_Returns503()
    {
        _renderer.Status = HttpStatusCode.ServiceUnavailable;

        var response = await Frame();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull((await Status()).LastError);
    }

    [Fact]
    public async Task PostModeNext_ThenGetFrame_ServesTheNextModeOrderedDithered()
    {
        var first = await Frame();
        _renderer.ResponseJson = "frame-b";

        await Client.PostAsync("/display/mode/next", null);
        var response = await Frame();

        Assert.Equal("frame-b", await response.Content.ReadAsStringAsync());
        Assert.NotEqual(first.Headers.ETag, response.Headers.ETag);
        Assert.Contains("path=/frame?mode=daily&", RenderQuery);
        Assert.Contains("dither=ordered", RenderQuery);
        Assert.Equal("daily", (await Status()).Mode);
    }

    [Fact]
    public async Task GetStatus_AfterARender_SerializesTheETagAsEtag()
    {
        var etag = (await Frame()).Headers.ETag!.Tag;

        var json = await Client.GetStringAsync("/display/frame/status");

        Assert.Contains("\"etag\":", json);
        Assert.DoesNotContain("e_tag", json);
        Assert.Equal(etag, (await Status()).ETag);
    }

    [Fact]
    public async Task GetStatus_NoPollYet_HasNoLastPoll()
    {
        Assert.Null((await Status()).LastPoll);
    }

    [Fact]
    public async Task GetStatus_AfterAPoll_ReportsTheClientAndItsETag()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/display/frame");
        request.Headers.UserAgent.ParseAdd("inkypull/0.3");
        request.Headers.Add("X-Forwarded-For", "192.168.1.42, 10.0.0.1");
        var etag = (await Client.SendAsync(request)).Headers.ETag!.Tag;

        var poll = (await Status()).LastPoll!;

        Assert.Equal(Noon, poll.At);
        Assert.Equal("192.168.1.42", poll.Address);
        Assert.Equal("inkypull/0.3", poll.UserAgent);
        Assert.Equal(etag, poll.ETag);
    }

    [Fact]
    public async Task GetStatus_PollGot304_StillRecordsIt()
    {
        var etag = (await Frame()).Headers.ETag!.Tag;
        _clock.Advance(TimeSpan.FromMinutes(5));

        await Frame(etag);

        var poll = (await Status()).LastPoll!;
        Assert.Equal(Noon.AddMinutes(5), poll.At);
        Assert.Equal(etag, poll.ETag);
    }

    [Fact]
    public async Task PostRefresh_SamePixels_KeepsTheETag()
    {
        var first = await Frame();
        var renders = _renderer.RequestCount;

        Assert.Equal(HttpStatusCode.Accepted, (await Client.PostAsync("/display/frame/refresh", null)).StatusCode);
        var response = await Frame();

        Assert.True(_renderer.RequestCount > renders);
        Assert.Equal(first.Headers.ETag, response.Headers.ETag);
    }

    [Fact]
    public async Task PostRefresh_RendererFailing_KeepsServingTheLastFrame()
    {
        await Frame();
        _renderer.Status = HttpStatusCode.BadGateway;

        await Client.PostAsync("/display/frame/refresh", null);
        var response = await Frame();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("frame-a", await response.Content.ReadAsStringAsync());
        Assert.NotNull((await Status()).LastError);
    }

    [Fact]
    public async Task PutSettings_Portrait_RendersUprightThenTurnsItTheChosenWay()
    {
        await Frame();

        await Client.PutAsJsonAsync("/display/settings", new
        {
            theme = "almanac", font = "inter", orientation = "portrait", left = Array.Empty<string>(),
            right = Array.Empty<string>(), split = 50, portrait_rotation = 270,
        });
        await Frame();

        Assert.Contains("width=1200&height=1600", RenderQuery);
        Assert.Contains("rotate=270", RenderQuery);
    }

    [Fact]
    public async Task FlatMode_AfterTheFlatInterval_RendersAgain()
    {
        await Client.PostAsync("/display/mode/next", null); // ambient → daily
        await Frame();
        var renders = _renderer.RequestCount;

        _clock.Advance(FrameRenderService.FlatInterval);

        await RendersSince(renders);
    }

    [Fact]
    public async Task GetFrame_WaitWithAStaleETag_ReturnsTheFrameAtOnce()
    {
        await Frame();

        var response = await Frame("\"stale\"", wait: 600);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("frame-a", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetFrame_WaitWithTheCurrentETag_ReturnsTheNewFrameWhenItChanges()
    {
        var etag = (await Frame()).Headers.ETag!.Tag;

        var poll = Frame(etag, wait: 600);
        await Task.Delay(100);
        Assert.False(poll.IsCompleted);
        _renderer.ResponseJson = "frame-b";
        await Client.PostAsync("/display/frame/refresh", null);
        var response = await poll.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("frame-b", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetFrame_WaitWithTheCurrentETag_Returns304WhenTheWaitRunsOut()
    {
        var etag = (await Frame()).Headers.ETag!.Tag;

        var poll = Frame(etag, wait: 60);
        // Advance fake time until the request is waiting on it and times out
        for (var i = 0; i < 100 && !poll.IsCompleted; i++)
        {
            await Task.Delay(20);
            _clock.Advance(TimeSpan.FromSeconds(30));
        }
        var response = await poll.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter?.Delta);
    }

    [Theory]
    [InlineData(600, 630)] // the next render in 10 min, plus the grace for it to finish
    [InlineData(-5, 30)]   // overdue (or rendering now): just the grace
    public void NextCheck_IsTheNextRenderPlusAGrace(int inSeconds, long expected)
    {
        Assert.Equal(expected, Extensions.WebApplicationExtensions.NextCheck(Noon.AddSeconds(inSeconds), Noon));
    }

    [Fact]
    public void NextCheck_NothingPlanned_IsNull()
    {
        Assert.Null(Extensions.WebApplicationExtensions.NextCheck(null, Noon));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?mode=night")]
    public async Task Photo_NotAPhotoMode_400(string query)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/display/photo" + query)).StatusCode);
    }

    [Fact]
    public async Task Photo_NoPhotosUrl_503()
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Client.GetAsync("/display/photo?mode=ambient")).StatusCode);
    }

    private static string Unsplash(string id) =>
        $$"""{"id":"{{id}}","url":"https://images.unsplash.com/{{id}}","thumb_url":null,"description":null,"photographer_name":"Ana","unsplash_link":"https://unsplash.com/{{id}}","source":"unsplash"}""";

    /// <summary>The photo the last render's /frame path carried (its URL is escaped twice: path, then photo).</summary>
    private string RenderedPhoto => Uri.UnescapeDataString(RenderQuery);

    private int PhotoPicks => _photos.RequestUris.Count(u => u.AbsolutePath == "/photos/random");

    /// <summary>Daily on the panel, with the Photo widget in a slot unless <paramref name="photo"/> is false.</summary>
    private async Task DailyWithPhoto(bool photo = true)
    {
        _withPhotos = true;
        await Client.PutAsJsonAsync("/display/settings", new
        {
            theme = "almanac", font = "inter", orientation = "landscape", left = Array.Empty<string>(),
            right = Array.Empty<string>(), split = 50, template = "quad",
            slots = photo ? new[] { "photo", "today" } : new[] { "today" },
        });
        await Client.PutAsJsonAsync("/display/mode/override", new { mode = "daily" });
    }

    [Fact]
    public async Task Photo_Daily_ReturnsTodaysHeldPhoto()
    {
        await DailyWithPhoto();
        var first = await Client.GetFromJsonAsync<FramePhoto>("/display/photo?mode=daily", HearthJson.SnakeCaseLower);
        _photos.ResponseJson = Unsplash("s2");

        var again = await Client.GetFromJsonAsync<FramePhoto>("/display/photo?mode=daily", HearthJson.SnakeCaseLower);

        Assert.Equal("s1", first?.Id);
        Assert.Equal("s1", again?.Id);
    }

    [Fact]
    public async Task DailyRender_IntervalRender_KeepsThePhoto()
    {
        await DailyWithPhoto();
        await Frame();
        _photos.ResponseJson = Unsplash("s2");
        var renders = _renderer.RequestCount;

        _clock.Advance(FrameRenderService.FlatInterval);
        await RendersSince(renders);

        Assert.Contains("\"id\":\"s1\"", RenderedPhoto);
    }

    [Fact]
    public async Task DailyRender_Refresh_MovesThePhotoOn()
    {
        await DailyWithPhoto();
        await Frame();
        _photos.ResponseJson = Unsplash("s2");
        var renders = _renderer.RequestCount;

        await Client.PostAsync("/display/frame/refresh", null);
        await RendersSince(renders);

        Assert.Contains("\"id\":\"s2\"", RenderedPhoto);
    }

    [Fact]
    public async Task DailyRender_NewDay_MovesThePhotoOn()
    {
        await DailyWithPhoto();
        await Frame();
        _photos.ResponseJson = Unsplash("s2");
        var renders = _renderer.RequestCount;

        _clock.Advance(TimeSpan.FromHours(12)); // midnight in the test's UTC home
        await RendersSince(renders);

        Assert.Contains("\"id\":\"s2\"", RenderedPhoto);
    }

    [Fact]
    public async Task DailyRender_NoPhotoWidget_NeverAsksPhotos()
    {
        await DailyWithPhoto(photo: false);
        await Frame();

        Assert.Equal(0, PhotoPicks);
        Assert.DoesNotContain("photo=", RenderQuery);
    }
}
