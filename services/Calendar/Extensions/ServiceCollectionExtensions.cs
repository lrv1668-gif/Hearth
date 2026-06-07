using System.Text.Json;
using Calendar;
using Calendar.Providers;
using Calendar.Providers.Google;
using Data;
using Data.Abstractions;

namespace Calendar.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForCalendar(this IServiceCollection services)
    {
        var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "calendar.db";

        services.AddKeyedSingleton<IDatabase>("calendar", (_, _) => new Database(dbPath));
        services.AddSingleton<CalendarStore>();
        services.AddSingleton<GoogleAuthService>();
        services.AddSingleton<GoogleCalendarProvider>();

        // Double-register GoogleCalendarProvider so:
        // - Provider-specific endpoints can inject GoogleCalendarProvider directly
        // - /calendar/events aggregation injects IEnumerable<ICalendarProvider>
        services.AddSingleton<ICalendarProvider>(sp => sp.GetRequiredService<GoogleCalendarProvider>());

        services.AddCors(opts =>
            opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.ConfigureHttpJsonOptions(opts =>
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
