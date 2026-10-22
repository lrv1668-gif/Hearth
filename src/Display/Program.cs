using Display.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Initialize services for each service
builder.Services.AddServicesForDisplay();

var app = builder.Build();
app.UseCors();

// Setup each service
app.InitializeWebAppForDisplay();

app.Run();

// For WebApplicationFactory in Display.Tests
public partial class Program;
