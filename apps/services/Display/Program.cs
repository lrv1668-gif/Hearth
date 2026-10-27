using ServiceDefaults;
using Display.Extensions;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

var builder = WebApplication.CreateBuilder(args);

// Initialize services for each service
builder.Services.AddServicesForDisplay();

var app = builder.Build();

// Setup each service
app.InitializeWebAppForDisplay();

app.Run();

// For WebApplicationFactory in Display.Tests
public partial class Program;
