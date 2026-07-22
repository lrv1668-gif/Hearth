using Data.Abstractions;
using Xunit;

namespace Data.Tests;

public sealed class DatabaseTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"hearth-datatest-{Guid.NewGuid():N}.db");
    private readonly Database _db;

    public DatabaseTests()
    {
        _db = new Database(_path);
    }

    public void Dispose()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { /* best effort */ }
    }

    [Fact]
    public void NonQuery_CreateTableThenInsert_QueryOneReturnsRow()
    {
        _db.NonQuery("CREATE TABLE widgets (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (1, 'sprocket')");

        var name = _db.QueryOne("SELECT name FROM widgets WHERE id = 1", r => r.Field<string>("name"));

        Assert.Equal("sprocket", name);
    }

    [Fact]
    public void Query_MultipleMatchingRows_ReturnsAllInInsertOrder()
    {
        _db.NonQuery("CREATE TABLE widgets (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (1, 'a')");
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (2, 'b')");
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (3, 'c')");

        var names = _db.Query("SELECT name FROM widgets ORDER BY id", r => r.Field<string>("name")).ToList();

        Assert.Equal(["a", "b", "c"], names);
    }

    [Fact]
    public void QueryOne_NoMatchingRow_ReturnsDefault()
    {
        _db.NonQuery("CREATE TABLE widgets (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");

        var name = _db.QueryOne("SELECT name FROM widgets WHERE id = 1", r => r.Field<string>("name"));

        Assert.Null(name);
    }

    [Fact]
    public void NonQuery_ParameterizedInsert_BindsValueCorrectly()
    {
        _db.NonQuery("CREATE TABLE widgets (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");

        _db.NonQuery("INSERT INTO widgets (id, name) VALUES ($id, $name)", cmd =>
        {
            cmd.AddParam("$id", 1);
            cmd.AddParam("$name", "it's a widget"); // apostrophe would break naive string concatenation
        });

        var name = _db.QueryOne("SELECT name FROM widgets WHERE id = 1", r => r.Field<string>("name"));
        Assert.Equal("it's a widget", name);
    }

    [Fact]
    public void NonQuery_ParameterWithNullValue_StoresAsDbNull()
    {
        _db.NonQuery("CREATE TABLE widgets (id INTEGER PRIMARY KEY, name TEXT NULL)");

        _db.NonQuery("INSERT INTO widgets (id, name) VALUES ($id, $name)", cmd =>
        {
            cmd.AddParam("$id", 1);
            cmd.AddParam("$name", null);
        });

        var name = _db.QueryOne("SELECT name FROM widgets WHERE id = 1", r => r.Field<string>("name"));

        Assert.Null(name);
    }

    [Fact]
    public void Query_ConnectionOpenedPerCall_SecondQueryAfterExternalWriteSeesNewData()
    {
        // Database opens a fresh connection per call rather than holding one open, so a
        // write from a second Database instance against the same file is immediately
        // visible to this one — there is no per-instance connection or result caching.
        _db.NonQuery("CREATE TABLE widgets (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (1, 'a')");

        var other = new Database(_path);
        other.NonQuery("INSERT INTO widgets (id, name) VALUES (2, 'b')");

        var names = _db.Query("SELECT name FROM widgets ORDER BY id", r => r.Field<string>("name")).ToList();

        Assert.Equal(["a", "b"], names);
    }
}
