namespace Tasks.Records;

public record TaskItem(
    long Id,
    string Title,
    bool Done,
    DateTime? DueDate,
    string? DueTime,
    DateTime CreatedAt,
    string? Description,
    string? Assignee,
    string? RecurrenceUnit,
    int? RecurrenceInterval,
    string? RecurrenceDays
);
