using weather.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForWeather();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForWeather();

app.Run();
