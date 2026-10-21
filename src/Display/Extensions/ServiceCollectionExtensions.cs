using Data.Extensions;
using ServiceDefaults;

namespace Display.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForDisplay(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSqliteDatabase("display", "display.db");
        serviceCollection.AddSingleton<FrameSettingsStore>();

        serviceCollection.AddHearthWebDefaults();
    }
}
