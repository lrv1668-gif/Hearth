using Data.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ServiceDefaults;

/// <summary>One check in a <see cref="HealthReportJson"/>: "healthy", "degraded" or "unhealthy".</summary>
public sealed record HealthCheckJson(string Name, string Status, string? Description);

/// <summary>What GET /&lt;service&gt;/health returns, for Settings → Diagnostics.</summary>
public sealed record HealthReportJson(string Status, IReadOnlyList<HealthCheckJson> Checks);

/// <summary>
/// Per-service health for Settings → Diagnostics. Every service gets an empty report from
/// <see cref="HearthServiceCollectionExtensions.AddHearthWebDefaults"/>, and adds the checks
/// that matter to it. An optional feature that isn't set up (an API key, the home location)
/// is Degraded, not Unhealthy — a household that never set up trains shouldn't see red.
/// </summary>
public static class HearthHealth
{
    /// <summary>
    /// Maps GET /&lt;prefix&gt;/health. Always 200 with a JSON body, so the page can tell a
    /// service that answers with a problem from one that doesn't answer at all.
    /// </summary>
    public static IEndpointConventionBuilder MapHearthHealth(this WebApplication app, string prefix)
    {
        // Created now, not on the first health request, so it sees failures from startup on
        app.Services.GetService<UpstreamFailures>();

        return app.MapHealthChecks($"/{prefix}/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status200OK,
            },
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(ToJson(report), HearthJson.SnakeCaseLower),
        });
    }

    public static HealthReportJson ToJson(HealthReport report) => new(
        Name(report.Status),
        report.Entries
            .Select(e => new HealthCheckJson(e.Key, Name(e.Value.Status), e.Value.Description ?? e.Value.Exception?.Message))
            .ToList());

    private static string Name(HealthStatus status) => status.ToString().ToLowerInvariant();

    /// <summary>Degraded, naming (never showing) the env vars that aren't set.</summary>
    public static IHealthChecksBuilder AddRequiredConfig(this IHealthChecksBuilder builder, params string[] names) =>
        builder.Add(new HealthCheckRegistration("config", services =>
            new DelegateCheck(() =>
            {
                var missing = services.GetRequiredService<IConfiguration>().Missing(names);
                return missing.Count == 0
                    ? HealthCheckResult.Healthy("Configured")
                    : HealthCheckResult.Degraded($"Not set: {string.Join(", ", missing)}");
            }), HealthStatus.Degraded, tags: null));

    /// <summary>Unhealthy when the service's SQLite database (keyed <paramref name="key"/>) can't be read.</summary>
    public static IHealthChecksBuilder AddDatabase(this IHealthChecksBuilder builder, string key) =>
        builder.Add(new HealthCheckRegistration("database", services =>
            new DelegateCheck(() =>
            {
                services.GetRequiredKeyedService<IDatabase>(key).QueryOne("SELECT 1", r => r.GetInt64(0));
                return HealthCheckResult.Healthy("Readable");
            }), HealthStatus.Unhealthy, tags: null));

    /// <summary>Degraded when there's no home location in Settings or the env vars.</summary>
    public static IHealthChecksBuilder AddHomeLocation(this IHealthChecksBuilder builder) =>
        builder.Add(new HealthCheckRegistration("home location", services =>
            new DelegateCheck(async ct =>
            {
                var home = await services.GetRequiredService<HomeLocationClient>().GetAsync(ct);
                return home.Coordinates is null
                    ? HealthCheckResult.Degraded("No home location — choose one in Settings → Home")
                    : HealthCheckResult.Healthy("Set");
            }), HealthStatus.Degraded, tags: null));

    private sealed class DelegateCheck(Func<CancellationToken, Task<HealthCheckResult>> check) : IHealthCheck
    {
        public DelegateCheck(Func<HealthCheckResult> check) : this(_ => Task.FromResult(check())) { }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
            check(ct);
    }
}
