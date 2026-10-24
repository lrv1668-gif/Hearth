using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Display;

/// <summary>
/// Settings → Diagnostics: whether the panel's frame is rendering, and when the frame
/// client last checked in. Degraded while the last render failed (the frame keeps the
/// previous picture), since that's usually the renderer sidecar.
/// </summary>
public sealed class FrameHealthCheck(FrameRenderService renderer, FramePolls polls, TimeProvider time) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var status = renderer.Status();
        var now = time.GetUtcNow();
        var checkIn = polls.Last is { } poll ? $"frame checked in {Ago(now - poll.At)}" : "no frame check-in since restart";

        if (status.LastError is { } error)
            return Task.FromResult(HealthCheckResult.Degraded($"Last render failed: {error} · {checkIn}"));

        var rendered = status.RenderedAt is { } at ? $"Rendered {Ago(now - at)}" : "Not rendered yet";
        return Task.FromResult(HealthCheckResult.Healthy($"{rendered} · {checkIn}"));
    }

    internal static string Ago(TimeSpan elapsed) => elapsed switch
    {
        { TotalMinutes: < 1 } => "just now",
        { TotalHours: < 1 } => $"{(int)elapsed.TotalMinutes} min ago",
        { TotalDays: < 1 } => $"{(int)elapsed.TotalHours} h ago",
        _ => $"{(int)elapsed.TotalDays} d ago",
    };
}
