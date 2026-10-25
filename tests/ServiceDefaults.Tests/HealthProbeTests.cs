using Xunit;

namespace ServiceDefaults.Tests;

public sealed class HealthProbeTests
{
    private static HealthReportJson Report(string status, params HealthCheckJson[] checks) => new(status, checks);

    [Theory]
    [InlineData("healthy", 0)]
    [InlineData("degraded", 0)]
    [InlineData("unhealthy", 1)]
    public void ExitCode_ByStatus_DegradedStillHealthy(string status, int expected)
    {
        Assert.Equal(expected, HealthProbe.ExitCode(Report(status)));
    }

    [Fact]
    public void ExitCode_NoReport_Unhealthy()
    {
        Assert.Equal(1, HealthProbe.ExitCode(null));
    }

    [Theory]
    [InlineData("8081", "8081")]
    [InlineData("8081;8082", "8081")]
    [InlineData(" 8081 ; 8082", "8081")]
    [InlineData(null, "8080")]
    [InlineData("", "8080")]
    public void Port_FromAspNetCoreHttpPorts_FirstOrDefault(string? ports, string expected)
    {
        Assert.Equal(expected, HealthProbe.Port(ports));
    }

    [Fact]
    public void Summary_ListsOnlyTheProblems()
    {
        var report = Report("degraded",
            new HealthCheckJson("database", "healthy", "Readable"),
            new HealthCheckJson("config", "degraded", "Not set: EBIRD_API_KEY"));

        Assert.Equal("degraded · config: Not set: EBIRD_API_KEY", HealthProbe.Summary(report));
    }
}
