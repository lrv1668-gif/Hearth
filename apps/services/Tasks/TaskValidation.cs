using System.Text.RegularExpressions;
using Tasks.Records;

namespace Tasks;

/// <summary>What POST and PUT /tasks accept; each check returns the reason for a 400, or null.</summary>
static partial class TaskValidation
{
    public const int MaxTitleLength       = 200;
    public const int MaxDescriptionLength = 2000;
    public const int MaxAssigneeLength    = 100;
    public const int MaxInterval          = 365;

    private static readonly string[] Units    = ["day", "week", "month"];
    private static readonly string[] Weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex DueTime();

    public static string? Check(CreateTaskRequest req) =>
        CheckFields(req.Title, req.DueTime, req.Description, req.Assignee) ?? CheckRecurrence(req);

    public static string? Check(UpdateTaskRequest req) =>
        CheckFields(req.Title, req.DueTime, req.Description, req.Assignee);

    private static string? CheckFields(string? title, string? dueTime, string? description, string? assignee)
    {
        if (string.IsNullOrWhiteSpace(title)) return "title required";
        if (title.Length > MaxTitleLength) return $"title must be at most {MaxTitleLength} characters";
        if (description?.Length > MaxDescriptionLength) return $"description must be at most {MaxDescriptionLength} characters";
        if (assignee?.Length > MaxAssigneeLength) return $"assignee must be at most {MaxAssigneeLength} characters";
        if (dueTime is not null && !DueTime().IsMatch(dueTime)) return "due_time must be HH:mm";
        return null;
    }

    private static string? CheckRecurrence(CreateTaskRequest req)
    {
        // Without a unit the task doesn't repeat, and the other recurrence fields mean nothing
        if (req.RecurrenceUnit is null) return null;

        if (!Units.Contains(req.RecurrenceUnit)) return "recurrence_unit must be day, week or month";
        if (req.DueDate is null) return "a repeating task needs a due date";
        if (req.IsCountdown) return "a countdown can't repeat";
        if (req.RecurrenceInterval is < 1 or > MaxInterval) return $"recurrence_interval must be 1–{MaxInterval}";
        if (req.RecurrenceEndDate is { } end && end.Date < req.DueDate.Value.Date)
            return "recurrence_end_date must not be before the due date";
        if (req.RecurrenceDays is { } days)
        {
            if (req.RecurrenceUnit != "week") return "recurrence_days only applies to weekly tasks";
            var chosen = days.Split(',', StringSplitOptions.TrimEntries);
            if (chosen.Length == 0 || chosen.Any(day => !Weekdays.Contains(day)) || chosen.Distinct().Count() != chosen.Length)
                return "recurrence_days must be a comma-separated list of Mon–Sun, each once";
        }
        return null;
    }
}
