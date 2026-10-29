using ServiceDefaults;
using Weather.Extensions;
using DotNetEnv;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForWeather();

var app = builder.Build();

app.InitializeWebAppForWeather();

app.Run();
