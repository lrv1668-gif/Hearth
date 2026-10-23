using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ServiceDefaults.Tests;

public sealed class HearthServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHearthWebDefaults_RegistersNoCorsPolicy()
    {
        var provider = new ServiceCollection()
            .AddHearthWebDefaults()
            .BuildServiceProvider();

        // Every service is reached same-origin through Caddy, so no site gets cross-origin access
        Assert.Null(provider.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>());
    }

    [Fact]
    public void AddHearthWebDefaults_ConfiguresSnakeCaseJsonNamingPolicy()
    {
        var provider = new ServiceCollection()
            .AddHearthWebDefaults()
            .BuildServiceProvider();

        var jsonOptions = provider.GetRequiredService<IOptions<JsonOptions>>().Value;
        var json = JsonSerializer.Serialize(new { FooBar = 1 }, jsonOptions.SerializerOptions);

        Assert.Equal("""{"foo_bar":1}""", json);
    }
}
