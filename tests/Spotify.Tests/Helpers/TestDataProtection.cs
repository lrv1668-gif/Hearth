using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Spotify.Tests.Helpers;

public static class TestDataProtection
{
    public static IDataProtectionProvider Provider { get; } =
        new ServiceCollection()
            .AddDataProtection()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IDataProtectionProvider>();
}
