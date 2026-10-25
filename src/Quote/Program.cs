using ServiceDefaults;
using Quote.Extensions;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForQuote();

var app = builder.Build();

app.InitializeWebAppForQuote();

app.Run();
