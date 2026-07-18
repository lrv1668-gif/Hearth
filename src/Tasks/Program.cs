using Tasks.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Initialize services for each service
builder.Services.AddServicesForTasks();

var app = builder.Build();
app.UseCors();

// Setup each service
app.InitializeWebAppForTasks();

app.Run();
