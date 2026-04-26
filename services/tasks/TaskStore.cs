using System.Data.Common;
using Data.Abstractions;
using Tasks.Records;

namespace Tasks
{
    public sealed class TaskStore([FromKeyedServices("tasks")] IDatabase db)
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
                cmd.AddParam("$title", title);
                cmd.AddParam("$due_date", dueDate.HasValue ? dueDate.Value.ToString("o") : null);
                cmd.AddParam("$due_time", dueTime);
            })!;

        public TaskItem? Update(long id, bool done) =>
            db.QueryOne("""
                UPDATE lu_tasks
                SET done = $done
                WHERE id = $id
                RETURNING id, title, done, due_date, due_time, created_at
                """, Map, cmd =>
            {
                cmd.AddParam("$done", done ? 1 : 0);
                cmd.AddParam("$id", id);
            });

        public void Delete(long id) =>
            db.NonQuery("DELETE FROM lu_tasks WHERE id = $id",
                cmd => cmd.AddParam("$id", id));

        private static TaskItem Map(DbDataReader r) =>
            new(r.GetInt64(0), r.GetString(1), r.GetBoolean(2),
                r.IsDBNull(3) ? null : r.GetDateTime(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                r.GetDateTime(5));
    }
}
