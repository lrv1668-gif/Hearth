using System.Net;
using System.Net.Sockets;

namespace Rss;

/// <summary>
/// Validates caller-supplied feed URLs before the service fetches them, to prevent
/// SSRF against loopback, private, link-local, and other non-public addresses.
/// DNS resolution is injectable so the validator can be unit tested without live DNS.
/// </summary>
public sealed class FeedUrlValidator(Func<string, Task<IPAddress[]>>? resolveHost = null)
{
    private readonly Func<string, Task<IPAddress[]>> _resolveHost =
        resolveHost ?? (host => Dns.GetHostAddressesAsync(host));

    /// <summary>
    /// Returns true only for absolute http/https URLs whose host resolves
    /// exclusively to publicly routable IP addresses.
    /// </summary>
    public async Task<bool> IsAllowedAsync(string url) =>
        await ResolvePinnedAddressAsync(url) is not null;

    /// <summary>
    /// Resolves the host of an absolute http/https URL and returns the first
    /// resolved address if every resolved address is publicly routable, or
    /// null if the URL is disallowed. Callers should connect using the
    /// returned address (rather than re-resolving the host) so the address
    /// that was validated is the one actually fetched — otherwise a DNS
    /// answer that changes between validation and fetch (DNS rebinding)
    /// could bypass this check.
    /// </summary>
    public async Task<IPAddress?> ResolvePinnedAddressAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return null;

        var host = uri.DnsSafeHost;
        if (string.IsNullOrEmpty(host))
            return null;

        IPAddress[] addresses;
        if (IPAddress.TryParse(host, out var literal))
        {
            addresses = [literal];
        }
        else
        {
            try
            {
                addresses = await _resolveHost(host);
            }
            catch
            {
                return null;
            }
        }

        // Every resolved address must be public; a single non-public record is enough to reject.
        return addresses.Length > 0 && addresses.All(IsPublicAddress) ? addresses[0] : null;
    }

    internal static bool IsPublicAddress(IPAddress ip)
    {
        // Normalize IPv4-mapped IPv6 (::ffff:a.b.c.d) so it is checked as IPv4.
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (IPAddress.IsLoopback(ip))
            return false;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] switch
            {
                0 => false,                                  // 0.0.0.0/8 "this network"
                10 => false,                                 // 10.0.0.0/8 private
                100 when b[1] >= 64 && b[1] <= 127 => false, // 100.64.0.0/10 CGNAT
                127 => false,                                // 127.0.0.0/8 loopback
                169 when b[1] == 254 => false,               // 169.254.0.0/16 link-local (incl. metadata)
                172 when b[1] >= 16 && b[1] <= 31 => false,  // 172.16.0.0/12 private
                192 when b[1] == 0 && b[2] == 0 => false,    // 192.0.0.0/24 IETF protocol assignments
                192 when b[1] == 168 => false,               // 192.168.0.0/16 private
                198 when b[1] == 18 || b[1] == 19 => false,  // 198.18.0.0/15 benchmarking
                >= 224 => false,                             // 224.0.0.0/4 multicast, 240.0.0.0/4 reserved
                _ => true,
            };
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.Equals(IPAddress.IPv6Any))
                return false;                                // ::
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
                return false;                                // fe80::/10, fec0::/10, ff00::/8
            var b = ip.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC)
                return false;                                // fc00::/7 unique local
            return true;
        }

        // Unknown address family — reject rather than fetch.
        return false;
    }
}
