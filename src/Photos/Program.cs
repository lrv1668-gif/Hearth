using DotNetEnv;
using Photos.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 200L * 1024 * 1024);

builder.Services.AddServicesForPhotos();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForPhotos();

app.Run();
