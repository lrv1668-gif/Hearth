using System.Net;

namespace Birds.Tests.Helpers;

/// <summary>
/// Returns a canned response per URL-path substring, so the recent and notable
/// eBird endpoints can be faked with different payloads in a single handler.
/// </summary>
public sealed class FakeHttpMessageHandler(
    IReadOnlyDictionary<string, string> responsesByPathContains,
    HttpStatusCode status = HttpStatusCode.OK)
    : HttpMessageHandler
{
    public FakeHttpMessageHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
        : this(new Dictionary<string, string> { [""] = responseJson }, status)
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;

        // Longest key wins so "recent/notable" beats "recent".
        var json = responsesByPathContains
            .Where(kv => path.Contains(kv.Key))
            .OrderByDescending(kv => kv.Key.Length)
            .Select(kv => kv.Value)
            .First();

        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
