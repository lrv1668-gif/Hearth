using System.Net.Http.Json;

namespace ServiceDefaults;

/// <summary>
/// The container health check: <c>/app/service --health /&lt;prefix&gt;/health</c> calls the running
/// service's own health endpoint and exits 0 or 1, so <c>docker ps</c> and Dozzle show Hearth's
/// real checks. The images are chiseled — no shell, curl or wget — so the service binary is
/// its own probe. Degraded counts as healthy: an optional integration that isn't set up
/// shouldn't mark the container unhealthy.
/// </summary>
public static class HealthProbe
{
    public const string Flag = "--health";

    /// <summary>Call first in Program.cs: runs the probe and exits when the args ask for it, else returns.</summary>
    public static void ExitIfProbe(string[] args)
    {
        if (args is not [Flag, var path]) return;
        Environment.Exit(RunAsync(path).GetAwaiter().GetResult());
    }

    private static async Task<int> RunAsync(string path)
    {
        var url = $"http://127.0.0.1:{Port(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"))}{path}";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var report = await http.GetFromJsonAsync<HealthReportJson>(url, HearthJson.SnakeCaseLower);
            // Docker keeps the probe's output (docker inspect), so say what's wrong
            Console.WriteLine(Summary(report));
            return ExitCode(report);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"unreachable: {ex.Message}");
            return 1;
        }
    }

    /// <summary>The first of ASPNETCORE_HTTP_PORTS ("8081" or "8081;8082"), else ASP.NET Core's default 8080.</summary>
    public static string Port(string? ports) =>
        ports?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "8080";

    /// <summary>0 for healthy or degraded, 1 for unhealthy or no report.</summary>
    public static int ExitCode(HealthReportJson? report) =>
        report?.Status is "healthy" or "degraded" ? 0 : 1;

    public static string Summary(HealthReportJson? report)
    {
        if (report is null) return "no report";
        var problems = report.Checks.Where(c => c.Status != "healthy").Select(c => $"{c.Name}: {c.Description}");
        return string.Join(" · ", problems.Prepend(report.Status));
    }
}
