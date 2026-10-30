using System.Linq;
using Hearth.TestUtilities;
using Tasks;

namespace Tasks.Tests;

public sealed class TaskStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly TaskStore _store;

    public TaskStoreTests()
    {
        _store = new TaskStore(_db.Db);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Create_NonRecurring_AppearsInListWithoutSeries()
    {
        var created = _store.Create("Buy milk", null, null, null, null, null, null, null, null);

        Assert.Null(created.SeriesId);
        var listed = Assert.Single(_store.List());
        Assert.Equal("Buy milk", listed.Title);
        Assert.False(listed.Done);
    }

    [Fact]
    public void Update_MarksDone_KeepsFieldsSentBack()
    {
        var due = DateTime.UtcNow.Date.AddDays(2);
        var created = _store.Create("Water plants", due, "09:00", "Ferns too", "Sam", null, null, null, null);

        var updated = _store.Update(created.Id, done: true, title: "Water plants", dueDate: due,
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
        var created = _store.Create("Water plants", DateTime.UtcNow.Date.AddDays(2), "09:00",
            "Ferns too", "Sam", null, null, null, null);

        var updated = _store.Update(created.Id, done: false, title: "Water plants", dueDate: null,
            dueTime: null, description: null, assignee: null);

        Assert.NotNull(updated);
        Assert.Null(updated!.DueDate);
        Assert.Null(updated.DueTime);
        Assert.Null(updated.Description);
        Assert.Null(updated.Assignee);
        Assert.Null(Assert.Single(_store.List()).DueDate);
    }

    [Fact]
    public void Create_RecurringCountdownWithEndDate_GeneratesTheSeriesKeepingBoth()
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(4);

        var created = _store.Create("Trip", start, null, null, null, "day", 1, null, end, isCountdown: true);

        Assert.Equal(created.Id, created.SeriesId);
        var instances = _store.List().Where(t => t.SeriesId == created.SeriesId).ToList();
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
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(10);
        var created = _store.Create("Course", start, null, null, null, "day", 1, null, end);

        // The tail is inside the 30-day extension window, so each List() considers extending it
        _store.List();
        var instances = _store.List().Where(t => t.SeriesId == created.SeriesId).ToList();

        Assert.Equal(11, instances.Count);
        Assert.All(instances, t => Assert.True(t.DueDate <= end));
    }

    [Fact]
    public void List_SeriesWithDoneTail_DoesNotRegenerateDoneDays()
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(10);
        var created = _store.Create("Course", start, null, null, null, "day", 1, null, end);
        // Ticking off the last few days leaves an earlier undone row as the tail
        _db.Db.NonQuery($"UPDATE lu_tasks SET done = 1 WHERE series_id = {created.Id} AND date(due_date) > date('{start.AddDays(7):o}')");

        var instances = _store.List().Where(t => t.SeriesId == created.SeriesId).ToList();

        Assert.Equal(11, instances.Count);
        Assert.Equal(instances.Count, instances.Select(t => t.DueDate).Distinct().Count());
    }

    [Fact]
    public void List_CalledInParallel_GeneratesEachDayOnce()
    {
        var start = DateTime.UtcNow.Date;
        var created = _store.Create("Water plants", start, null, null, null, "day", 1, null, start.AddDays(20));
        // Back to just the root, so every List() below finds the series nearing its tail
        _db.Db.NonQuery($"DELETE FROM lu_tasks WHERE series_id = {created.Id} AND id <> series_id");

        Parallel.For(0, 8, _ => _store.List().ToList());

        var instances = _store.List().Where(t => t.SeriesId == created.SeriesId).ToList();
        Assert.Equal(21, instances.Count);
        Assert.Equal(instances.Count, instances.Select(t => t.DueDate).Distinct().Count());
    }

    [Fact]
    public void Migrate_DuplicateInstances_KeepsTheDoneOneElseTheOldest()
    {
        var start = DateTime.UtcNow.Date;
        var created = _store.Create("Course", start, null, null, null, "day", 1, null, start.AddDays(2));
        // What overlapping List() calls left behind: a second copy of day 1 and of day 2,
        // where day 2's original is undone and the copy is done
        _db.Db.NonQuery($"""
            INSERT INTO lu_tasks (title, due_date, recurrence_unit, recurrence_interval, series_id, recurrence_end_date)
            SELECT title, due_date, recurrence_unit, recurrence_interval, series_id, recurrence_end_date
            FROM lu_tasks WHERE series_id = {created.Id} AND id <> series_id
            """);
        _db.Db.NonQuery($"UPDATE lu_tasks SET done = 1 WHERE id = (SELECT MAX(id) FROM lu_tasks WHERE date(due_date) = date('{start.AddDays(2):o}'))");

        _store.Migrate();

        var instances = _store.List().Where(t => t.SeriesId == created.SeriesId).OrderBy(t => t.DueDate).ToList();
        Assert.Equal(3, instances.Count);
        Assert.True(instances[2].Done);
    }

    [Fact]
    public void Migrate_LegacyInstancesMissingEndDate_BackfillsAndDropsOverrun()
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(2);
        var created = _store.Create("Course", start, null, null, null, "day", 1, null, end, isCountdown: true);
        // What the old generator left behind: instances without the end date or countdown
        // flag, plus one generated past the end
        _db.Db.NonQuery("UPDATE lu_tasks SET recurrence_end_date = NULL, is_countdown = 0 WHERE id <> series_id");
        _db.Db.NonQuery($"""
            INSERT INTO lu_tasks (title, due_date, recurrence_unit, recurrence_interval, series_id)
            VALUES ('Course', '{start.AddDays(5):o}', 'day', 1, {created.Id})
            """);

        _store.Migrate();

        var instances = _store.List().Where(t => t.SeriesId == created.SeriesId).ToList();
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
        var updated = _store.Update(9999, done: true, title: "Missing", dueDate: null,
            dueTime: null, description: null, assignee: null);

        Assert.Null(updated);
    }

    [Fact]
    public void Delete_Single_RemovesOnlyThatTask()
    {
        var keep = _store.Create("Keep", null, null, null, null, null, null, null, null);
        var drop = _store.Create("Drop", null, null, null, null, null, null, null, null);

        _store.Delete(drop.Id);

        var titles = _store.List().Select(t => t.Title).ToList();
        Assert.Contains("Keep", titles);
        Assert.DoesNotContain("Drop", titles);
    }

    [Fact]
    public void Delete_WithSeries_RemovesAllInstances()
    {
        var start = DateTime.UtcNow.Date;
        var created = _store.Create("Daily", start, null, null, null, "day", 1, null, start.AddDays(4));

        _store.Delete(created.Id, series: true);

        Assert.DoesNotContain(_store.List(), t => t.SeriesId == created.SeriesId);
    }

    [Fact]
    public void Delete_WithSeriesFromMidSeries_KeepsEarlierInstancesAndStopsTheSeries()
    {
        var start = DateTime.UtcNow.Date;
        // No end date, so before the fix the horizon extension would regrow a year of instances
        var created = _store.Create("Daily", start, null, null, null, "day", 1, null, null);
        var third = _store.List().Single(t => t.SeriesId == created.SeriesId && t.DueDate == start.AddDays(2));

        _store.Delete(third.Id, series: true);

        var remaining = _store.List().Where(t => t.SeriesId == created.SeriesId)
            .Select(t => t.DueDate).OrderBy(d => d).ToList();
        Assert.Equal([start, start.AddDays(1)], remaining);
    }
}
