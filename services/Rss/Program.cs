using Rss.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForRss();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForRss();

app.Run();
