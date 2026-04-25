namespace Tasks.Records;

public record TaskItem(long Id, string Title, bool Done, DateTime? DueDate, string? DueTime, DateTime CreatedAt);
