using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ServiceDefaults.Tests;

public sealed class HearthServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHearthWebDefaults_ConfiguresDefaultCorsPolicyToAllowAnyOriginMethodAndHeader()
    {
        var provider = new ServiceCollection()
            .AddHearthWebDefaults()
            .BuildServiceProvider();

        var corsOptions = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

        Assert.NotNull(policy);
        Assert.True(policy!.AllowAnyOrigin);
        Assert.True(policy.AllowAnyMethod);
        Assert.True(policy.AllowAnyHeader);
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
