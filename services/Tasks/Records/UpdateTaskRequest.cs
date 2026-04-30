namespace Tasks.Records;

record UpdateTaskRequest(bool Done, string? Title, DateTime? DueDate, string? DueTime, string? Description, string? Assignee);
