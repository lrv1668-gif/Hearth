namespace Hearth.TestUtilities;

/// <summary>Simulates a network-level failure (as opposed to an HTTP error status).</summary>
public sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    /// <summary>How many requests were attempted, for asserting what a cache saved.</summary>
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        throw new HttpRequestException("simulated network failure");
    }
}
