using Calendar.Providers.Ics;
using Hearth.TestUtilities;
using Microsoft.Extensions.Time.Testing;

namespace Calendar.Tests;

public sealed class IcsFeedStoreTests : IDisposable
{
    private const string Link = "https://p01-caldav.icloud.com/published/2/secret-token";

    private readonly TempDatabase _db = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly IcsFeedStore _store;

    public IcsFeedStoreTests()
    {
        _store = new IcsFeedStore(_db.Db, TestDataProtection.Provider, _clock);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Any_NoFeeds_IsFalse() => Assert.False(_store.Any());

    [Fact]
    public void Add_NewFeed_IsListedWithItsLink()
    {
        var added = _store.Add("Family", Link);

        Assert.True(_store.Any());
        var listed = Assert.Single(_store.List());
        Assert.Equal(added, listed);
        Assert.Equal("Family", listed.Name);
        Assert.Equal(Link, listed.Url);
    }

    [Fact]
    public void Add_SeveralFeeds_ListsThemOldestFirst()
    {
        _store.Add("Family", Link);
        _clock.Advance(TimeSpan.FromMinutes(1));
        _store.Add("School", "https://example.com/school.ics");

        Assert.Equal(["Family", "School"], _store.List().Select(f => f.Name).ToArray());
    }

    [Fact]
    public void Add_Link_IsEncryptedAtRest()
    {
        _store.Add("Family", Link);

        var stored = _db.Db.QueryOne("SELECT url FROM calendar_ics_feeds", r => r.GetString(0));
        Assert.NotNull(stored);
        Assert.DoesNotContain("secret-token", stored);
        Assert.DoesNotContain("icloud", stored);
    }

    [Fact]
    public void List_RowThatNoLongerDecrypts_IsLeftOut()
    {
        _store.Add("Family", Link);
        _db.Db.NonQuery("UPDATE calendar_ics_feeds SET url = 'not-a-protected-value'");

        Assert.Empty(_store.List());
    }

    [Fact]
    public void Remove_ExistingFeed_DeletesOnlyThatOne()
    {
        var family = _store.Add("Family", Link);
        _store.Add("School", "https://example.com/school.ics");

        Assert.True(_store.Remove(family.Id));
        Assert.Equal("School", Assert.Single(_store.List()).Name);
    }

    [Fact]
    public void Remove_UnknownFeed_ReturnsFalse() => Assert.False(_store.Remove("nope"));

    [Fact]
    public void Migrate_RunTwice_KeepsTheFeeds()
    {
        _store.Add("Family", Link);

        _store.Migrate();

        Assert.Single(_store.List());
    }
}
