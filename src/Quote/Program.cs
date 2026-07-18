using Quote.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesForQuote();

var app = builder.Build();
app.UseCors();

app.InitializeWebAppForQuote();

app.Run();
