using Data.Abstractions;
using Hearth.TestUtilities;

namespace Data.Tests;

public sealed class DatabaseTests : IDisposable
{
    private readonly TempDatabase _tmp = new();
    private readonly Database _db;

    public DatabaseTests()
    {
        _db = _tmp.Db;
        _db.NonQuery("CREATE TABLE widgets (id INTEGER PRIMARY KEY, name TEXT NULL)");
    }

    public void Dispose() => _tmp.Dispose();

    [Fact]
    public void NonQuery_Insert_QueryOneReturnsRow()
    {
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (1, 'sprocket')");

        var name = _db.QueryOne("SELECT name FROM widgets WHERE id = 1", r => r.Field<string>("name"));

        Assert.Equal("sprocket", name);
    }

    [Fact]
    public void Query_MultipleMatchingRows_ReturnsAllInInsertOrder()
    {
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (1, 'a')");
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (2, 'b')");
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (3, 'c')");

        var names = _db.Query("SELECT name FROM widgets ORDER BY id", r => r.Field<string>("name")).ToList();

        Assert.Equal(["a", "b", "c"], names);
    }

    [Fact]
    public void QueryOne_NoMatchingRow_ReturnsDefault()
    {
        var name = _db.QueryOne("SELECT name FROM widgets WHERE id = 1", r => r.Field<string>("name"));

        Assert.Null(name);
    }

    [Fact]
    public void NonQuery_ParameterizedInsert_BindsValueCorrectly()
    {
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
        _db.NonQuery("INSERT INTO widgets (id, name) VALUES (1, 'a')");

        var other = new Database(_tmp.Path);
        other.NonQuery("INSERT INTO widgets (id, name) VALUES (2, 'b')");

        var names = _db.Query("SELECT name FROM widgets ORDER BY id", r => r.Field<string>("name")).ToList();

        Assert.Equal(["a", "b"], names);
    }
}
