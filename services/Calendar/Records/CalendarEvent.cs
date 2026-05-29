namespace Calendar.Records;

public record CalendarEvent(
    string Id,
    string Title,
    string? Description,
    string? Location,
    string Start,       // ISO 8601 with timezone offset, OR "YYYY-MM-DD" for all-day events
    string End,
    bool IsAllDay,
    string? CalendarName,
    string Provider     // "google"
);
