using System.Net;

namespace Rss.Tests.Helpers;

public sealed class FakeHttpMessageHandler(string responseBody, HttpStatusCode status = HttpStatusCode.OK)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/xml")
        };
        return Task.FromResult(response);
    }
}
