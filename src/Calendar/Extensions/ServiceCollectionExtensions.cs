using Calendar;
using Calendar.Providers;
using Calendar.Providers.Google;
using Data.Extensions;
using ServiceDefaults;

namespace Calendar.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForCalendar(this IServiceCollection services)
    {
        services.AddSqliteDatabase("calendar", "calendar.db");
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<CalendarStore>();
        services.AddSingleton<GoogleAuthService>();
        services.AddSingleton<GoogleCalendarProvider>();

        // Double-register GoogleCalendarProvider so:
        // - Provider-specific endpoints can inject GoogleCalendarProvider directly
        // - /calendar/events aggregation injects IEnumerable<ICalendarProvider>
        services.AddSingleton<ICalendarProvider>(sp => sp.GetRequiredService<GoogleCalendarProvider>());

        services.AddHearthWebDefaults();
    }
}
