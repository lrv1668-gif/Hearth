using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace ServiceDefaults;

public static class HearthCache
{
    /// <summary>
    /// What cached values are serialized with (for the L2): snake_case like the APIs, plus
    /// fields, so value tuples (whose Item1/Item2 are fields) round-trip instead of coming back empty.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new(HearthJson.SnakeCaseLower) { IncludeFields = true };

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
            .WithSerializer(new FusionCacheSystemTextJsonSerializer(SerializerOptions))
            .TryWithRegisteredDistributedCache();

        // Settings → Diagnostics: an upstream that keeps failing behind the fail-safe
        if (services.All(d => d.ServiceType != typeof(UpstreamFailures)))
        {
            services.AddSingleton(sp => new UpstreamFailures(sp.GetRequiredService<IFusionCache>(), sp.GetService<TimeProvider>()));
            services.AddHealthChecks().AddCheck<UpstreamFailures>("upstream");
        }

        return services;
    }
}
