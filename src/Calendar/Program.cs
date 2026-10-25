using ServiceDefaults;
using Calendar.Extensions;
using DotNetEnv;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForCalendar();

var app = builder.Build();

app.InitializeWebAppForCalendar();

app.Run();
