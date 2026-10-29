namespace Tasks.Records;

record CreateTaskRequest(
    string Title,
    DateTime? DueDate,
    string? DueTime,
    string? Description,
    string? Assignee,
    string? RecurrenceUnit,
    int? RecurrenceInterval,
    string? RecurrenceDays,
    DateTime? RecurrenceEndDate,
    bool IsCountdown = false
);
