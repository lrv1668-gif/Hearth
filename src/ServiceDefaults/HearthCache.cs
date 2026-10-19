using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace ServiceDefaults;

public static class HearthCache
{
    /// <summary>
    /// Registers FusionCache for caching upstream API responses. Fail-safe is on by default: when a
    /// refresh fails, the last good value keeps being served (up to the entry's FailSafeMaxDuration)
    /// rather than an error — a quiet wall display should show slightly old data, not a broken widget.
    /// </summary>
    /// <remarks>
    /// Uses an <c>IDistributedCache</c> as L2 when one is registered (e.g. <c>AddSqliteDistributedCache</c>
    /// from <c>Data</c>), so cached values survive restarts; otherwise memory-only.
    /// </remarks>
    public static IServiceCollection AddHearthCache(this IServiceCollection services)
    {
        services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(6),
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer(HearthJson.SnakeCaseLower))
            .TryWithRegisteredDistributedCache();

        return services;
    }
}
