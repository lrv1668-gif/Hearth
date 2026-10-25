using ServiceDefaults;
using Spotify.Extensions;
using DotNetEnv;

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForSpotify();

var app = builder.Build();

app.InitializeWebAppForSpotify();

app.Run();

// For WebApplicationFactory in Spotify.Tests
public partial class Program;
