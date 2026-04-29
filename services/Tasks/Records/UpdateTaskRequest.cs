namespace Tasks.Records;

record UpdateTaskRequest(bool Done, string? Description, string? Assignee);
