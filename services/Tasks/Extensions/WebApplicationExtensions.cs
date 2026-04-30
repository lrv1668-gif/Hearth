using Tasks;
using Tasks.Records;

namespace tasks.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForTasks(this WebApplication app)
    {
        app.MigrateTaskStore();
        app.AddTasksEndpoints();
    }

    private static void MigrateTaskStore(this WebApplication app)
    {
        app.Services.GetRequiredService<TaskStore>().Migrate();
    }

    private static void AddTasksEndpoints(this WebApplication app)
    {
        app.MapGet("/tasks", (TaskStore store) => store.List());

        app.MapPost("/tasks", (CreateTaskRequest req, TaskStore store) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest("title required");
            var task = store.Create(req.Title, req.DueDate, req.DueTime,
                req.Description, req.Assignee,
                req.RecurrenceUnit, req.RecurrenceInterval, req.RecurrenceDays, req.RecurrenceEndDate);
            return Results.Created($"/tasks/{task.Id}", task);
        });

        app.MapPut("/tasks/{id:long}", (long id, UpdateTaskRequest req, TaskStore store) =>
        {
            var task = store.Update(id, req.Done, req.Title, req.DueDate, req.DueTime, req.Description, req.Assignee);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        app.MapDelete("/tasks/{id:long}", (long id, bool? series, TaskStore store) =>
        {
            store.Delete(id, series == true);
            return Results.NoContent();
        });
    }
}

