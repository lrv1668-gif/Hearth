using System.Linq;
using Tasks;
using Tasks.Tests.Helpers;
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
    public void Update_MarksDone_AndPreservesUnsetFields()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var created = store.Create("Water plants", null, null, null, null, null, null, null, null);

        var updated = store.Update(created.Id, done: true, title: null, dueDate: null,
            dueTime: null, description: null, assignee: null);

        Assert.NotNull(updated);
        Assert.True(updated!.Done);
        Assert.Equal("Water plants", updated.Title);
    }

    [Fact]
    public void Update_NonexistentId_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        var updated = store.Update(9999, done: true, title: null, dueDate: null,
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
}
