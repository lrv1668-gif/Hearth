using Almanac.Extensions;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForAlmanac();

var app = builder.Build();

app.InitializeWebAppForAlmanac();

app.Run();
