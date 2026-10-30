using Data.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Hearth.TestUtilities;

/// <summary>Boots a service in memory for endpoint tests.</summary>
public static class HearthApp
{
    /// <summary>
    /// The service with its keyed <see cref="IDatabase"/> swapped for <paramref name="db"/>;
    /// <paramref name="configure"/> adds settings and further test services.
    /// </summary>
    public static WebApplicationFactory<TProgram> Create<TProgram>(
        string dbKey, TempDatabase db, Action<IWebHostBuilder>? configure = null) where TProgram : class =>
        new WebApplicationFactory<TProgram>().WithWebHostBuilder(host =>
        {
            host.ConfigureTestServices(services => services.AddKeyedSingleton<IDatabase>(dbKey, db.Db));
            configure?.Invoke(host);
        });
}
