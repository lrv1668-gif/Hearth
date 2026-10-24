using System.Net;

namespace Hearth.TestUtilities;

public sealed class FakeHttpMessageHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    : HttpMessageHandler
{
    /// <summary>The URL of the most recent request, for asserting what was called.</summary>
    public Uri? LastRequestUri { get; private set; }

    /// <summary>How many requests reached the handler, for asserting what a cache saved.</summary>
    public int RequestCount { get; private set; }

    /// <summary>The body later requests get; set it mid-test to simulate upstream changing.</summary>
    public string ResponseJson { get; set; } = responseJson;

    /// <summary>The status later requests get; set it mid-test to simulate an upstream failure.</summary>
    public HttpStatusCode Status { get; set; } = status;

    /// <summary>
    /// Per-endpoint answers, keyed by URL path, for a service that calls several — a path not
    /// listed gets <see cref="ResponseJson"/> and <see cref="Status"/>.
    /// </summary>
    public Dictionary<string, (HttpStatusCode Status, string Json)> ByPath { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        RequestCount++;
        var (status, json) = request.RequestUri is { } uri && ByPath.TryGetValue(uri.AbsolutePath, out var routed)
            ? routed
            : (Status, ResponseJson);
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
