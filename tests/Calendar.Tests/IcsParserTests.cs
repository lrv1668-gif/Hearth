using Calendar.Providers.Ics;
using Calendar.Records;
using static Calendar.Tests.IcsSamples;

namespace Calendar.Tests;

public sealed class IcsParserTests
{
    private static readonly IcsFeed Feed = new("feed1", "Family", "https://example.com/family.ics");
    private static readonly DateTimeOffset From = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(61);
    private static readonly TimeZoneInfo Denver = TimeZoneInfo.FindSystemTimeZoneById("America/Denver");

    private static List<CalendarItem> Expand(string events, TimeZoneInfo? zone = null) =>
        IcsParser.Expand(IcsParser.Load(IcsSamples.Feed(events)), Feed, From, To, zone ?? Denver);

    [Fact]
    public void Expand_SingleTimedEvent_MapsItsFields()
    {
        var item = Assert.Single(Expand(Event("one", """
            DTSTART:20261112T180000Z
            DTEND:20261112T190000Z
            SUMMARY:Dentist
            DESCRIPTION:Bring the forms
            LOCATION:12 Main St
            URL:https://example.com/appointment
            """)));

        Assert.Equal("event", item.Kind);
        Assert.Equal("Dentist", item.Title);
        Assert.Equal("Bring the forms", item.Description);
        Assert.Equal("12 Main St", item.Location);
        Assert.Equal("Family", item.CalendarName);
        Assert.Equal("ics", item.Provider);
        Assert.False(item.IsAllDay);
        Assert.Null(item.IsCompleted);
        Assert.Null(item.TaskListId);
        Assert.Equal("https://example.com/appointment", item.HtmlLink);
        Assert.StartsWith("feed1:one:", item.Id);
    }

    [Fact]
    public void Expand_UtcEvent_IsWrittenInTheHomeZone()
    {
        var item = Assert.Single(Expand(Event("utc", """
            DTSTART:20261112T180000Z
            DURATION:PT30M
            SUMMARY:Call
            """)));

        Assert.Equal("2026-11-12T11:00:00.0000000-07:00", item.Start);
        Assert.Equal("2026-11-12T11:30:00.0000000-07:00", item.End);
    }

    [Fact]
    public void Expand_ZonedEvent_IsConvertedToTheHomeZone()
    {
        var item = Assert.Single(Expand(Event("zoned", """
            DTSTART;TZID=America/New_York:20261112T090000
            DTEND;TZID=America/New_York:20261112T100000
            SUMMARY:Standup
            """)));

        Assert.Equal("2026-11-12T07:00:00.0000000-07:00", item.Start);
        Assert.Equal("2026-11-12T08:00:00.0000000-07:00", item.End);
    }

    [Fact]
    public void Expand_FloatingEvent_KeepsItsWallClockInTheHomeZone()
    {
        var item = Assert.Single(Expand(Event("floating", """
            DTSTART:20261112T180000
            DTEND:20261112T190000
            SUMMARY:Dinner
            """)));

        Assert.Equal("2026-11-12T18:00:00.0000000-07:00", item.Start);
    }

    [Fact]
    public void Expand_NoHomeZone_KeepsEachEventsOwnOffset()
    {
        var items = IcsParser.Expand(IcsParser.Load(IcsSamples.Feed(Event("zoned", """
            DTSTART;TZID=America/New_York:20261112T090000
            DTEND;TZID=America/New_York:20261112T100000
            SUMMARY:Standup
            """))), Feed, From, To, homeZone: null);

        Assert.Equal("2026-11-12T09:00:00.0000000-05:00", Assert.Single(items).Start);
    }

    [Fact]
    public void Expand_AllDayEvent_UsesDatesWithAnExclusiveEnd()
    {
        var item = Assert.Single(Expand(Event("trip", """
            DTSTART;VALUE=DATE:20261120
            DTEND;VALUE=DATE:20261123
            SUMMARY:Trip
            """)));

        Assert.True(item.IsAllDay);
        Assert.Equal("2026-11-20", item.Start);
        Assert.Equal("2026-11-23", item.End);
    }

    [Fact]
    public void Expand_WeeklyRule_ExpandsHonouringExdateAndOverrides()
    {
        // Mondays 9am New York from 2 Nov, six times; the 23rd is cancelled and the 16th moved to 11am
        var items = Expand(Event("weekly", """
            DTSTART;TZID=America/New_York:20261102T090000
            DTEND;TZID=America/New_York:20261102T100000
            RRULE:FREQ=WEEKLY;COUNT=6
            EXDATE;TZID=America/New_York:20261123T090000
            SUMMARY:Standup
            """) + "\n" + Event("weekly", """
            RECURRENCE-ID;TZID=America/New_York:20261116T090000
            DTSTART;TZID=America/New_York:20261116T110000
            DTEND;TZID=America/New_York:20261116T120000
            SUMMARY:Standup (moved)
            """));

        Assert.Equal(
            [
                ("Standup", "2026-11-09T07:00:00.0000000-07:00"),
                ("Standup (moved)", "2026-11-16T09:00:00.0000000-07:00"),
                ("Standup", "2026-11-30T07:00:00.0000000-07:00"),
                ("Standup", "2026-12-07T07:00:00.0000000-07:00"),
            ],
            items.Select(i => (i.Title, i.Start!)).ToArray());
        Assert.Equal(items.Count, items.Select(i => i.Id).Distinct().Count());
    }

    [Fact]
    public void Expand_RuleCrossingDaylightSaving_KeepsTheLocalTime()
    {
        // 9am New York either side of the March 2027 change, seen from Phoenix, which has none
        var phoenix = TimeZoneInfo.FindSystemTimeZoneById("America/Phoenix");
        var items = IcsParser.Expand(IcsParser.Load(IcsSamples.Feed(Event("dst", """
            DTSTART;TZID=America/New_York:20270312T090000
            DTEND;TZID=America/New_York:20270312T093000
            RRULE:FREQ=DAILY;COUNT=4
            SUMMARY:Run
            """))), Feed, new DateTimeOffset(2027, 3, 10, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 3, 20, 0, 0, 0, TimeSpan.Zero), phoenix);

        Assert.Equal(
            ["2027-03-12T07:00", "2027-03-13T07:00", "2027-03-14T06:00", "2027-03-15T06:00"],
            items.Select(i => i.Start![..16]).ToArray());
    }

    [Fact]
    public void Expand_EventsOutsideTheWindow_AreLeftOut()
    {
        var items = Expand(
            Event("past", "DTSTART:20261101T180000Z\nDTEND:20261101T190000Z\nSUMMARY:Past") + "\n" +
            Event("past-day", "DTSTART;VALUE=DATE:20261108\nSUMMARY:Yesterday") + "\n" +
            Event("future", "DTSTART:20270601T180000Z\nDTEND:20270601T190000Z\nSUMMARY:Future"));

        Assert.Empty(items);
    }

    [Fact]
    public void Expand_EventAlreadyUnderWay_IsIncluded()
    {
        var items = Expand(
            Event("holiday", "DTSTART;VALUE=DATE:20261105\nDTEND;VALUE=DATE:20261112\nSUMMARY:Holiday") + "\n" +
            Event("today", "DTSTART;VALUE=DATE:20261109\nSUMMARY:Today"));

        Assert.Equal(["Holiday", "Today"], items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public void Expand_CancelledEvent_IsLeftOut() =>
        Assert.Empty(Expand(Event("off", """
            DTSTART:20261112T180000Z
            DTEND:20261112T190000Z
            STATUS:CANCELLED
            SUMMARY:Called off
            """)));

    [Fact]
    public void Expand_MissingTitleAndUnsafeLink_FallBack()
    {
        var item = Assert.Single(Expand(Event("bare", """
            DTSTART:20261112T180000Z
            URL:javascript:alert(1)
            """)));

        Assert.Equal("(No title)", item.Title);
        Assert.Null(item.Description);
        Assert.Null(item.HtmlLink);
    }

    [Fact]
    public void Expand_RunawayRule_StopsAtTheItemCap()
    {
        var items = Expand(Event("runaway", """
            DTSTART:20261109T000000Z
            DURATION:PT1M
            RRULE:FREQ=MINUTELY
            SUMMARY:Tick
            """));

        Assert.Equal(IcsParser.MaxItems, items.Count);
    }

    [Theory]
    [InlineData("<html><body>Not found</body></html>")]
    [InlineData("")]
    [InlineData("just some text")]
    public void Load_NotACalendar_Throws(string text) =>
        Assert.Throws<IcsFeedException>(() => IcsParser.Load(text));

    [Fact]
    public void NameOf_NamedAndUnnamedCalendars_ReturnsTheNameOrNull()
    {
        Assert.Equal("Family", IcsParser.NameOf(IcsParser.Load(IcsSamples.Feed("", "Family"))));
        Assert.Null(IcsParser.NameOf(IcsParser.Load(IcsSamples.Feed("", null))));
    }
}
