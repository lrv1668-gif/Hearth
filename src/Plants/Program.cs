using Plants.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Initialize services for each service
builder.Services.AddServicesForPlants();

var app = builder.Build();
app.UseCors();

// Setup each service
app.InitializeWebAppForPlants();

app.Run();
