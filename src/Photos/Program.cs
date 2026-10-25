using ServiceDefaults;
using DotNetEnv;
using Photos.Extensions;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 200L * 1024 * 1024);

builder.Services.AddServicesForPhotos();

var app = builder.Build();

app.InitializeWebAppForPhotos();

app.Run();
