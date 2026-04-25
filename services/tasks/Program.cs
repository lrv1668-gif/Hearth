using System.Text.Json;
using Tasks;
using Tasks.Records;
using Data;

var builder = WebApplication.CreateBuilder(args);
var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "tasks.db";

builder.Services.AddKeyedSingleton("tasks", (_, _) => new Database(dbPath));
builder.Services.AddSingleton<TaskStore>();
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

var app = builder.Build();
app.UseCors();
app.Services.GetRequiredService<TaskStore>().Migrate();

app.MapGet("/tasks", (TaskStore store) => store.List());

app.MapPost("/tasks", (CreateTaskRequest req, TaskStore store) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
    {
        return Results.BadRequest("title required");
    }
    var task = store.Create(req.Title, req.DueDate, req.DueTime);
    return Results.Created($"/tasks/{task.Id}", task);
});

app.MapPut("/tasks/{id:long}", (long id, UpdateTaskRequest req, TaskStore store) =>
{
    var task = store.Update(id, req.Done);
    return task is null ? Results.NotFound() : Results.Ok(task);
});

app.MapDelete("/tasks/{id:long}", (long id, TaskStore store) =>
{
    store.Delete(id);
    return Results.NoContent();
});

app.Run();
