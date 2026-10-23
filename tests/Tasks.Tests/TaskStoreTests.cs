using System.Linq;
using Hearth.TestUtilities;
using Tasks;
using Xunit;

namespace Tasks.Tests;

public sealed class TaskStoreTests
{
    private static TaskStore Migrated(TempDatabase tmp)
    {
        var store = new TaskStore(tmp.Db);
        store.Migrate();
        return store;
    }

    [Fact]
    public void Create_NonRecurring_AppearsInListWithoutSeries()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        var created = store.Create("Buy milk", null, null, null, null, null, null, null, null);

        Assert.Null(created.SeriesId);
        var listed = Assert.Single(store.List());
        Assert.Equal("Buy milk", listed.Title);
        Assert.False(listed.Done);
    }

    [Fact]
    public void Create_Recurring_GeneratesInstancesSharingSeriesId()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(4);

        var created = store.Create("Standup", start, null, null, null, "day", 1, null, end);

        Assert.Equal(created.Id, created.SeriesId);
        var instances = store.List().Where(t => t.SeriesId == created.SeriesId).ToList();
        Assert.True(instances.Count >= 2, $"expected a generated series, got {instances.Count}");
        Assert.All(instances, t => Assert.Equal(created.SeriesId, t.SeriesId));
    }

    [Fact]
    public void Update_MarksDone_KeepsFieldsSentBack()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var due = DateTime.UtcNow.Date.AddDays(2);
        var created = store.Create("Water plants", due, "09:00", "Ferns too", "Sam", null, null, null, null);

        var updated = store.Update(created.Id, done: true, title: "Water plants", dueDate: due,
            dueTime: "09:00", description: "Ferns too", assignee: "Sam");

        Assert.NotNull(updated);
        Assert.True(updated!.Done);
        Assert.Equal("Water plants", updated.Title);
        Assert.Equal(due, updated.DueDate);
        Assert.Equal("Ferns too", updated.Description);
    }

    [Fact]
    public void Update_NullFields_ClearsThem()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var created = store.Create("Water plants", DateTime.UtcNow.Date.AddDays(2), "09:00",
            "Ferns too", "Sam", null, null, null, null);

        var updated = store.Update(created.Id, done: false, title: "Water plants", dueDate: null,
            dueTime: null, description: null, assignee: null);

        Assert.NotNull(updated);
        Assert.Null(updated!.DueDate);
        Assert.Null(updated.DueTime);
        Assert.Null(updated.Description);
        Assert.Null(updated.Assignee);
        Assert.Null(Assert.Single(store.List()).DueDate);
    }

    [Fact]
    public void Create_RecurringCountdownWithEndDate_InstancesKeepBoth()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(4);

        var created = store.Create("Trip", start, null, null, null, "day", 1, null, end, isCountdown: true);

        var instances = store.List().Where(t => t.SeriesId == created.SeriesId).ToList();
        Assert.Equal(5, instances.Count);
        Assert.All(instances, t =>
        {
            Assert.True(t.IsCountdown);
            Assert.Equal(end, t.RecurrenceEndDate);
        });
    }

    [Fact]
    public void List_SeriesWithEndDateNearingTail_DoesNotExtendPastEnd()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(10);
        var created = store.Create("Course", start, null, null, null, "day", 1, null, end);

        // The tail is inside the 30-day extension window, so each List() considers extending it
        store.List();
        var instances = store.List().Where(t => t.SeriesId == created.SeriesId).ToList();

        Assert.Equal(11, instances.Count);
        Assert.All(instances, t => Assert.True(t.DueDate <= end));
    }

    [Fact]
    public void Migrate_LegacyInstancesMissingEndDate_BackfillsAndDropsOverrun()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(2);
        var created = store.Create("Course", start, null, null, null, "day", 1, null, end, isCountdown: true);
        // What the old generator left behind: instances without the end date or countdown
        // flag, plus one generated past the end
        tmp.Db.NonQuery("UPDATE lu_tasks SET recurrence_end_date = NULL, is_countdown = 0 WHERE id <> series_id");
        tmp.Db.NonQuery($"""
            INSERT INTO lu_tasks (title, due_date, recurrence_unit, recurrence_interval, series_id)
            VALUES ('Course', '{start.AddDays(5):o}', 'day', 1, {created.Id})
            """);

        store.Migrate();

        var instances = store.List().Where(t => t.SeriesId == created.SeriesId).ToList();
        Assert.Equal(3, instances.Count);
        Assert.All(instances, t =>
        {
            Assert.True(t.IsCountdown);
            Assert.Equal(end, t.RecurrenceEndDate);
        });
    }

    [Fact]
    public void Update_NonexistentId_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        var updated = store.Update(9999, done: true, title: "Missing", dueDate: null,
            dueTime: null, description: null, assignee: null);

        Assert.Null(updated);
    }

    [Fact]
    public void Delete_Single_RemovesOnlyThatTask()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var keep = store.Create("Keep", null, null, null, null, null, null, null, null);
        var drop = store.Create("Drop", null, null, null, null, null, null, null, null);

        store.Delete(drop.Id);

        var titles = store.List().Select(t => t.Title).ToList();
        Assert.Contains("Keep", titles);
        Assert.DoesNotContain("Drop", titles);
    }

    [Fact]
    public void Delete_WithSeries_RemovesAllInstances()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var start = DateTime.UtcNow.Date;
        var created = store.Create("Daily", start, null, null, null, "day", 1, null, start.AddDays(4));

        store.Delete(created.Id, series: true);

        Assert.DoesNotContain(store.List(), t => t.SeriesId == created.SeriesId);
    }

    [Fact]
    public void Delete_WithSeriesFromMidSeries_KeepsEarlierInstancesAndStopsTheSeries()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var start = DateTime.UtcNow.Date;
        // No end date, so before the fix the horizon extension would regrow a year of instances
        var created = store.Create("Daily", start, null, null, null, "day", 1, null, null);
        var third = store.List().Single(t => t.SeriesId == created.SeriesId && t.DueDate == start.AddDays(2));

        store.Delete(third.Id, series: true);

        var remaining = store.List().Where(t => t.SeriesId == created.SeriesId)
            .Select(t => t.DueDate).OrderBy(d => d).ToList();
        Assert.Equal([start, start.AddDays(1)], remaining);
    }
}
