using Calendar.Extensions;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForCalendar();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForCalendar();

app.Run();
