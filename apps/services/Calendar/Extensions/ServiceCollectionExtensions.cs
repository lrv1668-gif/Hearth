using Calendar;
using Calendar.Providers;
using Calendar.Providers.Google;
using Calendar.Providers.Ics;
using Data.Extensions;
using ServiceDefaults;

namespace Calendar.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForCalendar(this IServiceCollection services)
    {
        services.AddSqliteDatabase("calendar", "calendar.db");
        services.AddHearthDataProtection("calendar");
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<CalendarStore>();
        services.AddSingleton<GoogleAuthService>();
        services.AddSingleton<GoogleCalendarProvider>();

        // Double-register GoogleCalendarProvider so:
        // - Provider-specific endpoints can inject GoogleCalendarProvider directly
        // - /calendar/items aggregation injects IEnumerable<ICalendarProvider>
        services.AddSingleton<ICalendarProvider>(sp => sp.GetRequiredService<GoogleCalendarProvider>());

        // Calendars subscribed to by link (iCloud's public webcal:// address, any .ics URL).
        // FusionCache holds each one's items, in calendar.db so they survive a restart; the
        // home time zone places events that carry no zone of their own.
        services.AddSqliteDistributedCache("calendar");
        services.AddHearthCache();
        services.AddHearthHomeLocation();
        services.AddSingleton<FeedUrlValidator>();
        services.AddSingleton<IIcsSource, IcsFetcher>();
        services.AddSingleton<IcsFeedStore>();
        services.AddSingleton<IcsCalendarProvider>();
        services.AddSingleton<ICalendarProvider>(sp => sp.GetRequiredService<IcsCalendarProvider>());

        services.AddHearthWebDefaults();
        services.AddHealthChecks().AddDatabase("calendar").AddRequiredConfig("GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "GOOGLE_REDIRECT_URI");
    }
}
