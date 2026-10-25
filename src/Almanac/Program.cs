using ServiceDefaults;
using Almanac.Extensions;
using DotNetEnv;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForAlmanac();

var app = builder.Build();

app.InitializeWebAppForAlmanac();

app.Run();
