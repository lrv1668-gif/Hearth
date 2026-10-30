using Data;

namespace Hearth.TestUtilities;

/// <summary>
/// Wraps a real <see cref="Database"/> backed by a throwaway SQLite file.
/// <c>Database</c> opens a fresh connection per query, so an in-memory DB
/// would not persist across calls — a temp file is required.
/// </summary>
public sealed class TempDatabase : IDisposable
{
    /// <summary>The SQLite file, for opening a second <see cref="Database"/> on it.</summary>
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"hearth-test-{Guid.NewGuid():N}.db");

    public Database Db { get; }

    public TempDatabase() => Db = new Database(Path);

    public void Dispose()
    {
        // WAL mode leaves -wal and -shm files beside the database
        foreach (var path in new[] { Path, Path + "-wal", Path + "-shm" })
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
