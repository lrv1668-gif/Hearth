using DotNetEnv;
using Photos.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForPhotos();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForPhotos();

app.Run();
