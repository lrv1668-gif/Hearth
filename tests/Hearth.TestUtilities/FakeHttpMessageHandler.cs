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

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        RequestCount++;
        var response = new HttpResponseMessage(Status)
        {
            Content = new StringContent(ResponseJson, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
