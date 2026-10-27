using ServiceDefaults;
using Rss.Extensions;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForRss();

var app = builder.Build();

app.InitializeWebAppForRss();

app.Run();
