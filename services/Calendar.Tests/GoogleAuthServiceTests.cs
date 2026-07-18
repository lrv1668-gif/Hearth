using Calendar.Providers.Google;
using Calendar.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Calendar.Tests;

public sealed class GoogleAuthServiceTests
{
    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";
    private const string RedirectUri = "https://example.com/calendar/google/callback";

    private static GoogleAuthService MakeService(TempDatabase db, FakeTimeProvider time)
    {
        var store = new CalendarStore(db.Db);
        store.Migrate();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GOOGLE_CLIENT_ID"] = ClientId,
                ["GOOGLE_CLIENT_SECRET"] = ClientSecret,
                ["GOOGLE_REDIRECT_URI"] = RedirectUri,
            })
            .Build();

        return new GoogleAuthService(store, config, time);
    }

    [Fact]
    public void GenerateAuthUrl_ReturnsUrlContainingStateAndPromptConsent()
    {
        using var db = new TempDatabase();
        var authService = MakeService(db, new FakeTimeProvider(DateTimeOffset.UtcNow));

        var url = authService.GenerateAuthUrl("state-123");

        Assert.Contains("state=state-123", url);
        Assert.Contains("prompt=consent", url);
    }

    [Fact]
    public void GenerateAuthUrl_IncludesConfiguredClientIdAndRedirectUri()
    {
        using var db = new TempDatabase();
        var authService = MakeService(db, new FakeTimeProvider(DateTimeOffset.UtcNow));

        var url = authService.GenerateAuthUrl("state-123");

        Assert.Contains($"client_id={ClientId}", url);
        Assert.Contains(Uri.EscapeDataString(RedirectUri), url);
    }

    [Fact]
    public void ValidateAndConsumeState_UnknownState_ReturnsFalse()
    {
        using var db = new TempDatabase();
        var authService = MakeService(db, new FakeTimeProvider(DateTimeOffset.UtcNow));

        Assert.False(authService.ValidateAndConsumeState("never-issued"));
    }

    [Fact]
    public void ValidateAndConsumeState_FreshState_ReturnsTrue()
    {
        using var db = new TempDatabase();
        var authService = MakeService(db, new FakeTimeProvider(DateTimeOffset.UtcNow));
        authService.GenerateAuthUrl("state-123");

        Assert.True(authService.ValidateAndConsumeState("state-123"));
    }

    [Fact]
    public void ValidateAndConsumeState_SameStateTwice_SecondCallReturnsFalse()
    {
        using var db = new TempDatabase();
        var authService = MakeService(db, new FakeTimeProvider(DateTimeOffset.UtcNow));
        authService.GenerateAuthUrl("state-123");

        Assert.True(authService.ValidateAndConsumeState("state-123"));
        Assert.False(authService.ValidateAndConsumeState("state-123"));
    }

    [Fact]
    public void ValidateAndConsumeState_ExactlyAtTenMinuteBoundary_ReturnsTrue()
    {
        using var db = new TempDatabase();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var authService = MakeService(db, time);
        authService.GenerateAuthUrl("state-123");

        time.Advance(TimeSpan.FromMinutes(10));

        Assert.True(authService.ValidateAndConsumeState("state-123"));
    }

    [Fact]
    public void ValidateAndConsumeState_OneTickPastTenMinutes_ReturnsFalse()
    {
        using var db = new TempDatabase();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var authService = MakeService(db, time);
        authService.GenerateAuthUrl("state-123");

        time.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromTicks(1));

        Assert.False(authService.ValidateAndConsumeState("state-123"));
    }

    [Fact]
    public void GenerateAuthUrl_SameStateCalledTwice_OverwritesExpiry()
    {
        using var db = new TempDatabase();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var authService = MakeService(db, time);

        authService.GenerateAuthUrl("state-123"); // expires at start + 10min

        time.Advance(TimeSpan.FromMinutes(6));
        authService.GenerateAuthUrl("state-123"); // refreshed: expires at start + 16min

        time.Advance(TimeSpan.FromMinutes(5)); // now start + 11min: past the original expiry, not the refreshed one

        Assert.True(authService.ValidateAndConsumeState("state-123"));
    }

    [Fact]
    public void ValidateAndConsumeState_EmptyStringState_ReturnsFalse()
    {
        using var db = new TempDatabase();
        var authService = MakeService(db, new FakeTimeProvider(DateTimeOffset.UtcNow));

        Assert.False(authService.ValidateAndConsumeState(""));
    }
}
