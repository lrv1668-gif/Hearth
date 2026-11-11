using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Calendar.Providers.Ics;
using Calendar.Records;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ServiceDefaults;

namespace Calendar.Tests;

public sealed class IcsEndpointTests : IDisposable
{
    private const string Link = "webcal://p01-caldav.icloud.com/published/2/secret-token";

    private readonly TempDatabase _db = new();
    private readonly FakeIcsSource _source = new(IcsSamples.Daily);
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IcsEndpointTests()
    {
        _factory = HearthApp.Create<Program>("calendar", _db, host => host.ConfigureTestServices(services =>
        {
            services.AddSingleton<IDataProtectionProvider>(TestDataProtection.Provider);
            services.AddSingleton<IIcsSource>(_source);
        }));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    private async Task<IcsFeedResponse> AddAsync(string? name = "Family", string url = Link)
    {
        var response = await _client.PostAsJsonAsync("/calendar/ics/feeds", new { name, url });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IcsFeedResponse>(HearthJson.SnakeCaseLower))!;
    }

    [Fact]
    public async Task GetFeeds_NothingSubscribed_ReturnsEmptyList()
    {
        var feeds = await _client.GetFromJsonAsync<List<IcsFeedResponse>>("/calendar/ics/feeds", HearthJson.SnakeCaseLower);

        Assert.Empty(feeds!);
    }

    [Fact]
    public async Task PostFeed_ValidLink_SubscribesAndNeverReturnsTheLink()
    {
        var added = await AddAsync();

        Assert.Equal("Family", added.Name);
        Assert.Equal("p01-caldav.icloud.com", added.Host);

        var listed = await _client.GetStringAsync("/calendar/ics/feeds");
        Assert.Contains(added.Id, listed);
        Assert.DoesNotContain("secret-token", listed);
    }

    [Fact]
    public async Task PostFeed_LinkThatIsNotACalendar_Returns400WithTheReason()
    {
        _source.Ics = "<html>Sign in</html>";

        var response = await _client.PostAsJsonAsync("/calendar/ics/feeds", new { name = "Family", url = Link });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("didn't return a calendar", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PostFeed_MissingLink_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/calendar/ics/feeds", new { name = "Family" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFeed_SubscribedThenUnknown_Returns204Then404()
    {
        var added = await AddAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/calendar/ics/feeds/{added.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/calendar/ics/feeds/{added.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetItems_NothingConnected_ReturnsEmptyList()
    {
        var items = await _client.GetFromJsonAsync<List<CalendarItem>>("/calendar/items", HearthJson.SnakeCaseLower);

        Assert.Empty(items!);
    }

    [Fact]
    public async Task GetItems_SubscribedCalendar_ReturnsItsEvents()
    {
        await AddAsync();

        var items = await _client.GetFromJsonAsync<List<CalendarItem>>("/calendar/items", HearthJson.SnakeCaseLower);

        Assert.NotEmpty(items!);
        Assert.All(items!, i =>
        {
            Assert.Equal("ics", i.Provider);
            Assert.Equal("Family", i.CalendarName);
            Assert.Equal("Lunch", i.Title);
        });
    }

    [Fact]
    public async Task PostItemsRefresh_NextRead_FetchesTheCalendarAgain()
    {
        await AddAsync();
        await _client.GetAsync("/calendar/items");
        var before = _source.Requests;

        var response = await _client.PostAsync("/calendar/items/refresh", null);
        await _client.GetAsync("/calendar/items");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(before + 1, _source.Requests);
    }

    [Fact]
    public async Task Health_WithTestDatabase_Responds()
    {
        var response = await _client.GetAsync("/calendar/health");

        Assert.True(response.IsSuccessStatusCode);
    }
}
