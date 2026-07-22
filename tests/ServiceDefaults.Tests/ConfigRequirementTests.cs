using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceDefaults.Tests.Helpers;
using Xunit;

namespace ServiceDefaults.Tests;

public sealed class ConfigRequirementTests
{
    private static IConfiguration MakeConfig(params (string Key, string? Value)[] entries) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(entries.ToDictionary(e => e.Key, e => e.Value))
            .Build();

    [Fact]
    public void Missing_AllPresent_ReturnsEmptyList()
    {
        var config = MakeConfig(("A", "1"), ("B", "2"));

        Assert.Empty(config.Missing("A", "B"));
    }

    [Fact]
    public void Missing_SomeAbsentOrWhitespace_ReturnsThoseNames()
    {
        var config = MakeConfig(("A", "1"), ("B", "   "), ("C", null));

        var missing = config.Missing("A", "B", "C", "D");

        Assert.Equal(["B", "C", "D"], missing);
    }

    [Fact]
    public void RequireOrFail_NoneMissing_ReturnsNull()
    {
        var config = MakeConfig(("A", "1"));
        var logger = new FakeLogger();

        var result = config.RequireOrFail(logger, _ => Results.Ok(), "A");

        Assert.Null(result);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void RequireOrFail_SomeMissing_LogsErrorAndInvokesRespond()
    {
        var config = MakeConfig(("A", null));
        var logger = new FakeLogger();
        IReadOnlyList<string>? received = null;

        var result = config.RequireOrFail(logger, missing =>
        {
            received = missing;
            return Results.Json(new { error = "nope" }, statusCode: 503);
        }, "A");

        Assert.NotNull(result);
        Assert.Equal(["A"], received);
        Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, logger.Entries[0].Level);
    }

    [Fact]
    public void WarnIfMissing_NoneMissing_DoesNotLog()
    {
        var config = MakeConfig(("A", "1"));
        var logger = new FakeLogger();

        config.WarnIfMissing(logger, "should not appear", "A");

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void WarnIfMissing_SomeMissing_LogsMessage()
    {
        var config = MakeConfig(("A", null));
        var logger = new FakeLogger();

        config.WarnIfMissing(logger, "A is required", "A");

        Assert.Single(logger.Entries);
        Assert.Equal("A is required", logger.Entries[0].Message);
    }
}
