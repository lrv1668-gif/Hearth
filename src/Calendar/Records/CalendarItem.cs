namespace Calendar.Records;

public record CalendarItem(
    string Kind,           // "event" | "task"
    string Id,
    string Title,
    string? Description,
    string? Location,
    string? Start,         // ISO 8601 with offset, "YYYY-MM-DD" for all-day/tasks, or null for undated tasks
    string? End,           // null for tasks
    bool IsAllDay,
    string? CalendarName,
    string Provider,       // "google"
    bool? IsCompleted,     // null for events; true/false for tasks
    string? TaskListId,    // null for events; needed for toggle endpoint
    string? HtmlLink       // direct URL to view in provider (event page or tasks.google.com)
);
