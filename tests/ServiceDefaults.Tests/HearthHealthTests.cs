using Hearth.TestUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace ServiceDefaults.Tests;

public sealed class HearthHealthTests
{
    private static async Task<HealthReport> CheckAsync(
        Action<IHealthChecksBuilder> add, params (string Key, string? Value)[] config)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(config.ToDictionary(e => e.Key, e => e.Value))
            .Build());
        add(services.AddHealthChecks());
        await using var provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();
    }

    [Fact]
    public async Task AddRequiredConfig_SomeMissing_DegradedNamingOnlyTheMissing()
    {
        var report = await CheckAsync(b => b.AddRequiredConfig("KEY_A", "KEY_B"), ("KEY_A", "secret-value"));

        var entry = report.Entries["config"];
        Assert.Equal(HealthStatus.Degraded, entry.Status);
        Assert.Equal("Not set: KEY_B", entry.Description);
        Assert.DoesNotContain("secret-value", entry.Description);
    }

    [Fact]
    public async Task AddRequiredConfig_AllSet_Healthy()
    {
        var report = await CheckAsync(b => b.AddRequiredConfig("KEY_A"), ("KEY_A", "1"));

        Assert.Equal(HealthStatus.Healthy, report.Status);
    }

    [Fact]
    public async Task AddDatabase_Readable_Healthy()
    {
        using var db = new TempDatabase();

        var report = await CheckAsync(b =>
        {
            b.Services.AddKeyedSingleton<Data.Abstractions.IDatabase>("test", db.Db);
            b.AddDatabase("test");
        });

        Assert.Equal(HealthStatus.Healthy, report.Entries["database"].Status);
    }

    [Fact]
    public async Task AddDatabase_Throws_UnhealthyWithMessage()
    {
        var report = await CheckAsync(b => b.AddDatabase("not-registered"));

        var json = HearthHealth.ToJson(report);

        Assert.Equal("unhealthy", json.Status);
        var check = Assert.Single(json.Checks);
        Assert.Equal("database", check.Name);
        Assert.Equal("unhealthy", check.Status);
        Assert.False(string.IsNullOrEmpty(check.Description));
    }

    [Fact]
    public async Task ToJson_NoChecks_Healthy()
    {
        var json = HearthHealth.ToJson(await CheckAsync(_ => { }));

        Assert.Equal("healthy", json.Status);
        Assert.Empty(json.Checks);
    }
}
