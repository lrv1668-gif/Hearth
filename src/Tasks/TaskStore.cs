using System.Data.Common;
using Data.Abstractions;
using Tasks.Records;

namespace Tasks
{
    public sealed class TaskStore([FromKeyedServices("tasks")] IDatabase db)
    {
        // List() extends series as it reads, and every screen polls it: one extension at a time
        private readonly Lock _extendLock = new();

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
                "ALTER TABLE lu_tasks ADD COLUMN is_countdown BIT NOT NULL DEFAULT 0",
            })
            {
                try { db.NonQuery(col); }
                catch (DbException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
                {
                    // column already exists from a previous run
                }
            }

            // Canonicalize existing recurring rows that pre-date series support
            db.NonQuery("UPDATE lu_tasks SET series_id = id WHERE recurrence_unit IS NOT NULL AND series_id IS NULL");

            // Instances generated before they copied the series' end date and countdown flag
            // take them from the series root, then drop the undone ones the horizon extension
            // generated past that end date
            db.NonQuery("""
                UPDATE lu_tasks
                SET recurrence_end_date = (SELECT r.recurrence_end_date FROM lu_tasks r WHERE r.id = lu_tasks.series_id),
                    is_countdown        = (SELECT r.is_countdown        FROM lu_tasks r WHERE r.id = lu_tasks.series_id)
                WHERE series_id IS NOT NULL AND id <> series_id
                  AND EXISTS (SELECT 1 FROM lu_tasks r WHERE r.id = lu_tasks.series_id)
                """);
            db.NonQuery("""
                DELETE FROM lu_tasks
                WHERE series_id IS NOT NULL AND done = 0 AND recurrence_end_date IS NOT NULL
                  AND date(due_date) > date(recurrence_end_date)
                """);

            // Overlapping List() calls (and a done tail, which the extension used to grow
            // past again) generated a second instance for days a series already had. Keep the
            // done one, else the oldest; never drop a done instance.
            db.NonQuery("""
                DELETE FROM lu_tasks
                WHERE series_id IS NOT NULL AND id <> series_id AND done = 0 AND due_date IS NOT NULL
                  AND EXISTS (
                      SELECT 1 FROM lu_tasks k
                      WHERE k.series_id = lu_tasks.series_id AND k.id <> lu_tasks.id
                        AND date(k.due_date) = date(lu_tasks.due_date)
                        AND (k.done = 1 OR k.id < lu_tasks.id))
                """);
        }

        public IEnumerable<TaskItem> List()
        {
            ExtendNearingSeriesHorizon();

            var through = DateTime.UtcNow.Date.AddDays(365).ToString("o");
            return db.Query("""
                SELECT id, title, done, due_date, due_time, created_at,
                       description, assignee, recurrence_unit, recurrence_interval, recurrence_days, series_id, recurrence_end_date, is_countdown
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
            DateTime? recurrenceEndDate, bool isCountdown = false)
        {
            var first = db.QueryOne("""
                INSERT INTO lu_tasks (title, due_date, due_time, description, assignee,
                                      recurrence_unit, recurrence_interval, recurrence_days, recurrence_end_date, is_countdown)
                VALUES ($title, $due_date, $due_time, $description, $assignee,
                        $recurrence_unit, $recurrence_interval, $recurrence_days, $recurrence_end_date, $is_countdown)
                RETURNING id, title, done, due_date, due_time, created_at,
                          description, assignee, recurrence_unit, recurrence_interval, recurrence_days, series_id, recurrence_end_date, is_countdown
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
                cmd.AddParam("$is_countdown", isCountdown ? 1 : 0);
            })!;

            if (recurrenceUnit is not null && dueDate.HasValue)
            {
                db.NonQuery("UPDATE lu_tasks SET series_id = $sid WHERE id = $id",
                    cmd => { cmd.AddParam("$sid", first.Id); cmd.AddParam("$id", first.Id); });
                GenerateInstances(first.Id, first.Id, dueDate.Value, recurrenceUnit, recurrenceInterval ?? 1, recurrenceDays, recurrenceEndDate);
                return first with { SeriesId = first.Id };
            }

            return first;
        }

        /// <summary>
        /// Replaces the editable fields with the given values — a null clears that field.
        /// Returns null when no task has this id.
        /// </summary>
        public TaskItem? Update(long id, bool done, string title, DateTime? dueDate, string? dueTime, string? description, string? assignee)
        {
            return db.QueryOne("""
                UPDATE lu_tasks
                SET done = $done, title = $title, due_date = $due_date, due_time = $due_time,
                    description = $description, assignee = $assignee
                WHERE id = $id
                RETURNING id, title, done, due_date, due_time, created_at,
                          description, assignee, recurrence_unit, recurrence_interval, recurrence_days, series_id, recurrence_end_date, is_countdown
                """, Map, cmd =>
            {
                cmd.AddParam("$done", done ? 1 : 0);
                cmd.AddParam("$title", title);
                cmd.AddParam("$due_date", dueDate.HasValue ? dueDate.Value.ToString("o") : null);
                cmd.AddParam("$due_time", dueTime);
                cmd.AddParam("$description", description);
                cmd.AddParam("$assignee", assignee);
                cmd.AddParam("$id", id);
            });
        }

        public void Delete(long id, bool series = false)
        {
            if (series)
            {
                var row = db.QueryOne("SELECT series_id, due_date FROM lu_tasks WHERE id = $id",
                    r => (SeriesId: r.Field<long?>("series_id"), DueDate: r.Field<DateTime?>("due_date")),
                    cmd => cmd.AddParam("$id", id));
                if (row is { SeriesId: { } sid, DueDate: { } from })
                {
                    // "All future": this instance and every later one; earlier ones stay as history
                    db.NonQuery("DELETE FROM lu_tasks WHERE series_id = $sid AND date(due_date) >= date($from)", cmd =>
                    {
                        cmd.AddParam("$sid", sid);
                        cmd.AddParam("$from", from.ToString("o"));
                    });
                    // End the series the day before, so the horizon extension doesn't regrow it
                    db.NonQuery("UPDATE lu_tasks SET recurrence_end_date = $end WHERE series_id = $sid", cmd =>
                    {
                        cmd.AddParam("$sid", sid);
                        cmd.AddParam("$end", from.Date.AddDays(-1).ToString("o"));
                    });
                }
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

        /// <summary>
        /// Inserts the series' instances after <paramref name="start"/>, up to its end date or a year out,
        /// copying everything but the due date from the <paramref name="templateId"/> row.
        /// </summary>
        private void GenerateInstances(long seriesId, long templateId, DateTime start, string unit, int interval, string? days, DateTime? endDate = null)
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
                                          recurrence_unit, recurrence_interval, recurrence_days, series_id,
                                          recurrence_end_date, is_countdown)
                    SELECT title, $due_date, due_time, description, assignee,
                           recurrence_unit, recurrence_interval, recurrence_days, $sid,
                           recurrence_end_date, is_countdown
                    FROM lu_tasks WHERE id = $template
                      AND NOT EXISTS (
                          SELECT 1 FROM lu_tasks e
                          WHERE e.series_id = $sid AND date(e.due_date) = date($due_date))
                    """, cmd =>
                {
                    cmd.AddParam("$due_date", current.ToString("o"));
                    cmd.AddParam("$sid", seriesId);
                    cmd.AddParam("$template", templateId);
                });
                count++;
            }
        }

        private void ExtendNearingSeriesHorizon()
        {
            lock (_extendLock) ExtendNearingSeriesHorizonLocked();
        }

        private void ExtendNearingSeriesHorizonLocked()
        {
            var threshold = DateTime.UtcNow.Date.AddDays(30).ToString("o");
            // With a single MAX() aggregate SQLite takes the bare columns (id, recurrence_*) from
            // the tail row, which becomes the template. The end date is read across the whole
            // series so one row missing it can't let the series run past its end.
            var nearingTails = db.Query("""
                SELECT t.series_id, MAX(t.due_date) AS tail, t.id,
                       t.recurrence_unit, t.recurrence_interval, t.recurrence_days,
                       (SELECT MIN(e.recurrence_end_date) FROM lu_tasks e WHERE e.series_id = t.series_id) AS end_date
                FROM lu_tasks t
                WHERE t.series_id IS NOT NULL AND t.done = 0
                GROUP BY t.series_id
                HAVING MAX(t.due_date) < $threshold
                """,
                r => (
                    SeriesId: r.GetInt64(0),
                    Tail: r.GetDateTime(1),
                    TemplateId: r.GetInt64(2),
                    Unit: r.GetString(3),
                    Interval: r.IsDBNull(4) ? 1 : r.GetInt32(4),
                    Days: r.IsDBNull(5) ? null : r.GetString(5),
                    EndDate: r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6)
                ),
                cmd => cmd.AddParam("$threshold", threshold));

            foreach (var s in nearingTails)
                GenerateInstances(s.SeriesId, s.TemplateId, s.Tail, s.Unit, s.Interval, s.Days, s.EndDate);
        }

        internal static DateTime ComputeNextDue(DateTime current, string unit, int interval, string? days)
        {
            return unit switch
            {
                "month" => current.AddMonths(interval),
                "week" when days is not null => NextMatchingWeekday(current, days),
                "week" => current.AddDays(interval * 7),
                _ => current.AddDays(interval), // "day" and fallback
            };
        }

        internal static DateTime NextMatchingWeekday(DateTime from, string days)
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
                r.Field<string?>("recurrence_days"),
                r.Field<long?>("series_id"),
                r.Field<DateTime?>("recurrence_end_date"),
                r.Field<bool>("is_countdown"));
    }
}
