using System.Linq;
using Data.Abstractions;
using Plants;
using Plants.Tests.Helpers;
using Xunit;

namespace Plants.Tests;

public sealed class PlantStoreTests
{
    private static PlantStore Migrated(TempDatabase tmp)
    {
        var store = new PlantStore(tmp.Db);
        store.Migrate();
        return store;
    }

    [Fact]
    public void Create_NewPlant_HasNullLastWateredAt_AndAppearsInList()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        var created = store.Create("Monstera", "Monstera deliciosa", 7);

        Assert.Null(created.LastWateredAt);
        var listed = Assert.Single(store.List());
        Assert.Equal("Monstera", listed.Name);
        Assert.Equal("Monstera deliciosa", listed.Species);
        Assert.Equal(7, listed.WateringIntervalDays);
    }

    [Fact]
    public void Create_NewPlant_NotYetOverdue()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        var created = store.Create("Pothos", null, 7);

        Assert.False(created.IsOverdue);
        Assert.Equal(created.CreatedAt.Date.AddDays(7), created.NextWateringDue);
    }

    [Fact]
    public void Water_SetsLastWateredAt_AndRecalculatesNextDue()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var created = store.Create("Fern", null, 5);

        var watered = store.Water(created.Id);

        Assert.NotNull(watered);
        Assert.NotNull(watered!.LastWateredAt);
        Assert.Equal(watered.LastWateredAt!.Value.Date.AddDays(5), watered.NextWateringDue);
        Assert.False(watered.IsOverdue);
    }

    [Fact]
    public void Water_NonexistentId_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        Assert.Null(store.Water(9999));
    }

    [Fact]
    public void Update_ChangesNameSpeciesAndInterval()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var created = store.Create("Snake Plant", null, 14);

        var updated = store.Update(created.Id, "Snake Plant (living room)", "Dracaena trifasciata", 21);

        Assert.NotNull(updated);
        Assert.Equal("Snake Plant (living room)", updated!.Name);
        Assert.Equal("Dracaena trifasciata", updated.Species);
        Assert.Equal(21, updated.WateringIntervalDays);
    }

    [Fact]
    public void Update_NonexistentId_ReturnsNull()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);

        var updated = store.Update(9999, "Ghost Plant", null, 7);

        Assert.Null(updated);
    }

    [Fact]
    public void Delete_RemovesOnlyThatPlant()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var keep = store.Create("Keep", null, 7);
        var drop = store.Create("Drop", null, 7);

        store.Delete(drop.Id);

        var names = store.List().Select(p => p.Name).ToList();
        Assert.Contains("Keep", names);
        Assert.DoesNotContain("Drop", names);
    }

    [Fact]
    public void List_OrdersMostOverdueFirst()
    {
        using var tmp = new TempDatabase();
        var store = Migrated(tmp);
        var soon = store.Create("Due Soon", null, 30);
        var overdue = store.Create("Overdue", null, 7);

        // Backdate "Overdue" so its interval has clearly elapsed.
        tmp.Db.NonQuery("UPDATE lu_plants SET last_watered_at = $t WHERE id = $id", cmd =>
        {
            cmd.AddParam("$t", DateTime.UtcNow.AddDays(-10).ToString("o"));
            cmd.AddParam("$id", overdue.Id);
        });

        var listed = store.List().ToList();

        Assert.True(listed[0].IsOverdue);
        Assert.Equal("Overdue", listed[0].Name);
        Assert.False(listed[1].IsOverdue);
        Assert.Equal("Due Soon", listed[1].Name);
    }
}
