namespace Quote.Tests.Helpers;

/// <summary>Simulates a network-level failure (as opposed to an HTTP error status).</summary>
public sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("simulated network failure");
}
