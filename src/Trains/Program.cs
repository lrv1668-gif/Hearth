using Trains.Extensions;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForTrains();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForTrains();

app.Run();
