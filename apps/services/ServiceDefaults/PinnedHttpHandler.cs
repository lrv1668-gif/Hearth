using System.Net;
using System.Net.Sockets;

namespace ServiceDefaults;

public static class PinnedHttpHandler
{
    /// <summary>
    /// A handler for fetching a caller-supplied URL: it connects to <paramref name="pinnedAddress"/>
    /// rather than re-resolving the URL's host, and follows no redirects. The address must come
    /// from <see cref="FeedUrlValidator.ResolvePinnedAddressAsync"/> for the same URL — pinning
    /// closes the gap where the host's DNS answer could change between validation and fetch
    /// (DNS rebinding), and a redirect target hasn't been validated at all. TLS/SNI and the Host
    /// header still use the URL's hostname, so HTTPS virtual hosting and certificate validation
    /// are unaffected.
    /// </summary>
    public static SocketsHttpHandler Create(IPAddress pinnedAddress) => new()
    {
        AllowAutoRedirect = false,
        ConnectCallback = async (context, token) =>
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(pinnedAddress, context.DnsEndPoint.Port, token);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
            return new NetworkStream(socket, ownsSocket: true);
        },
    };
}
