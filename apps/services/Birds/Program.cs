using ServiceDefaults;
using Birds.Extensions;
using DotNetEnv;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForBirds();

var app = builder.Build();

app.InitializeWebAppForBirds();

app.Run();
