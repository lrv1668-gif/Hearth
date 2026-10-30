using Data;

namespace Hearth.TestUtilities;

/// <summary>
/// Wraps a real <see cref="Database"/> backed by a throwaway SQLite file.
/// <c>Database</c> opens a fresh connection per query, so an in-memory DB
/// would not persist across calls — a temp file is required.
/// </summary>
public sealed class TempDatabase : IDisposable
{
    private readonly string _path;
    public Database Db { get; }

    public TempDatabase()
    {
        _path = Path.Combine(Path.GetTempPath(), $"hearth-test-{Guid.NewGuid():N}.db");
        Db = new Database(_path);
    }

    public void Dispose()
    {
        // WAL mode leaves -wal and -shm files beside the database
        foreach (var path in new[] { _path, _path + "-wal", _path + "-shm" })
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
