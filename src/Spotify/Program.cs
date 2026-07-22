using Spotify.Extensions;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForSpotify();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForSpotify();

app.Run();
