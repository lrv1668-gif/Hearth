using Calendar.Providers.Google;
using Google.Apis.Calendar.v3.Data;
using Xunit;
using GTask = Google.Apis.Tasks.v1.Data.Task;

namespace Calendar.Tests;

public sealed class GoogleCalendarProviderMappingTests
{
    [Fact]
    public void MapCalendarEvents_TimedEvent_MapsFieldsAndIsAllDayFalse()
    {
        var start = new DateTimeOffset(2026, 7, 20, 14, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 7, 20, 15, 0, 0, TimeSpan.Zero);
        var events = new List<Event>
        {
            new()
            {
                Id = "evt-1",
                Summary = "Team sync",
                Description = "Weekly sync",
                Location = "Room 4",
                HtmlLink = "https://calendar.google.com/event?eid=abc",
                Start = new EventDateTime { DateTimeDateTimeOffset = start },
                End = new EventDateTime { DateTimeDateTimeOffset = end },
            },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        var item = Assert.Single(result);
        Assert.Equal("event", item.Kind);
        Assert.Equal("evt-1", item.Id);
        Assert.Equal("Team sync", item.Title);
        Assert.Equal("Weekly sync", item.Description);
        Assert.Equal("Room 4", item.Location);
        Assert.False(item.IsAllDay);
        Assert.Equal(start.ToString("o"), item.Start);
        Assert.Equal(end.ToString("o"), item.End);
        Assert.Equal("google", item.Provider);
        Assert.Null(item.IsCompleted);
        Assert.Null(item.TaskListId);
        Assert.Equal("https://calendar.google.com/event?eid=abc", item.HtmlLink);
    }

    [Fact]
    public void MapCalendarEvents_AllDayEvent_UsesDateFieldsAndIsAllDayTrue()
    {
        var events = new List<Event>
        {
            new()
            {
                Id = "evt-1",
                Summary = "Holiday",
                Start = new EventDateTime { Date = "2026-07-20" },
                End = new EventDateTime { Date = "2026-07-21" },
            },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        var item = Assert.Single(result);
        Assert.True(item.IsAllDay);
        Assert.Equal("2026-07-20", item.Start);
        Assert.Equal("2026-07-21", item.End);
    }

    [Fact]
    public void MapCalendarEvents_AllDayEventMissingEnd_FallsBackToStartDate()
    {
        var events = new List<Event>
        {
            new()
            {
                Id = "evt-1",
                Start = new EventDateTime { Date = "2026-07-20" },
                End = null,
            },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        Assert.Equal("2026-07-20", Assert.Single(result).End);
    }

    [Fact]
    public void MapCalendarEvents_TimedEventMissingEnd_FallsBackToStart()
    {
        var start = new DateTimeOffset(2026, 7, 20, 14, 0, 0, TimeSpan.Zero);
        var events = new List<Event>
        {
            new()
            {
                Id = "evt-1",
                Start = new EventDateTime { DateTimeDateTimeOffset = start },
                End = null,
            },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        var item = Assert.Single(result);
        Assert.Equal(start.ToString("o"), item.Start);
        Assert.Equal(item.Start, item.End);
    }

    [Fact]
    public void MapCalendarEvents_NullId_GeneratesNonEmptyId()
    {
        var events = new List<Event>
        {
            new() { Id = null, Start = new EventDateTime { Date = "2026-07-20" } },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        var item = Assert.Single(result);
        Assert.False(string.IsNullOrEmpty(item.Id));
        Assert.True(Guid.TryParse(item.Id, out _));
    }

    [Fact]
    public void MapCalendarEvents_NullSummary_DefaultsToNoTitle()
    {
        var events = new List<Event>
        {
            new() { Id = "evt-1", Summary = null, Start = new EventDateTime { Date = "2026-07-20" } },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        Assert.Equal("(No title)", Assert.Single(result).Title);
    }

    [Fact]
    public void MapCalendarEvents_NullStart_EventIsSkipped()
    {
        var events = new List<Event>
        {
            new() { Id = "evt-1", Start = null },
            new() { Id = "evt-2", Start = new EventDateTime { Date = "2026-07-20" } },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        var item = Assert.Single(result);
        Assert.Equal("evt-2", item.Id);
    }

    [Fact]
    public void MapCalendarEvents_PreservesDescriptionLocationAndHtmlLink()
    {
        var events = new List<Event>
        {
            new()
            {
                Id = "evt-1",
                Description = null,
                Location = null,
                HtmlLink = null,
                Start = new EventDateTime { Date = "2026-07-20" },
            },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        var item = Assert.Single(result);
        Assert.Null(item.Description);
        Assert.Null(item.Location);
        Assert.Null(item.HtmlLink);
        Assert.Null(item.CalendarName);
    }

    [Fact]
    public void MapCalendarEvents_EmptyList_ReturnsEmptyList()
    {
        var result = GoogleCalendarProvider.MapCalendarEvents(new List<Event>());

        Assert.Empty(result);
    }

    [Fact]
    public void MapCalendarEvents_MultipleEvents_PreservesCountAndOrder()
    {
        var events = new List<Event>
        {
            new() { Id = "evt-1", Start = new EventDateTime { Date = "2026-07-20" } },
            new() { Id = "evt-2", Start = new EventDateTime { Date = "2026-07-21" } },
            new() { Id = "evt-3", Start = new EventDateTime { Date = "2026-07-22" } },
        };

        var result = GoogleCalendarProvider.MapCalendarEvents(events);

        Assert.Equal(["evt-1", "evt-2", "evt-3"], result.Select(i => i.Id));
    }

    [Fact]
    public void MapGoogleTasks_WithDueDate_StartIsFirstTenChars()
    {
        var tasks = new List<GTask>
        {
            new() { Id = "task-1", Title = "Buy milk", Due = "2026-07-20T00:00:00.000Z" },
        };

        var result = GoogleCalendarProvider.MapGoogleTasks(tasks, "@default");

        Assert.Equal("2026-07-20", Assert.Single(result).Start);
    }

    [Fact]
    public void MapGoogleTasks_NullDue_StartIsNullTaskStillIncluded()
    {
        var tasks = new List<GTask> { new() { Id = "task-1", Title = "Someday", Due = null } };

        var result = GoogleCalendarProvider.MapGoogleTasks(tasks, "@default");

        var item = Assert.Single(result);
        Assert.Null(item.Start);
    }

    [Fact]
    public void MapGoogleTasks_NullId_TaskIsSkipped()
    {
        var tasks = new List<GTask>
        {
            new() { Id = null, Title = "Orphan" },
            new() { Id = "task-2", Title = "Kept" },
        };

        var result = GoogleCalendarProvider.MapGoogleTasks(tasks, "@default");

        var item = Assert.Single(result);
        Assert.Equal("task-2", item.Id);
    }

    [Fact]
    public void MapGoogleTasks_NullTitle_DefaultsToNoTitle()
    {
        var tasks = new List<GTask> { new() { Id = "task-1", Title = null } };

        var result = GoogleCalendarProvider.MapGoogleTasks(tasks, "@default");

        Assert.Equal("(No title)", Assert.Single(result).Title);
    }

    [Theory]
    [InlineData("completed", true)]
    [InlineData("needsAction", false)]
    [InlineData("some-unexpected-status", false)]
    public void MapGoogleTasks_StatusValue_MapsToIsCompleted(string status, bool expectedCompleted)
    {
        var tasks = new List<GTask> { new() { Id = "task-1", Status = status } };

        var result = GoogleCalendarProvider.MapGoogleTasks(tasks, "@default");

        Assert.Equal(expectedCompleted, Assert.Single(result).IsCompleted);
    }

    [Fact]
    public void MapGoogleTasks_AlwaysSetsIsAllDayTrueTaskListIdAndStaticHtmlLink()
    {
        var tasks = new List<GTask> { new() { Id = "task-1" } };

        var result = GoogleCalendarProvider.MapGoogleTasks(tasks, "my-list");

        var item = Assert.Single(result);
        Assert.True(item.IsAllDay);
        Assert.Equal("my-list", item.TaskListId);
        Assert.Equal("https://tasks.google.com/", item.HtmlLink);
        Assert.Null(item.End);
        Assert.Null(item.Location);
        Assert.Equal("task", item.Kind);
    }

    [Fact]
    public void MapGoogleTasks_NotesMappedToDescription()
    {
        var tasks = new List<GTask> { new() { Id = "task-1", Notes = "Get 2%" } };

        var result = GoogleCalendarProvider.MapGoogleTasks(tasks, "@default");

        Assert.Equal("Get 2%", Assert.Single(result).Description);
    }

    [Fact]
    public void MapGoogleTasks_DueShorterThanTenChars_ThrowsArgumentOutOfRangeException()
    {
        // Documents a real gap found while writing these tests: MapGoogleTasks does
        // `t.Due[..10]` with no length guard. A malformed/short Due string from Google
        // (or corrupted cache data) crashes rather than degrading gracefully.
        var tasks = new List<GTask> { new() { Id = "task-1", Due = "2026" } };

        Assert.Throws<ArgumentOutOfRangeException>(() => GoogleCalendarProvider.MapGoogleTasks(tasks, "@default"));
    }
}
