using Birds.Extensions;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForBirds();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForBirds();

app.Run();
