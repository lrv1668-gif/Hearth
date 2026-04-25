namespace Tasks.Records;

record CreateTaskRequest(string Title, DateTime? DueDate, string? DueTime);
