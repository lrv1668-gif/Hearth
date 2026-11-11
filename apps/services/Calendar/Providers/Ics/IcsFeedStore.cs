using System.Data.Common;
using System.Security.Cryptography;
using Calendar.Records;
using Data.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Calendar.Providers.Ics;

/// <summary>
/// The calendars subscribed to by link. The links are encrypted at rest like the OAuth
/// tokens, because a public share link is all it takes to read the calendar.
/// </summary>
public sealed class IcsFeedStore(
    [FromKeyedServices("calendar")] IDatabase db, IDataProtectionProvider dpProvider, TimeProvider time)
{
    public const int MaxFeeds = 20;

    private readonly IDataProtector _protector = dpProvider.CreateProtector("Calendar.IcsFeeds.v1");

    public void Migrate() =>
        db.NonQuery("""
            CREATE TABLE IF NOT EXISTS calendar_ics_feeds (
                id         TEXT PRIMARY KEY,
                name       TEXT NOT NULL,
                url        TEXT NOT NULL,
                created_at TEXT NOT NULL
            )
            """);

    public bool Any() =>
        db.QueryOne("SELECT 1 FROM calendar_ics_feeds LIMIT 1", _ => true) is true;

    /// <summary>Oldest first. A row whose link no longer decrypts (lost keys) is left out.</summary>
    public List<IcsFeed> List() =>
        db.Query("SELECT id, name, url FROM calendar_ics_feeds ORDER BY created_at, id", MapFeed)
            .OfType<IcsFeed>()
            .ToList();

    public IcsFeed Add(string name, string url)
    {
        var feed = new IcsFeed(Guid.NewGuid().ToString("N"), name, url);
        db.NonQuery("""
            INSERT INTO calendar_ics_feeds (id, name, url, created_at)
            VALUES ($id, $name, $url, $created_at)
            """, cmd =>
        {
            cmd.AddParam("$id", feed.Id);
            cmd.AddParam("$name", feed.Name);
            cmd.AddParam("$url", _protector.Protect(feed.Url));
            cmd.AddParam("$created_at", time.GetUtcNow().ToString("o"));
        });
        return feed;
    }

    /// <summary>False when there was no such feed.</summary>
    public bool Remove(string id)
    {
        var existed = db.QueryOne(
            "SELECT 1 FROM calendar_ics_feeds WHERE id = $id", _ => true, c => c.AddParam("$id", id)) is true;
        db.NonQuery("DELETE FROM calendar_ics_feeds WHERE id = $id", c => c.AddParam("$id", id));
        return existed;
    }

    private IcsFeed? MapFeed(DbDataReader r)
    {
        try
        {
            return new IcsFeed(
                r.Field<string>("id")!,
                r.Field<string>("name")!,
                _protector.Unprotect(r.Field<string>("url")!));
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
