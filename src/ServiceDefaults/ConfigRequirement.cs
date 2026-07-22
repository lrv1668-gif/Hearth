using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceDefaults;

public static class ConfigRequirement
{
    public static IReadOnlyList<string> Missing(this IConfiguration config, params string[] names) =>
        names.Where(n => string.IsNullOrWhiteSpace(config[n])).ToList();

    public static IResult? RequireOrFail(
        this IConfiguration config,
        ILogger logger,
        Func<IReadOnlyList<string>, IResult> respond,
        params string[] names)
    {
        var missing = config.Missing(names);
        if (missing.Count == 0) return null;

        logger.LogError("{Vars} must be set. Update the .env file to configure.", string.Join(", ", missing));
        return respond(missing);
    }

    public static void WarnIfMissing(this IConfiguration config, ILogger logger, string message, params string[] names)
    {
        if (config.Missing(names).Count > 0) logger.LogError(message);
    }
}
