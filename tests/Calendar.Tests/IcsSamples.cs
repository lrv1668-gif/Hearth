using Calendar.Providers.Ics;

namespace Calendar.Tests;

/// <summary>iCalendar text and a stand-in for the network, shared by the ICS tests.</summary>
internal static class IcsSamples
{
    public static string Feed(string events, string? name = "Family") => $"""
        BEGIN:VCALENDAR
        VERSION:2.0
        PRODID:-//Hearth//Tests//EN
        {(name is null ? "X-COMMENT:unnamed" : $"X-WR-CALNAME:{name}")}
        {events.Trim()}
        END:VCALENDAR
        """;

    public static string Event(string uid, string body) => $"""
        BEGIN:VEVENT
        UID:{uid}
        {body.Trim()}
        END:VEVENT
        """;

    /// <summary>Every day at noon UTC since 2020, so it always has occurrences in the items window.</summary>
    public static readonly string Daily = Feed(Event("daily", """
        DTSTART:20200101T120000Z
        DTEND:20200101T123000Z
        RRULE:FREQ=DAILY
        SUMMARY:Lunch
        """));
}

/// <summary>An <see cref="IIcsSource"/> answering every link with <see cref="Ics"/>, or failing.</summary>
internal sealed class FakeIcsSource(string ics) : IIcsSource
{
    public string Ics { get; set; } = ics;
    public bool Failing { get; set; }
    public int Requests { get; private set; }
    public string? LastUrl { get; private set; }

    public Task<string> FetchAsync(string url, CancellationToken ct = default)
    {
        Requests++;
        LastUrl = url;
        return Failing
            ? Task.FromException<string>(new HttpRequestException("Response status code does not indicate success: 404"))
            : Task.FromResult(Ics);
    }
}
