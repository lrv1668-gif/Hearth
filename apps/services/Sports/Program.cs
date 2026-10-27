using ServiceDefaults;
using Sports.Extensions;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForSports();

var app = builder.Build();

app.InitializeWebAppForSports();

app.Run();

public partial class Program; // lets endpoint tests use WebApplicationFactory<Program>
