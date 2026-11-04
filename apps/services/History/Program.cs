using ServiceDefaults;
using History.Extensions;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForHistory();

var app = builder.Build();

app.InitializeWebAppForHistory();

app.Run();

public partial class Program; // lets endpoint tests use WebApplicationFactory<Program>
