using System.Data.Common;
using Data.Abstractions;
using Tasks.Records;

namespace Tasks
{
    public sealed class TaskStore([FromKeyedServices("tasks")] IDatabase db)
    {
        public void Migrate()
        {
            db.NonQuery("""
                CREATE TABLE IF NOT EXISTS lu_tasks (
                    id         INTEGER  PRIMARY KEY AUTOINCREMENT,
                    title      TEXT     NOT NULL,
                    done       INTEGER  NOT NULL DEFAULT 0,
                    due_date   DATETIME NULL,
                    due_time   TEXT     NULL,
                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                )
                """);

            foreach (var col in new[]
            {
                "ALTER TABLE lu_tasks ADD COLUMN description TEXT NULL",
                "ALTER TABLE lu_tasks ADD COLUMN assignee TEXT NULL",
                "ALTER TABLE lu_tasks ADD COLUMN recurrence_unit TEXT NULL",
                "ALTER TABLE lu_tasks ADD COLUMN recurrence_interval INTEGER NULL",
                "ALTER TABLE lu_tasks ADD COLUMN recurrence_days TEXT NULL",
            })
            {
                try { db.NonQuery(col); } catch { /* column already exists */ }
            }
        }

        public IEnumerable<TaskItem> List() =>
            db.Query("""
                SELECT id, title, done, due_date, due_time, created_at,
                       description, assignee, recurrence_unit, recurrence_interval, recurrence_days
                FROM lu_tasks
                ORDER BY created_at DESC
                """, Map);

        public TaskItem Create(
            string title, DateTime? dueDate, string? dueTime,
            string? description, string? assignee,
            string? recurrenceUnit, int? recurrenceInterval, string? recurrenceDays) =>
            db.QueryOne("""
                INSERT INTO lu_tasks (title, due_date, due_time, description, assignee,
                                      recurrence_unit, recurrence_interval, recurrence_days)
                VALUES ($title, $due_date, $due_time, $description, $assignee,
                        $recurrence_unit, $recurrence_interval, $recurrence_days)
                RETURNING id, title, done, due_date, due_time, created_at,
                          description, assignee, recurrence_unit, recurrence_interval, recurrence_days
                """, Map, cmd =>
            {
                cmd.AddParam("$title", title);
                cmd.AddParam("$due_date", dueDate.HasValue ? dueDate.Value.ToString("o") : null);
                cmd.AddParam("$due_time", dueTime);
                cmd.AddParam("$description", description);
                cmd.AddParam("$assignee", assignee);
                cmd.AddParam("$recurrence_unit", recurrenceUnit);
                cmd.AddParam("$recurrence_interval", (object?)recurrenceInterval ?? DBNull.Value);
                cmd.AddParam("$recurrence_days", recurrenceDays);
            })!;

        public TaskItem? Update(long id, bool done, string? description, string? assignee)
        {
            var task = db.QueryOne("""
                SELECT id, title, done, due_date, due_time, created_at,
                       description, assignee, recurrence_unit, recurrence_interval, recurrence_days
                FROM lu_tasks WHERE id = $id
                """, Map, cmd => cmd.AddParam("$id", id));

            if (task is null) return null;

            var newDescription = description ?? task.Description;
            var newAssignee    = assignee    ?? task.Assignee;

            if (done && task.RecurrenceUnit is not null && task.DueDate.HasValue)
            {
                var nextDue = ComputeNextDue(
                    task.DueDate.Value,
                    task.RecurrenceUnit,
                    task.RecurrenceInterval ?? 1,
                    task.RecurrenceDays);

                return db.QueryOne("""
                    UPDATE lu_tasks
                    SET done = 0, due_date = $next_due, description = $description, assignee = $assignee
                    WHERE id = $id
                    RETURNING id, title, done, due_date, due_time, created_at,
                              description, assignee, recurrence_unit, recurrence_interval, recurrence_days
                    """, Map, cmd =>
                {
                    cmd.AddParam("$next_due", nextDue.ToString("o"));
                    cmd.AddParam("$description", newDescription);
                    cmd.AddParam("$assignee", newAssignee);
                    cmd.AddParam("$id", id);
                });
            }

            return db.QueryOne("""
                UPDATE lu_tasks
                SET done = $done, description = $description, assignee = $assignee
                WHERE id = $id
                RETURNING id, title, done, due_date, due_time, created_at,
                          description, assignee, recurrence_unit, recurrence_interval, recurrence_days
                """, Map, cmd =>
            {
                cmd.AddParam("$done", done ? 1 : 0);
                cmd.AddParam("$description", newDescription);
                cmd.AddParam("$assignee", newAssignee);
                cmd.AddParam("$id", id);
            });
        }

        public void Delete(long id) =>
            db.NonQuery("DELETE FROM lu_tasks WHERE id = $id",
                cmd => cmd.AddParam("$id", id));

        private static DateTime ComputeNextDue(DateTime current, string unit, int interval, string? days)
        {
            return unit switch
            {
                "month" => current.AddMonths(interval),
                "week" when days is not null => NextMatchingWeekday(current, days),
                "week" => current.AddDays(interval * 7),
                _ => current.AddDays(interval), // "day" and fallback
            };
        }

        private static DateTime NextMatchingWeekday(DateTime from, string days)
        {
            var targets = days.Split(',')
                .Select(d => d.Trim() switch
                {
                    "Sun" => DayOfWeek.Sunday,
                    "Mon" => DayOfWeek.Monday,
                    "Tue" => DayOfWeek.Tuesday,
                    "Wed" => DayOfWeek.Wednesday,
                    "Thu" => DayOfWeek.Thursday,
                    "Fri" => DayOfWeek.Friday,
                    "Sat" => DayOfWeek.Saturday,
                    _ => DayOfWeek.Monday,
                })
                .ToHashSet();

            var next = from.AddDays(1);
            for (int i = 0; i < 7; i++, next = next.AddDays(1))
                if (targets.Contains(next.DayOfWeek)) return next;

            return from.AddDays(7);
        }

        private static TaskItem Map(DbDataReader r) =>
            new(r.Field<long>("id"),
                r.Field<string>("title")!,
                r.Field<bool>("done"),
                r.Field<DateTime?>("due_date"),
                r.Field<string?>("due_time"),
                r.Field<DateTime>("created_at"),
                r.Field<string?>("description"),
                r.Field<string?>("assignee"),
                r.Field<string?>("recurrence_unit"),
                r.Field<int?>("recurrence_interval"),
                r.Field<string?>("recurrence_days"));
    }
}
