using Microsoft.Data.Sqlite;

public record TaskItem(long Id, string Title, bool Done, DateTime? DueDate, string? DueTime, DateTime CreatedAt);

public sealed class TaskStore
{
    private readonly string _cs;
    public TaskStore(string dbPath) => _cs = $"Data Source={dbPath}";

    public void Migrate()
    {
        using var conn = Open();
        Exec(conn,
            """
            CREATE TABLE IF NOT EXISTS lu_tasks (
                id         INTEGER  PRIMARY KEY AUTOINCREMENT,
                title      TEXT     NOT NULL,
                done       INTEGER  NOT NULL DEFAULT 0,
                due_date   DATETIME NULL,
                due_time   TEXT     NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """
        );
        // add columns to existing databases that predate these fields
        try { Exec(conn, "ALTER TABLE lu_tasks ADD COLUMN due_date DATETIME NULL"); } catch { }
        try { Exec(conn, "ALTER TABLE lu_tasks ADD COLUMN due_time TEXT NULL"); } catch { }
    }

    public IEnumerable<TaskItem> List()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT id, title, done, due_date, due_time, created_at FROM lu_tasks ORDER BY created_at DESC";

        using var r = cmd.ExecuteReader();
        var tasks = new List<TaskItem>();
        while (r.Read()) tasks.Add(Map(r));

        return tasks;
    }

    public TaskItem Create(string title, DateTime? dueDate, string? dueTime)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            INSERT INTO lu_tasks (title, due_date, due_time)
            VALUES ($title, $due_date, $due_time)
            RETURNING id, title, done, due_date, due_time, created_at
        """;

        cmd.Parameters.AddWithValue("$title", title);
        cmd.Parameters.AddWithValue("$due_date", dueDate.HasValue ? dueDate.Value.ToString("o") : DBNull.Value);
        cmd.Parameters.AddWithValue("$due_time", (object?)dueTime ?? DBNull.Value);

        using var r = cmd.ExecuteReader();
        r.Read();

        return Map(r);
    }

    public TaskItem? Update(long id, bool done)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            UPDATE lu_tasks
            SET done = $done
            WHERE id = $id
            RETURNING id, title, done, due_date, due_time, created_at
        """;

        cmd.Parameters.AddWithValue("$done", done ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        return r.Read() ? Map(r) : null;
    }

    public void Delete(long id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM lu_tasks WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_cs);
        conn.Open();
        return conn;
    }

    private static void Exec(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static TaskItem Map(SqliteDataReader r) =>
        new(r.GetInt64(0), r.GetString(1), r.GetBoolean(2),
            r.IsDBNull(3) ? null : r.GetDateTime(3),
            r.IsDBNull(4) ? null : r.GetString(4),
            r.GetDateTime(5));
}
