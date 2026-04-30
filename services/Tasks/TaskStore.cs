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
                "ALTER TABLE lu_tasks ADD COLUMN series_id INTEGER NULL",
                "ALTER TABLE lu_tasks ADD COLUMN recurrence_end_date DATETIME NULL",
            })
            {
                try { db.NonQuery(col); } catch { /* column already exists */ }
            }

            // Canonicalize existing recurring rows that pre-date series support
            db.NonQuery("UPDATE lu_tasks SET series_id = id WHERE recurrence_unit IS NOT NULL AND series_id IS NULL");
        }

        public IEnumerable<TaskItem> List()
        {
            ExtendNearingSeriesHorizon();

            var through = DateTime.UtcNow.Date.AddDays(60).ToString("o");
            return db.Query("""
                SELECT id, title, done, due_date, due_time, created_at,
                       description, assignee, recurrence_unit, recurrence_interval, recurrence_days, series_id, recurrence_end_date
                FROM lu_tasks
                WHERE done = 1
                   OR due_date IS NULL
                   OR date(due_date) <= date($through)
                ORDER BY due_date ASC, created_at DESC
                """, Map, cmd => cmd.AddParam("$through", through));
        }

        public TaskItem Create(
            string title, DateTime? dueDate, string? dueTime,
            string? description, string? assignee,
            string? recurrenceUnit, int? recurrenceInterval, string? recurrenceDays,
            DateTime? recurrenceEndDate)
        {
            var first = db.QueryOne("""
                INSERT INTO lu_tasks (title, due_date, due_time, description, assignee,
                                      recurrence_unit, recurrence_interval, recurrence_days, recurrence_end_date)
                VALUES ($title, $due_date, $due_time, $description, $assignee,
                        $recurrence_unit, $recurrence_interval, $recurrence_days, $recurrence_end_date)
                RETURNING id, title, done, due_date, due_time, created_at,
                          description, assignee, recurrence_unit, recurrence_interval, recurrence_days, series_id, recurrence_end_date
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
                cmd.AddParam("$recurrence_end_date", recurrenceEndDate.HasValue ? recurrenceEndDate.Value.ToString("o") : null);
            })!;

            if (recurrenceUnit is not null && dueDate.HasValue)
            {
                db.NonQuery("UPDATE lu_tasks SET series_id = $sid WHERE id = $id",
                    cmd => { cmd.AddParam("$sid", first.Id); cmd.AddParam("$id", first.Id); });
                GenerateInstances(first.Id, dueDate.Value, recurrenceUnit, recurrenceInterval ?? 1, recurrenceDays, recurrenceEndDate);
                return first with { SeriesId = first.Id };
            }

            return first;
        }

        public TaskItem? Update(long id, bool done, string? description, string? assignee)
        {
            var task = db.QueryOne("""
                SELECT id, title, done, due_date, due_time, created_at,
                       description, assignee, recurrence_unit, recurrence_interval, recurrence_days, series_id, recurrence_end_date
                FROM lu_tasks WHERE id = $id
                """, Map, cmd => cmd.AddParam("$id", id));

            if (task is null) return null;

            var newDescription = description ?? task.Description;
            var newAssignee    = assignee    ?? task.Assignee;

            return db.QueryOne("""
                UPDATE lu_tasks
                SET done = $done, description = $description, assignee = $assignee
                WHERE id = $id
                RETURNING id, title, done, due_date, due_time, created_at,
                          description, assignee, recurrence_unit, recurrence_interval, recurrence_days, series_id, recurrence_end_date
                """, Map, cmd =>
            {
                cmd.AddParam("$done", done ? 1 : 0);
                cmd.AddParam("$description", newDescription);
                cmd.AddParam("$assignee", newAssignee);
                cmd.AddParam("$id", id);
            });
        }

        public void Delete(long id, bool series = false)
        {
            if (series)
            {
                var sid = db.QueryOne<long?>("SELECT series_id FROM lu_tasks WHERE id = $id",
                    r => r.IsDBNull(0) ? null : r.GetInt64(0),
                    cmd => cmd.AddParam("$id", id));
                if (sid.HasValue)
                    db.NonQuery("DELETE FROM lu_tasks WHERE series_id = $sid",
                        cmd => cmd.AddParam("$sid", sid.Value));
                else
                    db.NonQuery("DELETE FROM lu_tasks WHERE id = $id",
                        cmd => cmd.AddParam("$id", id));
            }
            else
            {
                db.NonQuery("DELETE FROM lu_tasks WHERE id = $id",
                    cmd => cmd.AddParam("$id", id));
            }
        }

        private void GenerateInstances(long seriesId, DateTime start, string unit, int interval, string? days, DateTime? endDate = null)
        {
            var horizon = endDate.HasValue
                ? endDate.Value.Date
                : DateTime.UtcNow.Date.AddYears(1);
            var current = start.Date;
            int count = 0;

            while (count < 400)
            {
                current = ComputeNextDue(current, unit, interval, days);
                if (current > horizon) break;
                db.NonQuery("""
                    INSERT INTO lu_tasks (title, due_date, due_time, description, assignee,
                                          recurrence_unit, recurrence_interval, recurrence_days, series_id)
                    SELECT title, $due_date, due_time, description, assignee,
                           recurrence_unit, recurrence_interval, recurrence_days, $sid
                    FROM lu_tasks WHERE id = $sid
                    """, cmd =>
                {
                    cmd.AddParam("$due_date", current.ToString("o"));
                    cmd.AddParam("$sid", seriesId);
                });
                count++;
            }
        }

        private void ExtendNearingSeriesHorizon()
        {
            var threshold = DateTime.UtcNow.Date.AddDays(30).ToString("o");
            var nearingTails = db.Query("""
                SELECT series_id, MAX(due_date) as tail,
                       recurrence_unit, recurrence_interval, recurrence_days, recurrence_end_date
                FROM lu_tasks
                WHERE series_id IS NOT NULL AND done = 0
                GROUP BY series_id
                HAVING MAX(due_date) < $threshold
                """,
                r => (
                    SeriesId: r.GetInt64(0),
                    Tail: r.GetDateTime(1),
                    Unit: r.GetString(2),
                    Interval: r.IsDBNull(3) ? 1 : r.GetInt32(3),
                    Days: r.IsDBNull(4) ? null : r.GetString(4),
                    EndDate: r.IsDBNull(5) ? (DateTime?)null : r.GetDateTime(5)
                ),
                cmd => cmd.AddParam("$threshold", threshold));

            foreach (var s in nearingTails)
                GenerateInstances(s.SeriesId, s.Tail, s.Unit, s.Interval, s.Days, s.EndDate);
        }

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
            new(r.GetInt64(0), r.GetString(1), r.GetBoolean(2),
                r.IsDBNull(3)  ? null : r.GetDateTime(3),
                r.IsDBNull(4)  ? null : r.GetString(4),
                r.GetDateTime(5),
                r.IsDBNull(6)  ? null : r.GetString(6),
                r.IsDBNull(7)  ? null : r.GetString(7),
                r.IsDBNull(8)  ? null : r.GetString(8),
                r.IsDBNull(9)  ? null : r.GetInt32(9),
                r.IsDBNull(10) ? null : r.GetString(10),
                r.IsDBNull(11) ? null : r.GetInt64(11),
                r.IsDBNull(12) ? null : r.GetDateTime(12));
    }
}
