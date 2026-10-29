using ServiceDefaults;
using Trains.Extensions;
using DotNetEnv;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForTrains();

var app = builder.Build();

app.InitializeWebAppForTrains();

app.Run();
