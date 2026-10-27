using ServiceDefaults;
using Tasks.Extensions;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

var builder = WebApplication.CreateBuilder(args);

// Initialize services for each service
builder.Services.AddServicesForTasks();

var app = builder.Build();

// Setup each service
app.InitializeWebAppForTasks();

app.Run();

// For WebApplicationFactory in Tasks.Tests
public partial class Program;
