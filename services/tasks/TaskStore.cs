using Data;
using Microsoft.Data.Sqlite;
using Tasks.Records;

namespace Tasks
{
    public sealed class TaskStore([FromKeyedServices("tasks")] Database db)
    {
        public void Migrate() => db.NonQuery("""
            CREATE TABLE IF NOT EXISTS lu_tasks (
                id         INTEGER  PRIMARY KEY AUTOINCREMENT,
                title      TEXT     NOT NULL,
                done       INTEGER  NOT NULL DEFAULT 0,
                due_date   DATETIME NULL,
                due_time   TEXT     NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """);

        public IEnumerable<TaskItem> List() =>
            db.Query("""
                SELECT id, title, done, due_date, due_time, created_at
                FROM lu_tasks
                ORDER BY created_at DESC
                """, Map);

        public TaskItem Create(string title, DateTime? dueDate, string? dueTime) =>
            db.QueryOne("""
                INSERT INTO lu_tasks (title, due_date, due_time)
                VALUES ($title, $due_date, $due_time)
                RETURNING id, title, done, due_date, due_time, created_at
                """, Map, cmd =>
            {
                cmd.Parameters.AddWithValue("$title", title);
                cmd.Parameters.AddWithValue("$due_date", dueDate.HasValue ? dueDate.Value.ToString("o") : DBNull.Value);
                cmd.Parameters.AddWithValue("$due_time", (object?)dueTime ?? DBNull.Value);
            })!;

        public TaskItem? Update(long id, bool done) =>
            db.QueryOne("""
                UPDATE lu_tasks
                SET done = $done
                WHERE id = $id
                RETURNING id, title, done, due_date, due_time, created_at
                """, Map, cmd =>
            {
                cmd.Parameters.AddWithValue("$done", done ? 1 : 0);
                cmd.Parameters.AddWithValue("$id", id);
            });

        public void Delete(long id) =>
            db.NonQuery("DELETE FROM lu_tasks WHERE id = $id",
                cmd => cmd.Parameters.AddWithValue("$id", id));

        private static TaskItem Map(SqliteDataReader r) =>
            new(r.GetInt64(0), r.GetString(1), r.GetBoolean(2),
                r.IsDBNull(3) ? null : r.GetDateTime(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                r.GetDateTime(5));
    }
}
